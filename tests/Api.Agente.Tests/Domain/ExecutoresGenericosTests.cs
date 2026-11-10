using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;
using Api.Agente.Domain.Motor;
using static Api.Agente.Tests.Domain.RegrasDeTeste;

namespace Api.Agente.Tests.Domain;

/// <summary>Spec 004, Fase 1: acessor de elementos (D404) e executores genéricos (D405) com regras sintéticas.</summary>
public class ExecutoresGenericosTests
{
    private const string Chave = "35260311222333000181550010000000011000000010";
    private const string OutraChave = "35260311222333000181550010000000021000000020";

    private static readonly CatalogoParametros Parametros = new(new Dictionary<string, ParametroRegra>
    {
        ["TOL_TESTE"] = new("TOL_TESTE", "decimal", true, 0.05m, null),
        ["TOL_SEM_VALOR"] = new("TOL_SEM_VALOR", "decimal", false, null, null),
        ["JANELA"] = new("JANELA", "int", true, 2m, null),
    });

    [Theory]
    [InlineData("C100.VL_DOC", "C100", "VL_DOC")]
    [InlineData("C100.VL_BC_ICMS, VL_ICMS", "C100", "VL_BC_ICMS,VL_ICMS")]
    public void Elemento_Sped(string texto, string registro, string campos)
    {
        var (elemento, motivo) = InterpretadorRegra.Elemento(TipoObrigacao.EfdIcmsIpi, texto);

        Assert.Null(motivo);
        Assert.Equal(registro, elemento!.Grupo);
        Assert.Equal(campos.Split(','), elemento.Campos.Select(c => c.Nome));
    }

    [Theory]
    [InlineData("total/ICMSTot/vNF", "ICMSTot", "vNF", null)]
    [InlineData("ICMSTot/vBC, vICMS", "ICMSTot", "vBC,vICMS", null)]
    [InlineData("ide/mod, serie, nNF, dhEmi(data)", "ide", "mod,serie,nNF,dhEmi", "data")]
    public void Elemento_Xml(string texto, string grupo, string campos, string? modificadorUltimo)
    {
        var (elemento, motivo) = InterpretadorRegra.Elemento(TipoObrigacao.NFe, texto);

        Assert.Null(motivo);
        Assert.Equal(grupo, elemento!.Grupo);
        Assert.Equal(campos.Split(','), elemento.Campos.Select(c => c.Nome));
        Assert.Equal(modificadorUltimo, elemento.Campos[^1].Modificador);
    }

    [Fact]
    public void Elemento_ChaveDoDocumento()
    {
        var (elemento, _) = InterpretadorRegra.Elemento(TipoObrigacao.NFe, "infNFe/@Id");

        Assert.True(elemento!.ChaveDoDocumento);
    }

    [Theory]
    [InlineData(TipoObrigacao.EfdIcmsIpi, "C100.COD_PART -> 0150.CNPJ")]
    [InlineData(TipoObrigacao.NFe, "vST > 0; dest/enderDest/UF")]
    [InlineData(TipoObrigacao.EfdIcmsIpi, "0000 (ano)")]
    [InlineData(TipoObrigacao.NFe, "C100.VL_DOC")]
    public void Elemento_ForaDoFormato_DevolveMotivo(TipoObrigacao tipo, string texto)
    {
        var (elemento, motivo) = InterpretadorRegra.Elemento(tipo, texto);

        Assert.Null(elemento);
        Assert.NotNull(motivo);
    }

    [Fact]
    public void Filtros_Suportados()
    {
        var (filtros, motivo) = InterpretadorRegra.Filtros("IND_EMIT=0; COD_SIT in (00, 01, 06); autorizada; sem 110111; C100.IND_OPER=1");

        Assert.Null(motivo);
        Assert.Collection(
            filtros!,
            f => Assert.Equal(new FiltroElemento.Igual("IND_EMIT", "0"), f),
            f => Assert.Equal("00,01,06", string.Join(',', Assert.IsType<FiltroElemento.Em>(f).Valores)),
            f => Assert.IsType<FiltroElemento.NaoCancelado>(f),
            f => Assert.IsType<FiltroElemento.NaoCancelado>(f),
            f => Assert.Equal(new FiltroElemento.Igual("C100.IND_OPER", "1"), f));
    }

    [Theory]
    [InlineData("emit/CNPJ = 0000.CNPJ")]
    [InlineData("CFOP de receita (PARAM:TABELA_CFOP_RECEITA)")]
    [InlineData("todos os estabelecimentos")]
    public void Filtro_TextoLivre_DevolveMotivo(string texto)
    {
        var (filtros, motivo) = InterpretadorRegra.Filtros(texto);

        Assert.Null(filtros);
        Assert.Contains(texto, motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void Identidade_ValoresIguaisDentroDaTolerancia_SemAchados()
    {
        var regra = Identidade(tolerancia: "PARAM:TOL_TESTE");

        var achados = Executar(regra, Nota(Chave, ("ICMSTot", "vNF", "100.00")), Efd(C100(Chave, ("VL_DOC", "100.04"))));

        Assert.Empty(achados);
    }

    [Fact]
    public void Identidade_ValoresDivergentes_InconsistenciaComValoresETolerancia()
    {
        var regra = Identidade(tolerancia: "PARAM:TOL_TESTE");

        var achado = Assert.Single(Executar(regra, Nota(Chave, ("ICMSTot", "vNF", "1000.00")), Efd(C100(Chave, ("VL_DOC", "900.00")))));

        Assert.Equal(Natureza.InconsistenciaCruzamento, achado.Natureza);
        Assert.Equal(Severidade.ErroGrave, achado.Severidade);
        Assert.Equal((900m, 1000m, 0.05m), (achado.ValorOrigem!.Value, achado.ValorDestino!.Value, achado.ToleranciaAplicada!.Value));
        Assert.Contains(Chave, achado.Descricao, StringComparison.Ordinal);
        Assert.Contains("VL_DOC", achado.Descricao, StringComparison.Ordinal);
        Assert.Contains("vNF", achado.Descricao, StringComparison.Ordinal);
        Assert.Equal(["efd", "nfe"], achado.Evidencias.Select(e => e.ObrigacaoId));
    }

    [Fact]
    public void Identidade_CampoAusente_AusenciaDeEvidencias()
    {
        var achado = Assert.Single(Executar(Identidade(), Nota(Chave), Efd(C100(Chave, ("VL_DOC", "900.00")))));

        Assert.Equal(Natureza.AusenciaEvidencias, achado.Natureza);
    }

    [Fact]
    public void Identidade_DocumentoCanceladoOuLinhaIrregular_ForaDoUniverso()
    {
        var cancelada = Nota(Chave, ("ICMSTot", "vNF", "1000.00")) with
        {
            Registros = new Dictionary<string, IReadOnlyList<OcorrenciaRegistro>>
            {
                ["ICMSTot"] = [Linha(("vNF", "1000.00"))],
                ["evento"] = [Linha(("tpEvento", "110111"))],
            },
        };
        var irregular = Efd(C100(OutraChave, ("VL_DOC", "1.00"), ("COD_SIT", "02")));

        Assert.Empty(Executar(Identidade(), cancelada, Efd(C100(Chave, ("VL_DOC", "900.00")))));
        Assert.Empty(Executar(Identidade(), Nota(OutraChave, ("ICMSTot", "vNF", "1000.00")), irregular));
    }

    [Fact]
    public void Identidade_FiltroDaOrigem_RestringeUniverso()
    {
        var regra = Identidade(filtroOrigem: "IND_EMIT=0");
        var terceiros = Efd(C100(Chave, ("VL_DOC", "900.00"), ("IND_EMIT", "1")));

        Assert.Empty(Executar(regra, Nota(Chave, ("ICMSTot", "vNF", "1000.00")), terceiros));
    }

    [Fact]
    public void Limite_MenorOuIgual_SoAcusaExcesso()
    {
        var regra = Limite("MENOR_OU_IGUAL");

        Assert.Empty(Executar(regra, Nota(Chave, ("ICMSTot", "vICMS", "180.00")), Efd(C100(Chave, ("VL_ICMS", "0.00")))));
        var achado = Assert.Single(Executar(regra, Nota(Chave, ("ICMSTot", "vICMS", "60.00")), Efd(C100(Chave, ("VL_ICMS", "250.00")))));
        Assert.Equal((60m, 250m), (achado.ValorOrigem!.Value, achado.ValorDestino!.Value));
    }

    [Fact]
    public void Limite_MaiorOuIgualComDatas()
    {
        var regra = Limite("MAIOR_OU_IGUAL", origem: "ide/dhEmi", destino: "C100.DT_E_S");

        Assert.Empty(Executar(regra, Nota(Chave, ("ide", "dhEmi", "2026-03-10T10:00:00-03:00")), Efd(C100(Chave, ("DT_E_S", "2026-03-10")))));
        Assert.Single(Executar(regra, Nota(Chave, ("ide", "dhEmi", "2026-03-10T10:00:00-03:00")), Efd(C100(Chave, ("DT_E_S", "2026-03-09")))));
    }

    [Fact]
    public void Presenca_SemCorrespondente_Inconsistencia_ExcluidoNao()
    {
        var regra = Presenca(filtrosExclusao: ["EXC-901"]);
        var contexto = Contexto(Nota(Chave), Nota(OutraChave), Efd(C100(OutraChave)));

        var achado = Assert.Single(new ExecutorPresenca(regra.Id, InterpretadorRegra.Interpretar(regra, Parametros).Regra!).Validar(contexto, regra));
        Assert.Contains(Chave, achado.Descricao, StringComparison.Ordinal);

        contexto.RegistrarExclusao("EXC-901", Chave, "Exclusão de teste.");
        Assert.Empty(new ExecutorPresenca(regra.Id, InterpretadorRegra.Interpretar(regra, Parametros).Regra!).Validar(contexto, regra));
    }

    [Fact]
    public void Exclusao_RegistraDocumentosQueAtendemAoFiltro()
    {
        var regra = Cruzamento("EXC-901", TipoRelacao.Exclusividade, obrigacoes: [TipoObrigacao.NFe]) with
        {
            Declaracao = new DeclaracaoRegra { Origem = new LadoRegra(TipoObrigacao.NFe, "NFE", "infNFe/@Id", "evento 110111 homologado", null), Operador = "FILTRA" },
        };
        var cancelada = Nota(Chave) with
        {
            Registros = new Dictionary<string, IReadOnlyList<OcorrenciaRegistro>> { ["evento"] = [Linha(("tpEvento", "110111"))] },
        };
        var contexto = Contexto(cancelada, Nota(OutraChave));

        var (validador, motivo) = ExecutoresGenericos.Criar(regra, Parametros);
        validador!.Validar(contexto, regra);

        Assert.Null(motivo);
        Assert.True(contexto.EstaExcluido(Chave));
        Assert.False(contexto.EstaExcluido(OutraChave));
    }

    [Theory]
    [InlineData("PARAM:TOL_SEM_VALOR", "sem valor")]
    [InlineData("PARAM:JANELA", "não é um parâmetro decimal")]
    [InlineData("PARAM:INEXISTENTE", "não é um parâmetro decimal")]
    public void Tolerancia_SemValorOuNaoDecimal_NaoInterpreta(string tolerancia, string trecho)
    {
        var (_, motivo) = ExecutoresGenericos.Criar(Identidade(tolerancia: tolerancia), Parametros);

        Assert.Contains(trecho, motivo, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("CNPJ_BASE, ANO")]
    [InlineData("CHV_NFE, NUM_ITEM=nItem")]
    public void ChaveComposta_NaoInterpreta(string chave)
    {
        var regra = Identidade() with { Declaracao = Identidade().Declaracao! with { ChaveJuncao = chave.Split(", ") } };

        Assert.Contains("chave de junção", ExecutoresGenericos.Criar(regra, Parametros).Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void PresencaComCondicaoEmTextoLivre_ExigeValidadorDedicado()
    {
        var regra = Presenca() with { Declaracao = Presenca().Declaracao! with { CondicaoDescritiva = "Período de alocação: mês de dhEmi." } };

        Assert.Contains("texto livre", ExecutoresGenericos.Criar(regra, Parametros).Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void Agregacao_SemExecutorGenericoNaFase1()
    {
        var regra = Cruzamento("M3-99", TipoRelacao.Agregacao) with { Declaracao = new DeclaracaoRegra { Operador = "SOMA_IGUAL" } };

        Assert.Contains("sem executor genérico", ExecutoresGenericos.Criar(regra, Parametros).Motivo, StringComparison.Ordinal);
    }

    private static Regra Identidade(string? tolerancia = null, string? filtroOrigem = null) =>
        Cruzamento("M1-I93", TipoRelacao.Identidade) with
        {
            Declaracao = new DeclaracaoRegra
            {
                Origem = new LadoRegra(TipoObrigacao.EfdIcmsIpi, "EFD_ICMS_IPI", "C100.VL_DOC", filtroOrigem, null),
                Destino = new LadoRegra(TipoObrigacao.NFe, "NFE", "total/ICMSTot/vNF", null, null),
                ChaveJuncao = ["CHV_NFE"],
                Operador = "IGUAL",
                Tolerancia = tolerancia,
            },
        };

    private static Regra Limite(string operador, string origem = "ICMSTot/vICMS", string destino = "C100.VL_ICMS") =>
        Cruzamento("M1-I94", TipoRelacao.Limite) with
        {
            Declaracao = new DeclaracaoRegra
            {
                Origem = new LadoRegra(TipoObrigacao.NFe, "NFE", origem, null, null),
                Destino = new LadoRegra(TipoObrigacao.EfdIcmsIpi, "EFD_ICMS_IPI", destino, null, null),
                ChaveJuncao = ["CHV_NFE"],
                Operador = operador,
            },
        };

    private static Regra Presenca(IReadOnlyList<string>? filtrosExclusao = null) =>
        Cruzamento("M1-P93", TipoRelacao.PresencaDireta) with
        {
            Declaracao = new DeclaracaoRegra
            {
                Origem = new LadoRegra(TipoObrigacao.NFe, "NFE", "infNFe/@Id", "autorizada", null),
                Destino = new LadoRegra(TipoObrigacao.EfdIcmsIpi, "EFD_ICMS_IPI", "C100.CHV_NFE", null, null),
                ChaveJuncao = ["CHV_NFE"],
                Operador = "EXISTE",
                FiltrosExclusao = filtrosExclusao ?? [],
            },
        };

    private static IReadOnlyList<Achado> Executar(Regra regra, params ObrigacaoRecebida[] obrigacoes)
    {
        var (validador, motivo) = ExecutoresGenericos.Criar(regra, Parametros);
        Assert.True(validador is not null, motivo);
        return validador.Validar(Contexto(obrigacoes), regra);
    }

    private static ObrigacaoRecebida Nota(string chave, params (string Grupo, string Campo, string Valor)[] campos) =>
        Obrigacao(campos.Length == 0 && chave == OutraChave ? "nfe-2" : "nfe", TipoObrigacao.NFe) with
        {
            Identificacao = new Identificacao(null, null, null, chave),
            Registros = campos.GroupBy(c => c.Grupo).ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<OcorrenciaRegistro>)[Linha([.. g.Select(c => (c.Campo, c.Valor))])]),
        };

    private static ObrigacaoRecebida Efd(params OcorrenciaRegistro[] c100) =>
        Obrigacao("efd", TipoObrigacao.EfdIcmsIpi) with
        {
            Registros = new Dictionary<string, IReadOnlyList<OcorrenciaRegistro>> { ["C100"] = c100 },
        };

    private static OcorrenciaRegistro C100(string chave, params (string Campo, string Valor)[] campos) =>
        Linha([("CHV_NFE", chave), ("COD_SIT", "00"), ("IND_EMIT", "0"), .. campos]);

    private static OcorrenciaRegistro Linha(params (string Campo, string Valor)[] campos)
    {
        var dicionario = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (campo, valor) in campos)
        {
            dicionario[campo] = valor;
        }

        return new OcorrenciaRegistro(7, dicionario);
    }
}
