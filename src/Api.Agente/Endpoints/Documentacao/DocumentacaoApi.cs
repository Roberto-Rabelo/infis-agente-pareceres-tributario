using System.Text.Json.Nodes;
using Api.Agente.Application.Contratos;
using Api.Agente.Domain;
using Microsoft.OpenApi;

namespace Api.Agente.Endpoints.Documentacao;

/// <summary>
/// Documentação OpenAPI (Swagger) da API REST. Desligada por padrão e nunca servida em Production:
/// o documento e a interface são anônimos, e a constitution 5.1 só admite endpoints anônimos declarados.
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
                Title = "Api.Agente – infis-agente-parecer-tributário",
                Version = Documento,
                Description =
                    "Agente de cruzamento entre obrigações fiscais já validadas na origem. Recebe o lote normalizado, " +
                    "confere as pré-condições, executa as regras do catálogo, fundamenta os achados na base de conhecimento " +
                    "e emite o parecer técnico. O parecer é apoio técnico e não certifica conformidade fiscal.",
            });
            o.AddSecurityDefinition(EsquemaSeguranca, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT do provedor de identidade. Em Development: `dotnet user-jwts create --project src/Api.Agente`.",
            });
            o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(DocumentacaoApi).Assembly.GetName().Name}.xml"), includeControllerXmlComments: true);
            o.SupportNonNullableReferenceTypes();
            o.MapType<TipoObrigacao>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = [.. Codigos.CodigosTipoObrigacao.Select(c => (JsonNode)c)],
            });
            o.SchemaFilter<FiltroSchemaLote>();
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
            o.SwaggerEndpoint($"/swagger/{Documento}/swagger.json", $"Api.Agente {Documento}");
            o.DocumentTitle = "Api.Agente – Swagger";
            o.EnablePersistAuthorization();
            o.DisplayRequestDuration();
        });
        return app;
    }
}
