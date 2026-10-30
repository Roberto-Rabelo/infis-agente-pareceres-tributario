using System.Text;
using System.Text.Json.Nodes;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Application.PreCondicoes;
using Api.Agente.Domain.Motor;
using Api.Agente.Infrastructure.Catalogo;

namespace Api.Agente.Tests.Fixtures;

/// <summary>Lotes fictícios do contrato de entrada v1. Nenhum dado real (P17).</summary>
public static class Lotes
{
    public const string CnpjMatriz = "11222333000181";
    public const string CnpjFilial = "11222333000262";
    public const string CnpjOutroContribuinte = "11444777000161";
    public const string CnpjAlfanumerico = "12ABC34501DE35";
    public const string ChaveNfe = "35260311222333000181550010000000011000000010";

    public static JsonObject Lote(params JsonObject[] obrigacoes) => Lote("11222333", "2026-03-01", "2026-03-31", obrigacoes);

    public static JsonObject Lote(string cnpj, string inicio, string fim, params JsonObject[] obrigacoes) => new()
    {
        ["versaoContrato"] = "1.0.0",
        ["loteId"] = "lote-teste",
        ["contribuinte"] = new JsonObject { ["cnpj"] = cnpj },
        ["periodo"] = new JsonObject { ["inicio"] = inicio, ["fim"] = fim },
        ["obrigacoes"] = new JsonArray(obrigacoes.Cast<JsonNode>().ToArray()),
    };

    public static JsonObject EfdIcmsIpi(string id = "efd-icms-1", string cnpj = CnpjMatriz, string recibo = "REC-1") =>
        Escrituracao(id, "EFD_ICMS_IPI", "020", cnpj, "2026-03-01", "2026-03-31", recibo, new JsonObject
        {
            ["0000"] = Registro(1, ("COD_VER", "020"), ("CNPJ", cnpj), ("DT_INI", "2026-03-01"), ("DT_FIN", "2026-03-31")),
        });

    public static JsonObject Ecd(string id = "ecd-1") =>
        Escrituracao(id, "ECD", "9.00", CnpjMatriz, "2026-01-01", "2026-12-31", "REC-ECD", new JsonObject
        {
            ["I010"] = Registro(2, ("IND_ESC", "G"), ("COD_VER_LC", "9.00")),
        });

    public static JsonObject EfdContribuicoes(string id = "efd-contrib-1") =>
        Escrituracao(id, "EFD_CONTRIBUICOES", "006", CnpjMatriz, "2026-03-01", "2026-03-31", "REC-CTB", []);

    public static JsonObject NFe(string id = "nfe-1", string chave = ChaveNfe) => new()
    {
        ["id"] = id,
        ["tipo"] = "NFE",
        ["versaoLayout"] = "4.00",
        ["contribuinte"] = new JsonObject { ["cnpj"] = CnpjMatriz, ["inscricaoEstadual"] = "110042490114", ["uf"] = "SP" },
        ["periodo"] = new JsonObject { ["inicio"] = "2026-03-10", ["fim"] = "2026-03-10" },
        ["finalidade"] = "ORIGINAL",
        ["identificacao"] = new JsonObject { ["chaveAcesso"] = chave },
        ["validacaoOrigem"] = Validacao("APROVADO"),
        ["registros"] = new JsonObject
        {
            ["ide"] = Registro(null, ("mod", "55"), ("serie", "1"), ("nNF", "1"), ("tpNF", "1")),
        },
    };

    public static JsonObject Escrituracao(
        string id, string tipo, string versao, string cnpj, string inicio, string fim, string recibo, JsonObject registros) => new()
    {
        ["id"] = id,
        ["tipo"] = tipo,
        ["versaoLayout"] = versao,
        ["contribuinte"] = new JsonObject { ["cnpj"] = cnpj, ["inscricaoEstadual"] = "110042490114", ["uf"] = "SP" },
        ["periodo"] = new JsonObject { ["inicio"] = inicio, ["fim"] = fim },
        ["finalidade"] = "ORIGINAL",
        ["identificacao"] = new JsonObject { ["recibo"] = recibo, ["hash"] = "HASH-" + recibo },
        ["validacaoOrigem"] = Validacao("APROVADO"),
        ["registros"] = registros,
    };

    public static JsonObject Validacao(string status, params (string Tipo, string Mensagem)[] ocorrencias) => new()
    {
        ["agente"] = "agente-validador-teste",
        ["versaoValidador"] = "1.0.0",
        ["dataValidacao"] = "2026-04-10T10:00:00-03:00",
        ["status"] = status,
        ["ocorrencias"] = new JsonArray(ocorrencias
            .Select(o => (JsonNode)new JsonObject { ["tipo"] = o.Tipo, ["codigo"] = "C1", ["mensagem"] = o.Mensagem, ["registro"] = "C100", ["linha"] = 10 })
            .ToArray()),
    };

    public static JsonArray Registro(long? linha, params (string Campo, string Valor)[] campos)
    {
        var objeto = new JsonObject();
        foreach (var (campo, valor) in campos)
        {
            objeto[campo] = valor;
        }

        return [new JsonObject { ["linha"] = linha, ["campos"] = objeto }];
    }

    public static byte[] Bytes(JsonNode lote) => Encoding.UTF8.GetBytes(lote.ToJsonString());

    /// <summary>Monta o serviço com o catálogo real (catalog/) e os validadores de produção, salvo se outros forem informados.</summary>
    public static ServicoAnalise Servico(params IValidadorCruzamento[] validadores)
    {
        var diretorio = CarregadorCatalogo.ResolverDiretorio("catalog");
        var leitor = new LeitorLote(new OpcoesEntrada());
        return new ServicoAnalise(
            CarregadorCatalogo.CarregarRegras(diretorio),
            new VerificadorPreCondicoes(CarregadorCatalogo.CarregarVersoes(diretorio)),
            new MotorRegras(validadores.Length > 0 ? validadores : ValidadoresNfeEfd.Todos()),
            ParametrosComparacao.Exata,
            leitor);
    }

    public static ResultadoAnalise Analisar(JsonObject lote)
    {
        var leitura = new LeitorLote(new OpcoesEntrada()).Ler(Bytes(lote));
        Assert.Null(leitura.Erro);
        return Servico().Analisar(leitura.Lote!);
    }

    public static AptidaoObrigacao Aptidao(ResultadoAnalise resultado, string id) =>
        resultado.Aptidao.Single(a => a.ObrigacaoId == id);
}
