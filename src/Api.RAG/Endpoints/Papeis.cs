namespace Api.RAG.Endpoints;

/// <summary>Papéis exigidos no token (D15 pendente): ingestão só para administradores (constitution 5.1).</summary>
public static class Papeis
{
    public const string Administrador = "rag.admin";
    public const string Consulta = "rag.consulta";
    public const string PoliticaAdministracao = "AdministracaoRag";
    public const string PoliticaConsulta = "ConsultaRag";
}
