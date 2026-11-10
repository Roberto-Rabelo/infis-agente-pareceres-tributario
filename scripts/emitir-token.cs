// Emite um JWT HS256 para o modo de chave simétrica (feature 003, ambiente de teste sem IdP).
// Uso: dotnet run scripts/emitir-token.cs -- <sujeito> <dias> [papel ...]
// Lê AUTENTICACAO_CHAVE (base64, >= 32 bytes), AUTENTICACAO_EMISSOR e AUTENTICACAO_AUDIENCE do ambiente.
// Os mesmos valores vão para Autenticacao__ChaveSimetrica, Autenticacao__Emissor e Autenticacao__Audience na Vercel.
// Sem AOT: o padrão dos file-based apps puxa o ILCompiler, que o NuGet audit barra como erro (TreatWarningsAsErrors).
#:property PublishAot=false
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length < 2 || !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out var dias) || dias < 1)
{
    Console.Error.WriteLine("Uso: dotnet run scripts/emitir-token.cs -- <sujeito> <dias> [papel ...]");
    return 2;
}

var chave = Convert.FromBase64String(Ambiente("AUTENTICACAO_CHAVE"));
if (chave.Length < 32)
{
    Console.Error.WriteLine("AUTENTICACAO_CHAVE precisa de pelo menos 32 bytes.");
    return 2;
}

var agora = DateTimeOffset.UtcNow;
var carga = new Dictionary<string, object>
{
    ["iss"] = Ambiente("AUTENTICACAO_EMISSOR"),
    ["aud"] = Ambiente("AUTENTICACAO_AUDIENCE"),
    ["sub"] = args[0],
    ["iat"] = agora.ToUnixTimeSeconds(),
    ["nbf"] = agora.ToUnixTimeSeconds(),
    ["exp"] = agora.AddDays(dias).ToUnixTimeSeconds(),
    ["jti"] = Guid.NewGuid().ToString("N"),
};
if (args.Length > 2)
{
    carga["role"] = args[2..];
}

var conteudo = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string> { ["alg"] = "HS256", ["typ"] = "JWT" }))
    + "." + Base64Url(JsonSerializer.SerializeToUtf8Bytes(carga));
Console.WriteLine(conteudo + "." + Base64Url(HMACSHA256.HashData(chave, Encoding.ASCII.GetBytes(conteudo))));
return 0;

static string Ambiente(string nome) => Environment.GetEnvironmentVariable(nome) is { Length: > 0 } valor
    ? valor
    : throw new InvalidOperationException($"Defina {nome}.");

static string Base64Url(byte[] dados) => Convert.ToBase64String(dados).TrimEnd('=').Replace('+', '-').Replace('/', '_');
