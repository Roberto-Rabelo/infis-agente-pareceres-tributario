using Api.RAG.Domain;
using Microsoft.Extensions.Options;

namespace Api.RAG.Application;

public sealed record ConsultaConhecimento(
    string? Pergunta,
    IReadOnlyList<string> RegraIds,
    IReadOnlyList<string> Obrigacoes,
    IReadOnlyList<TipoDocumento> TiposDocumento,
    DateOnly? DataReferencia,
    int? Limite,
    string? CorrelacaoId);

public sealed record ConsumoConsulta(string Provedor, string Modelo, int QuantidadeTextos, long? TokensEntrada);

public sealed record ResultadoConsulta(IReadOnlyList<TrechoEncontrado> Trechos, IReadOnlyList<string> Limitacoes, ConsumoConsulta? Consumo);

/// <summary>
/// Recuperação em dois modos: por identificador de regra (exata, determinística) e por similaridade da pergunta.
/// Trechos por regra vêm primeiro; a busca semântica completa até o limite sem repetir trechos.
/// </summary>
public sealed class ServicoConsulta(
    IRepositorioConhecimento repositorio,
    ProvedorEmbeddings embeddings,
    IOptions<OpcoesEmbeddings> opcoesEmbeddings,
    IOptions<OpcoesConsulta> opcoes)
{
    private readonly OpcoesConsulta _opcoes = opcoes.Value;

    public async Task<ResultadoConsulta> ConsultarAsync(ConsultaConhecimento consulta, CancellationToken cancelamento)
    {
        Validar(consulta);
        var limite = consulta.Limite ?? _opcoes.LimitePadrao;
        var filtro = new FiltroBusca(consulta.Obrigacoes, consulta.TiposDocumento, consulta.DataReferencia, embeddings.Modelo);
        var limitacoes = new List<string>();
        var trechos = new List<TrechoEncontrado>();

        if (consulta.RegraIds.Count > 0)
        {
            trechos.AddRange(await repositorio.BuscarPorRegrasAsync(consulta.RegraIds, filtro, limite, cancelamento).ConfigureAwait(false));
            var semTrecho = consulta.RegraIds.Where(id => !trechos.Any(t => t.Trecho.RegraIds.Contains(id))).ToList();
            if (semTrecho.Count > 0)
            {
                limitacoes.Add($"Sem trechos na base para: {string.Join(", ", semTrecho)}.");
            }
        }

        ConsumoConsulta? consumo = null;
        if (!string.IsNullOrWhiteSpace(consulta.Pergunta) && trechos.Count < limite)
        {
            try
            {
                var gerado = await embeddings.GerarAsync([consulta.Pergunta], "CONSULTA", consulta.CorrelacaoId, cancelamento).ConfigureAwait(false);
                consumo = new ConsumoConsulta(opcoesEmbeddings.Value.Provedor, embeddings.Modelo, 1, gerado.TokensEntrada);
                var semanticos = await repositorio
                    .BuscarSemanticaAsync(gerado.Vetores[0], filtro, limite, cancelamento)
                    .ConfigureAwait(false);
                var conhecidos = trechos.Select(t => t.TrechoId).ToHashSet();
                trechos.AddRange(semanticos
                    .Where(t => _opcoes.LimiarSimilaridade is not { } limiar || t.Similaridade >= limiar)
                    .Where(t => !conhecidos.Contains(t.TrechoId))
                    .Take(limite - trechos.Count));
            }
            catch (ProvedorEmbeddingsException) when (trechos.Count > 0)
            {
                limitacoes.Add("Busca semântica indisponível; retornados apenas os trechos localizados por regra.");
            }
        }

        if (trechos.Count == 0)
        {
            limitacoes.Add("Nenhum trecho encontrado para os filtros informados.");
        }

        return new ResultadoConsulta(trechos, limitacoes, consumo);
    }

    private void Validar(ConsultaConhecimento consulta)
    {
        var temPergunta = !string.IsNullOrWhiteSpace(consulta.Pergunta);
        if (!temPergunta && consulta.RegraIds.Count == 0)
        {
            throw new ErroValidacaoException("CONSULTA_VAZIA", "Informe uma pergunta ou ao menos um identificador de regra.");
        }

        if (consulta.Pergunta?.Length > _opcoes.MaximoCaracteresPergunta)
        {
            throw new ErroValidacaoException("PERGUNTA_EXCEDE_LIMITE", $"A pergunta excede {_opcoes.MaximoCaracteresPergunta} caracteres.");
        }

        if (consulta.RegraIds.Count > _opcoes.MaximoRegraIds || !consulta.RegraIds.All(ReferenciasRegra.EhIdValido))
        {
            throw new ErroValidacaoException("REGRA_ID_INVALIDO", $"Informe até {_opcoes.MaximoRegraIds} identificadores de regra válidos.");
        }

        if (!consulta.Obrigacoes.All(CodigosObrigacao.Todos.Contains))
        {
            throw new ErroValidacaoException("OBRIGACAO_DESCONHECIDA", "Código de obrigação desconhecido.");
        }

        if (consulta.Limite is { } limite && (limite < 1 || limite > _opcoes.LimiteMaximo))
        {
            throw new ErroValidacaoException("LIMITE_INVALIDO", $"O limite deve estar entre 1 e {_opcoes.LimiteMaximo}.");
        }
    }
}
