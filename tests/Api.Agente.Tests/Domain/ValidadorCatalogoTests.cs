using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;
using static Api.Agente.Tests.Domain.RegrasDeTeste;

namespace Api.Agente.Tests.Domain;

public class ValidadorCatalogoTests
{
    [Fact]
    public void RegraCompleta_SemErros()
    {
        Assert.Empty(ValidadorCatalogo.Validar(new CatalogoRegras("1", [Cruzamento("CRZ-NFE-EFD-001")])));
    }

    [Fact]
    public void SemFonte_Rejeitada()
    {
        var regra = Cruzamento("CRZ-TST-001") with { Fontes = [] };

        Assert.Contains(ValidadorCatalogo.Validar(Catalogo(regra)), e => e.Contains("sem fonte oficial", StringComparison.Ordinal));
    }

    [Fact]
    public void AprovadaComFonteNaoConferida_Rejeitada()
    {
        var regra = Cruzamento("CRZ-TST-001") with { Fontes = [FonteConferida with { Conferida = false }] };

        Assert.Contains(ValidadorCatalogo.Validar(Catalogo(regra)), e => e.Contains("FR-012", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("IN SRF 104/1998 (conferir dispositivo)")]
    [InlineData("Verificar texto da exceção")]
    public void AprovadaComMarcaDeConferencia_Rejeitada(string localizacao)
    {
        var regra = Cruzamento("CRZ-TST-001") with { Fontes = [FonteConferida with { Localizacao = localizacao }] };

        Assert.Contains(ValidadorCatalogo.Validar(Catalogo(regra)), e => e.Contains("FR-013", StringComparison.Ordinal));
    }

    [Fact]
    public void PropostaComFonteNaoConferida_Aceita()
    {
        var regra = Cruzamento("CRZ-TST-001", status: StatusRegra.Proposta) with
        {
            Fontes = [FonteConferida with { Conferida = false, Localizacao = "(conferir)" }],
            Testes = [],
        };

        Assert.Empty(ValidadorCatalogo.Validar(Catalogo(regra)));
    }

    [Fact]
    public void AprovadaSemResponsavelOuSemTestes_Rejeitada()
    {
        var erros = ValidadorCatalogo.Validar(Catalogo(Cruzamento("CRZ-TST-001") with { AprovadoPor = null, Testes = [] }));

        Assert.Contains(erros, e => e.Contains("sem responsável", StringComparison.Ordinal));
        Assert.Contains(erros, e => e.Contains("sem testes", StringComparison.Ordinal));
    }

    [Fact]
    public void CruzamentoSemSeveridadePadraoOuComUmaObrigacao_Rejeitada()
    {
        var regra = Cruzamento("CRZ-TST-001", obrigacoes: [TipoObrigacao.NFe]) with { Severidades = new Dictionary<string, Severidade>() };

        var erros = ValidadorCatalogo.Validar(Catalogo(regra));

        Assert.Contains(erros, e => e.Contains("severidade padrão", StringComparison.Ordinal));
        Assert.Contains(erros, e => e.Contains("duas obrigações", StringComparison.Ordinal));
    }

    [Fact]
    public void IdDuplicadoOuForaDoPadrao_Rejeitado()
    {
        var erros = ValidadorCatalogo.Validar(Catalogo(Cruzamento("CRZ-TST-001"), Cruzamento("CRZ-TST-001"), Cruzamento("regra1")));

        Assert.Contains(erros, e => e.Contains("duplicado", StringComparison.Ordinal));
        Assert.Contains(erros, e => e.Contains("fora do padrão", StringComparison.Ordinal));
    }

    [Fact]
    public void VigenciaInvertida_Rejeitada()
    {
        var regra = Cruzamento("CRZ-TST-001", vigenciaInicio: new DateOnly(2026, 6, 1), vigenciaFim: new DateOnly(2026, 1, 1));

        Assert.Contains(ValidadorCatalogo.Validar(Catalogo(regra)), e => e.Contains("vigência", StringComparison.Ordinal));
    }

    [Fact]
    public void DependenciaInexistenteCircularOuDeFasePosterior_Rejeitada()
    {
        var a = Cruzamento("CRZ-TST-001", TipoRelacao.Identidade, dependeDe: ["CRZ-TST-002"]);
        var b = Cruzamento("CRZ-TST-002", TipoRelacao.Identidade, dependeDe: ["CRZ-TST-001"]);
        var c = Cruzamento("CRZ-TST-003", dependeDe: ["CRZ-TST-999"]);
        var d = Cruzamento("CRZ-TST-004", TipoRelacao.Parametro, dependeDe: ["CRZ-TST-001"]);

        var erros = ValidadorCatalogo.Validar(Catalogo(a, b, c, d));

        Assert.Contains(erros, e => e.Contains("circular", StringComparison.Ordinal));
        Assert.Contains(erros, e => e.Contains("inexistente CRZ-TST-999", StringComparison.Ordinal));
        Assert.Contains(erros, e => e.Contains("fase posterior", StringComparison.Ordinal));
    }

    [Fact]
    public void AssistidaPorIaSemJustificativa_Rejeitada()
    {
        var regra = Cruzamento("CRZ-TST-001") with { Implementacao = Implementacao.AssistidaIa };

        Assert.Contains(ValidadorCatalogo.Validar(Catalogo(regra)), e => e.Contains("sem justificativa", StringComparison.Ordinal));
    }

    private static CatalogoRegras Catalogo(params Regra[] regras) => new("1", regras);
}
