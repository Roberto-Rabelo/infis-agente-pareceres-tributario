using System.Text.RegularExpressions;
using Api.RAG.Domain;
using Microsoft.Extensions.Options;

namespace Api.RAG.Application;

public sealed record ComandoIngestao(
    string Chave,
    string Titulo,
    TipoDocumento Tipo,
    string Emissor,
    string Versao,
    DateOnly? DataPublicacao,
    DateOnly? VigenciaInicio,
    DateOnly? VigenciaFim,
    string Origem,
    IReadOnlyList<string> Obrigacoes,
    string Formato,
    string Conteudo,
    string? CorrelacaoId);

public enum SituacaoIngestao
{
    Criado,
    JaExistente,
}

public sealed record ResultadoIngestao(SituacaoIngestao Situacao, Documento Documento, int QuantidadeTrechos, Guid? SubstituiuDocumentoId);

/// <summary>
/// Critérios de ingestão (P7): mesma chave + versão + hash é idempotente; mesma chave + versão com conteúdo diferente
/// é conflito; nova versão substitui a ativa, que permanece consultável como histórico (constitution 4.1).
/// </summary>
public sealed partial class ServicoIngestao(
    IRepositorioConhecimento repositorio,
    ProvedorEmbeddings embeddings,
    IOptions<OpcoesSegmentacao> segmentacao,
    TimeProvider relogio)
{
    public const string FormatoMarkdown = "markdown";
    public const string FormatoTexto = "texto";

    public async Task<ResultadoIngestao> IngerirAsync(ComandoIngestao comando, CancellationToken cancelamento)
    {
        Validar(comando);
        var hash = SegmentadorMarkdown.Hash(comando.Conteudo);

        var mesmaVersao = await repositorio.ObterPorChaveVersaoAsync(comando.Chave, comando.Versao, cancelamento).ConfigureAwait(false);
        if (mesmaVersao is not null)
        {
            return mesmaVersao.HashConteudo == hash
                ? new ResultadoIngestao(SituacaoIngestao.JaExistente, mesmaVersao, 0, null)
                : throw new ConflitoVersaoException("A versão informada já foi ingerida com conteúdo diferente; publique uma nova versão.");
        }

        var trechos = SegmentadorMarkdown.Segmentar(comando.Conteudo, segmentacao.Value.MaximoCaracteres);
        if (trechos.Count == 0)
        {
            throw new ErroValidacaoException("DOCUMENTO_SEM_CONTEUDO", "O documento não produziu trechos.");
        }

        var textos = trechos.Select(t => TextoParaEmbedding(comando.Titulo, t)).ToList();
        var vetores = await embeddings.GerarAsync(textos, "INGESTAO", comando.CorrelacaoId, cancelamento).ConfigureAwait(false);

        var ativo = await repositorio.ObterAtivoAsync(comando.Chave, cancelamento).ConfigureAwait(false);
        var documento = new Documento(
            Guid.CreateVersion7(),
            comando.Chave,
            comando.Titulo.Trim(),
            comando.Tipo,
            comando.Emissor.Trim(),
            comando.Versao.Trim(),
            comando.DataPublicacao,
            comando.VigenciaInicio,
            comando.VigenciaFim,
            comando.Origem.Trim(),
            hash,
            comando.Obrigacoes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList(),
            StatusDocumento.Ativo,
            embeddings.Modelo,
            embeddings.Dimensao,
            relogio.GetUtcNow(),
            null);

        var indexados = trechos
            .Select((t, i) => new TrechoIndexado(Guid.CreateVersion7(), ComObrigacoesDoDocumento(t, documento), vetores.Vetores[i]))
            .ToList();
        await repositorio.SalvarAsync(documento, indexados, ativo?.Id, cancelamento).ConfigureAwait(false);
        return new ResultadoIngestao(SituacaoIngestao.Criado, documento, indexados.Count, ativo?.Id);
    }

    /// <summary>O título e a seção entram no texto vetorizado para dar contexto a linhas de tabela isoladas.</summary>
    public static string TextoParaEmbedding(string titulo, Trecho trecho) => $"{titulo}\n{trecho.Secao}\n\n{trecho.Texto}";

    /// <summary>Trecho sem obrigação identificada herda as do documento; documento sem obrigações gera trecho geral.</summary>
    private static Trecho ComObrigacoesDoDocumento(Trecho trecho, Documento documento) =>
        trecho.Obrigacoes.Count > 0 || documento.Obrigacoes.Count == 0 ? trecho : trecho with { Obrigacoes = documento.Obrigacoes };

    private static void Validar(ComandoIngestao comando)
    {
        Exigir(Chave().IsMatch(comando.Chave ?? string.Empty), "CHAVE_INVALIDA", "A chave deve ter de 1 a 200 caracteres [a-z0-9._/-].");
        Exigir(Preenchido(comando.Titulo, 300), "TITULO_INVALIDO", "Título obrigatório (até 300 caracteres).");
        Exigir(Preenchido(comando.Emissor, 200), "EMISSOR_INVALIDO", "Emissor obrigatório (até 200 caracteres).");
        Exigir(Preenchido(comando.Versao, 100), "VERSAO_INVALIDA", "Versão obrigatória (até 100 caracteres).");
        Exigir(Preenchido(comando.Origem, 1000), "ORIGEM_INVALIDA", "Origem (URL ou referência) obrigatória.");
        Exigir(Enum.IsDefined(comando.Tipo), "TIPO_INVALIDO", "Tipo de documento desconhecido.");
        Exigir(comando.Formato is FormatoMarkdown or FormatoTexto, "FORMATO_NAO_SUPORTADO", "Formatos aceitos: markdown, texto.");
        Exigir(!string.IsNullOrWhiteSpace(comando.Conteudo), "DOCUMENTO_SEM_CONTEUDO", "Conteúdo obrigatório.");
        Exigir(comando.Obrigacoes.All(CodigosObrigacao.Todos.Contains), "OBRIGACAO_DESCONHECIDA", "Código de obrigação desconhecido.");
        Exigir(
            comando.VigenciaInicio is null || comando.VigenciaFim is null || comando.VigenciaInicio <= comando.VigenciaFim,
            "VIGENCIA_INVALIDA",
            "Início da vigência posterior ao fim.");
    }

    private static bool Preenchido(string? valor, int maximo) => !string.IsNullOrWhiteSpace(valor) && valor.Length <= maximo;

    private static void Exigir(bool condicao, string codigo, string mensagem)
    {
        if (!condicao)
        {
            throw new ErroValidacaoException(codigo, mensagem);
        }
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._/-]{0,199}$")]
    private static partial Regex Chave();
}
