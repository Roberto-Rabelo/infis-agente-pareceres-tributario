using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.RAG.Endpoints.Documentacao;

/// <summary>
/// Declara o corpo JSON de uma ação que lê o corpo bruto. O model binding não é usado para que o contrato de ingestão
/// mantenha a desserialização estrita, o limite de tamanho e o erro padronizado.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CorpoJsonAttribute(Type tipo) : Attribute
{
    public Type Tipo { get; } = tipo;
}

internal sealed class FiltroCorpoJson : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<CorpoJsonAttribute>() is not { } corpo)
        {
            return;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = context.SchemaGenerator.GenerateSchema(corpo.Tipo, context.SchemaRepository) },
            },
        };
    }
}

/// <summary>Exige o esquema Bearer e documenta 401/403 apenas nas ações que exigem autorização.</summary>
internal sealed class FiltroAutorizacao : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadados = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var autorizacoes = metadados.OfType<IAuthorizeData>().ToList();
        if (autorizacoes.Count == 0 || metadados.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(DocumentacaoApi.EsquemaSeguranca, context.Document)] = [] }];
        operation.Responses ??= [];
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Token ausente, inválido ou expirado." });
        if (autorizacoes.Any(a => a.Policy is not null || a.Roles is not null))
        {
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Token válido sem o papel exigido." });
        }
    }
}

/// <summary>Cabeçalho de correlação (P13) e erro interno padronizado (constitution 5.2), comuns a todas as ações.</summary>
internal sealed class FiltroRespostasComuns : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = Correlacao.Cabecalho,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Identificador de correlação (1 a 64 caracteres: letras, dígitos, `_` e `-`). Fora do formato, é descartado.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Pattern = "^[A-Za-z0-9_-]{1,64}$" },
        });

        operation.Responses ??= [];
        operation.Responses.TryAdd("500", new OpenApiResponse
        {
            Description = "Falha técnica, sem detalhes internos.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = context.SchemaGenerator.GenerateSchema(typeof(ErroPadronizado), context.SchemaRepository) },
            },
        });
    }
}
