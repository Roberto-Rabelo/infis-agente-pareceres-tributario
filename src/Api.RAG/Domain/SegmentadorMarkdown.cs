using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Api.RAG.Domain;

/// <summary>
/// Segmentação determinística de Markdown (ADR D12, parâmetros provisórios até a avaliação de recuperação):
/// cada linha de tabela vira um trecho próprio com o cabeçalho repetido, para que uma regra (CRZ-*, ENT-*, M1-P01...)
/// seja recuperada inteira; parágrafos são agrupados por seção até o limite de caracteres, sem sobreposição.
/// </summary>
public static partial class SegmentadorMarkdown
{
    public const int MinimoCaracteres = 200;

    public static IReadOnlyList<Trecho> Segmentar(string conteudo, int maximoCaracteres)
    {
        ArgumentNullException.ThrowIfNull(conteudo);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximoCaracteres, MinimoCaracteres);

        var linhas = conteudo.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var estado = new Estado(maximoCaracteres);
        for (var i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i];
            var numero = i + 1;
            if (estado.EmCodigo)
            {
                estado.AdicionarAoBloco(linha, numero);
                estado.EmCodigo = !Cerca().IsMatch(linha);
                continue;
            }

            if (Cerca().IsMatch(linha))
            {
                estado.FecharTabela();
                estado.AdicionarAoBloco(linha, numero);
                estado.EmCodigo = true;
                continue;
            }

            var titulo = Titulo().Match(linha);
            if (titulo.Success)
            {
                estado.FecharTabela();
                estado.FecharBloco();
                estado.EmitirAcumulado();
                estado.DefinirTitulo(titulo.Groups[1].Length, titulo.Groups[2].Value.Trim());
                continue;
            }

            if (LinhaTabela().IsMatch(linha))
            {
                if (estado.CabecalhoTabela is not null)
                {
                    estado.EmitirLinhaTabela(linha, numero);
                    continue;
                }

                if (i + 1 < linhas.Length && Separador().IsMatch(linhas[i + 1]))
                {
                    estado.FecharBloco();
                    estado.EmitirAcumulado();
                    estado.CabecalhoTabela = linha.TrimEnd() + "\n" + linhas[i + 1].TrimEnd();
                    i++;
                    continue;
                }
            }
            else
            {
                estado.FecharTabela();
            }

            if (string.IsNullOrWhiteSpace(linha) || Regua().IsMatch(linha))
            {
                estado.FecharBloco();
                continue;
            }

            estado.AdicionarAoBloco(linha, numero);
        }

        estado.FecharBloco();
        estado.EmitirAcumulado();
        return estado.Trechos;
    }

    public static string Hash(string texto) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));

    private sealed class Estado(int maximo)
    {
        private readonly List<(int Nivel, string Texto)> _titulos = [];
        private readonly List<(string Texto, int Linha)> _bloco = [];
        private readonly StringBuilder _acumulado = new();
        private int _inicioAcumulado;
        private int _fimAcumulado;

        public List<Trecho> Trechos { get; } = [];

        public bool EmCodigo { get; set; }

        public string? CabecalhoTabela { get; set; }

        private string Secao => string.Join(" > ", _titulos.Select(t => t.Texto));

        public void DefinirTitulo(int nivel, string texto)
        {
            _titulos.RemoveAll(t => t.Nivel >= nivel);
            _titulos.Add((nivel, texto));
        }

        public void FecharTabela() => CabecalhoTabela = null;

        public void AdicionarAoBloco(string linha, int numero) => _bloco.Add((linha.TrimEnd(), numero));

        public void EmitirLinhaTabela(string linha, int numero)
        {
            var primeiraCelula = linha.Trim().TrimStart('|').Split('|')[0];
            var ids = ReferenciasRegra.Extrair(primeiraCelula);
            var principal = ids.Count > 0 ? ids[0] : null;
            Emitir(CabecalhoTabela + "\n" + linha.TrimEnd(), numero, numero, principal);
        }

        public void FecharBloco()
        {
            if (_bloco.Count == 0)
            {
                return;
            }

            var texto = string.Join('\n', _bloco.Select(l => l.Texto));
            if (texto.Length > maximo)
            {
                EmitirAcumulado();
                DividirBlocoLongo();
            }
            else
            {
                if (_acumulado.Length > 0 && _acumulado.Length + 2 + texto.Length > maximo)
                {
                    EmitirAcumulado();
                }

                if (_acumulado.Length == 0)
                {
                    _inicioAcumulado = _bloco[0].Linha;
                }
                else
                {
                    _acumulado.Append("\n\n");
                }

                _acumulado.Append(texto);
                _fimAcumulado = _bloco[^1].Linha;
            }

            _bloco.Clear();
        }

        public void EmitirAcumulado()
        {
            if (_acumulado.Length == 0)
            {
                return;
            }

            Emitir(_acumulado.ToString(), _inicioAcumulado, _fimAcumulado, null);
            _acumulado.Clear();
        }

        private void DividirBlocoLongo()
        {
            var parte = new StringBuilder();
            var inicio = _bloco[0].Linha;
            var fim = inicio;
            foreach (var (texto, linha) in _bloco)
            {
                if (parte.Length > 0 && parte.Length + 1 + texto.Length > maximo)
                {
                    Emitir(parte.ToString(), inicio, fim, null);
                    parte.Clear();
                    inicio = linha;
                }

                if (parte.Length > 0)
                {
                    parte.Append('\n');
                }

                parte.Append(texto);
                fim = linha;
            }

            Emitir(parte.ToString(), inicio, fim, null);
        }

        private void Emitir(string texto, int inicio, int fim, string? principal)
        {
            var secao = Secao;
            var ids = ReferenciasRegra.Extrair(texto);
            var obrigacoes = ReferenciasRegra.ObrigacoesDosIds(ids)
                .Union(ReferenciasRegra.ObrigacoesNomeadas(secao), StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();
            Trechos.Add(new Trecho(Trechos.Count, secao, inicio, fim, texto, Hash(texto), ids, principal, obrigacoes));
        }
    }

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")]
    private static partial Regex Titulo();

    [GeneratedRegex(@"^\s*\|")]
    private static partial Regex LinhaTabela();

    [GeneratedRegex(@"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$")]
    private static partial Regex Separador();

    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex Cerca();

    [GeneratedRegex(@"^\s*(-{3,}|\*{3,}|_{3,})\s*$")]
    private static partial Regex Regua();
}
