using Api.Agente.Domain;

namespace Api.Agente.Tests.Domain;

public class CnpjPeriodoTests
{
    [Theory]
    [InlineData("11222333000181", "11222333000181")]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("12.ABC.345/01DE-35", "12ABC34501DE35")]
    [InlineData("12abc34501de35", "12ABC34501DE35")]
    public void Cnpj_Valido_NormalizadoComoTexto(string entrada, string esperado)
    {
        Assert.True(Cnpj.TentarCriar(entrada, out var cnpj, out _));
        Assert.Equal(esperado, cnpj.Valor);
        Assert.Equal(esperado[..8], cnpj.Raiz);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("11222333000182")]
    [InlineData("12ABC34501DE36")]
    [InlineData("1122233300018")]
    [InlineData("11111111111111")]
    [InlineData("12ABC34501DEA5")]
    [InlineData("12ABC3450#DE35")]
    public void Cnpj_Invalido_Rejeitado(string? entrada)
    {
        Assert.False(Cnpj.TentarCriar(entrada, out _, out var erro));
        Assert.NotNull(erro);
    }

    [Theory]
    [InlineData("11222333", "11222333")]
    [InlineData("11.222.333/0001-81", "11222333")]
    [InlineData("12ABC345", "12ABC345")]
    public void Raiz_AceitaRaizOuCnpjCompleto(string entrada, string esperada)
    {
        Assert.True(Cnpj.TentarObterRaiz(entrada, out var raiz, out _));
        Assert.Equal(esperada, raiz);
    }

    [Fact]
    public void Periodo_Ano_TemDozeMeses()
    {
        var meses = Periodo.Ano(2026).Meses();

        Assert.Equal(12, meses.Count);
        Assert.Equal(new DateOnly(2026, 12, 1), meses[^1]);
    }

    [Fact]
    public void Periodo_SituacaoEspecial_SoMesesDoPeriodo()
    {
        var especial = new Periodo(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 15));

        Assert.Equal(4, especial.Meses().Count);
        Assert.True(especial.SobrepoeA(Periodo.Mes(2026, 4)));
        Assert.False(especial.SobrepoeA(Periodo.Mes(2026, 5)));
        Assert.False(especial.Contem(Periodo.Mes(2026, 4)));
    }

    [Fact]
    public void Periodo_FimAntesDoInicio_Rejeitado()
    {
        Assert.False(Periodo.TentarCriar(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 31), out _));
        Assert.Throws<ArgumentException>(() => new Periodo(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void Periodo_MesDeFevereiro_TerminaNoUltimoDia()
    {
        Assert.Equal(new DateOnly(2028, 2, 29), Periodo.Mes(2028, 2).Fim);
    }

    [Theory]
    [InlineData("11222333000181", "11**********81")]
    [InlineData("11222333", "11****33")]
    [InlineData("123", "***")]
    public void Mascaramento_OcultaMiolo(string valor, string esperado)
    {
        Assert.Equal(esperado, Mascaramento.Documento(valor));
    }
}
