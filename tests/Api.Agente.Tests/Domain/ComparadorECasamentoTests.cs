using Api.Agente.Domain;
using Api.Agente.Domain.Casamento;
using Api.Agente.Domain.Motor;

namespace Api.Agente.Tests.Domain;

public class ComparadorECasamentoTests
{
    [Fact]
    public void Comparar_SemTolerancia_CentavoDiverge()
    {
        var resultado = ComparadorValores.Comparar(100.00m, 99.99m, ParametrosComparacao.Exata);

        Assert.True(resultado.Diverge);
        Assert.Equal(0.01m, resultado.Diferenca);
        Assert.Equal(0m, resultado.ToleranciaAplicada);
        Assert.Null(resultado.Material);
    }

    [Fact]
    public void Comparar_DentroDaTolerancia_NaoDiverge()
    {
        Assert.False(ComparadorValores.Comparar(100.00m, 99.99m, new ParametrosComparacao(0.01m, null)).Diverge);
    }

    [Fact]
    public void Comparar_ComMaterialidade_ClassificaMaterial()
    {
        var parametros = new ParametrosComparacao(0m, 1000m);

        Assert.False(ComparadorValores.Comparar(100m, 50m, parametros).Material);
        Assert.True(ComparadorValores.Comparar(5000m, 50m, parametros).Material);
    }

    [Fact]
    public void Limite_ValorAcimaDoLimite_Diverge()
    {
        Assert.True(ComparadorValores.Limite(100m, 100.01m, ParametrosComparacao.Exata).Diverge);
        Assert.False(ComparadorValores.Limite(100m, 80m, ParametrosComparacao.Exata).Diverge);
    }

    [Fact]
    public void Comparar_ToleranciaNegativa_Rejeitada()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ComparadorValores.Comparar(1m, 1m, new ParametrosComparacao(-1m, null)));
    }

    [Theory]
    [InlineData("35260311222333000181550010000000011000000010", true)]
    [InlineData("NFe35260311222333000181550010000000011000000010", true)]
    [InlineData("3526 0311 2223 3300 0181 5500 1000 0000 0110 0000 0010", true)]
    [InlineData("352603122ABC34501DE35550010000000011000000010", false)]
    [InlineData("35260312ABC34501DE35550010000000011000000010", true)]
    [InlineData("3526031122233300018155001000000001100000001", false)]
    public void ChaveAcesso_Normalizada(string entrada, bool valida)
    {
        Assert.Equal(valida, ChaveAcesso.TentarNormalizar(entrada, out var chave));
        if (valida)
        {
            Assert.Equal(44, chave.Length);
        }
    }

    [Fact]
    public void ChaveSecundaria_IgnoraZerosAEsquerda()
    {
        Cnpj.TentarCriar("11222333000181", out var cnpj, out _);
        var data = new DateOnly(2026, 3, 10);

        Assert.Equal(ChaveSecundaria.Criar(cnpj, "55", "001", "000123", data), ChaveSecundaria.Criar(cnpj, "55", "1", "123", data));
    }

    [Fact]
    public void Participante_ResolvidoPeloCnpjDo0150()
    {
        Cnpj.TentarCriar("11222333000181", out var cnpj, out _);
        var efd = new ObrigacaoRecebida(
            "efd", TipoObrigacao.EfdIcmsIpi, "020", cnpj, null, null, Periodo.Mes(2026, 3), Finalidade.Original,
            new Identificacao("R", null, null, null), null,
            new Dictionary<string, IReadOnlyList<OcorrenciaRegistro>>
            {
                ["0150"] =
                [
                    new(3, new Dictionary<string, string?> { ["COD_PART"] = "CLI01", ["CNPJ"] = "11.444.777/0001-61", ["CPF"] = null }),
                    new(4, new Dictionary<string, string?> { ["COD_PART"] = "PF01", ["CNPJ"] = "", ["CPF"] = "12345678909" }),
                ],
            });

        var resolvedor = new ResolvedorParticipantes(efd);

        Assert.Equal("11444777000161", resolvedor.Resolver("CLI01"));
        Assert.Equal("12345678909", resolvedor.Resolver("PF01"));
        Assert.Null(resolvedor.Resolver("INEXISTENTE"));
    }

    [Theory]
    [InlineData(1, PapelContribuinte.Emitente, 1, 0, false)]
    [InlineData(0, PapelContribuinte.Emitente, 0, 0, false)]
    [InlineData(1, PapelContribuinte.Destinatario, 0, 1, false)]
    [InlineData(0, PapelContribuinte.Destinatario, 1, 1, true)]
    public void TabelaSentido_ConformeNucleo1(int tpNF, PapelContribuinte papel, int indOper, int indEmit, bool conferirCfop)
    {
        Assert.Equal(new Sentido(indOper, indEmit, conferirCfop), TabelaSentido.Resolver(tpNF, papel));
    }

    [Fact]
    public void TabelaSentido_TpNfInvalido_Rejeitado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TabelaSentido.Resolver(2, PapelContribuinte.Emitente));
    }
}
