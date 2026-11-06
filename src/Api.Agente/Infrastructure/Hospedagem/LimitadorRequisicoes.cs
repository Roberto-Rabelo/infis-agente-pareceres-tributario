using Microsoft.AspNetCore.RateLimiting;

namespace Api.Agente.Infrastructure.Hospedagem;

/// <summary>Rate limit opcional na borda (skill DevOps). Limite 0 = desligado, para não afetar testes locais.</summary>
public static class LimitadorRequisicoes
{
    public const string Politica = "sofia";
    public const string Secao = "RateLimiting";

    public static IServiceCollection AdicionarSeConfigurado(IServiceCollection servicos, IConfiguration configuracao)
    {
        var limite = configuracao.GetValue($"{Secao}:PermitLimitPorMinuto", 0);
        if (limite <= 0)
        {
            return servicos;
        }

        servicos.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.AddFixedWindowLimiter(Politica, janela =>
            {
                janela.PermitLimit = limite;
                janela.Window = TimeSpan.FromMinutes(1);
                janela.QueueLimit = 0;
            });
        });
        return servicos;
    }

    public static IApplicationBuilder UsarSeConfigurado(IApplicationBuilder app, IConfiguration configuracao)
    {
        if (configuracao.GetValue($"{Secao}:PermitLimitPorMinuto", 0) > 0)
        {
            app.UseRateLimiter();
        }

        return app;
    }

    public static TBuilder ComLimiteSeConfigurado<TBuilder>(TBuilder builder, IConfiguration configuracao)
        where TBuilder : IEndpointConventionBuilder
    {
        if (configuracao.GetValue($"{Secao}:PermitLimitPorMinuto", 0) > 0)
        {
            builder.RequireRateLimiting(Politica);
        }

        return builder;
    }
}
