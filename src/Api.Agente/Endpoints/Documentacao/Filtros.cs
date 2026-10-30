using System.Reflection;
using System.Text.Json.Nodes;
using Api.Agente.Application.Contratos;
using Api.Agente.Application.Contratos.Entrada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Agente.Endpoints.Documentacao;

/// <summary>
/// Declara o corpo JSON de uma ação que lê o corpo bruto. O model binding não é usado para que o contrato de entrada
/// mantenha a desserialização estrita, o limite de tamanho e o erro padronizado.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CorpoJsonAttribute(Type tipo) : Attribute
{
    public Type Tipo { get; } = tipo;

    /// <summary>Arquivo JSON, relativo ao diretório da aplicação, usado como exemplo do corpo.</summary>
    public string? Exemplo { get; set; }
}

internal sealed class FiltroCorpoJson : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<CorpoJsonAttribute>() is not { } corpo)
        {
            return;
        }

        var midia = new OpenApiMediaType { Schema = context.SchemaGenerator.GenerateSchema(corpo.Tipo, context.SchemaRepository) };
        if (corpo.Exemplo is { } exemplo)
        {
            midia.Example = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, exemplo)));
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = midia },
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
            Description = "Identificador de correlação (1 a 64 caracteres: letras, dígitos, `_` e `-`). Fora do formato, um novo é gerado. Devolvido no cabeçalho da resposta.",
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

/// <summary>
/// O lote guarda as obrigações como JSON bruto para que cada uma seja validada isoladamente (PRE-001).
/// Na documentação, cada item é descrito pelo contrato da obrigação.
/// </summary>
internal sealed class FiltroSchemaLote : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema alvo || alvo.Properties is null)
        {
            return;
        }

        if (context.Type == typeof(LoteEntradaDto) && alvo.Properties.TryGetValue("obrigacoes", out var obrigacoes) && obrigacoes is OpenApiSchema lista)
        {
            lista.Items = context.SchemaGenerator.GenerateSchema(typeof(ObrigacaoDto), context.SchemaRepository);
            lista.Description = "Obrigações do lote. Uma obrigação fora do contrato não rejeita o lote: gera achado PRE-001.";
        }

        if (context.Type == typeof(ObrigacaoDto) && alvo.Properties.TryGetValue("tipo", out var tipo) && tipo is OpenApiSchema texto)
        {
            texto.Enum = [.. Codigos.CodigosTipoObrigacao.Select(c => (JsonNode)c)];
        }
    }
}
