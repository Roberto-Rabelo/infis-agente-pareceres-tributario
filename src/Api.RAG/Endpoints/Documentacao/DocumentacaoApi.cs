using Microsoft.OpenApi;

namespace Api.RAG.Endpoints.Documentacao;

/// <summary>
/// Documentação OpenAPI (Swagger) da API REST de administração. Desligada por padrão e nunca servida em Production:
/// o documento e a interface são anônimos, e a constitution 5.1 só admite endpoints anônimos declarados.
/// O servidor MCP (<c>/mcp</c>) não é REST e fica fora deste documento.
/// </summary>
public static class DocumentacaoApi
{
    public const string ChaveHabilitado = "Swagger:Habilitado";
    public const string Documento = "v1";
    public const string EsquemaSeguranca = "Bearer";

    public static IServiceCollection AdicionarDocumentacaoApi(this IServiceCollection servicos)
    {
        servicos.AddSwaggerGen(o =>
        {
            o.SwaggerDoc(Documento, new OpenApiInfo
            {
                Title = "Api.RAG – infis-agente-parecer-tributário",
                Version = Documento,
                Description =
                    "Administração da base de conhecimento (documentos oficiais e base interna de regras), com segmentação, " +
                    "embeddings e PgVector. A consulta pelos agentes é feita pelo servidor MCP em `/mcp`, fora deste documento.",
            });
            o.AddSecurityDefinition(EsquemaSeguranca, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT com papel `rag.admin`. Em Development: `dotnet user-jwts create --project src/Api.RAG --role rag.admin`.",
            });
            o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(DocumentacaoApi).Assembly.GetName().Name}.xml"), includeControllerXmlComments: true);
            o.SupportNonNullableReferenceTypes();
            o.OperationFilter<FiltroCorpoJson>();
            o.OperationFilter<FiltroAutorizacao>();
            o.OperationFilter<FiltroRespostasComuns>();
        });
        return servicos;
    }

    public static WebApplication UsarDocumentacaoApi(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(ChaveHabilitado) || app.Environment.IsProduction())
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint($"/swagger/{Documento}/swagger.json", $"Api.RAG {Documento}");
            o.DocumentTitle = "Api.RAG – Swagger";
            o.EnablePersistAuthorization();
            o.DisplayRequestDuration();
        });
        return app;
    }
}
