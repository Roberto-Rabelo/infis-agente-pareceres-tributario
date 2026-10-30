using Api.RAG.Application;
using Api.RAG.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Api.RAG.Tests.Fixtures;

/// <summary>Monta os serviços do RAG sobre dublês (sem provedor real e sem banco).</summary>
public sealed class Ambiente
{
    public const int Dimensao = 64;
    public const string Modelo = "modelo-teste";

    public Ambiente(IRepositorioConhecimento? repositorio = null, IRegistroConsumo? consumo = null, OpcoesConsulta? consulta = null)
    {
        Repositorio = repositorio ?? Memoria;
        var opcoesEmbeddings = Options.Create(new OpcoesEmbeddings { Provedor = "Falso", Modelo = Modelo, Dimensao = Dimensao, TamanhoLote = 4 });
        Provedor = new ProvedorEmbeddings(Gerador, opcoesEmbeddings, consumo ?? Memoria, TimeProvider.System, NullLogger<ProvedorEmbeddings>.Instance);
        Ingestao = new ServicoIngestao(Repositorio, Provedor, Options.Create(new OpcoesSegmentacao { MaximoCaracteres = 600 }), TimeProvider.System);
        Consulta = new ServicoConsulta(Repositorio, Provedor, opcoesEmbeddings, Options.Create(consulta ?? new OpcoesConsulta()));
    }

    public RepositorioEmMemoria Memoria { get; } = new();

    public IRepositorioConhecimento Repositorio { get; }

    public GeradorFalso Gerador { get; } = new(Dimensao);

    public ProvedorEmbeddings Provedor { get; }

    public ServicoIngestao Ingestao { get; }

    public ServicoConsulta Consulta { get; }

    public static ComandoIngestao Comando(
        string conteudo = Documentos.Cruzamentos,
        string chave = "docs/03-regras-cruzamento.md",
        string versao = "2026-10-29",
        TipoDocumento tipo = TipoDocumento.BaseRegrasInterna,
        DateOnly? vigenciaInicio = null,
        DateOnly? vigenciaFim = null,
        string origem = "docs/03-regras-cruzamento.md",
        IReadOnlyList<string>? obrigacoes = null,
        string formato = "markdown") => new(
            chave,
            "03 – Regras de Cruzamento entre Obrigações",
            tipo,
            "Projeto infis-agente-parecer-tributário",
            versao,
            null,
            vigenciaInicio,
            vigenciaFim,
            origem,
            obrigacoes ?? [],
            formato,
            conteudo,
            "corr-teste");

    public static ConsultaConhecimento Pergunta(
        string? pergunta = null,
        IReadOnlyList<string>? regraIds = null,
        IReadOnlyList<string>? obrigacoes = null,
        IReadOnlyList<TipoDocumento>? tipos = null,
        DateOnly? dataReferencia = null,
        int? limite = null) =>
        new(pergunta, regraIds ?? [], obrigacoes ?? [], tipos ?? [], dataReferencia, limite, "corr-teste");
}

/// <summary>Trechos sintéticos no formato de docs/03 (não são regras aprovadas).</summary>
public static class Documentos
{
    public const string Cruzamentos = """
        # 03 – Regras de Cruzamento entre Obrigações

        Cada regra indica origem → destino, a condição e as hipóteses legítimas de divergência.

        ## 2. NF-e/NFC-e → EFD ICMS/IPI

        | ID | Condição | Evidência | Hipóteses legítimas | Severidade |
        |---|---|---|---|---|
        | CRZ-NFE-EFD-001 | Toda NF-e de emissão própria autorizada deve estar no C100 com IND_EMIT = 0 e CHV_NFE igual. | [MANUAL] | NF-e cancelada (ver CRZ-NFE-EFD-002) | ESCRITURACAO_GRAVE |
        | CRZ-NFE-EFD-002 | NF-e com evento de cancelamento homologado deve estar com COD_SIT = 02. | [MANUAL] | — | ESCRITURACAO_GRAVE |

        ## 7. ECD → ECF

        | ID | Condição | Evidência | Hipóteses legítimas | Severidade |
        |---|---|---|---|---|
        | CRZ-ECD-ECF-001 | Saldos finais da ECD devem ser recuperados no bloco K da ECF. | [MANUAL] | Ajustes de transição | ESCRITURACAO_GRAVE |
        """;
}
