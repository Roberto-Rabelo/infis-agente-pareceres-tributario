using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace Api.Agente.Application.A2A;

/// <summary>
/// Tarefas A2A recentes em memória, para <c>tasks/get</c> e <c>tasks/cancel</c>. As tarefas concluem de forma síncrona
/// em <c>message/send</c>; o repositório só guarda o resultado por um período curto. Em várias instâncias ou após
/// reinício, uma tarefa antiga pode não ser encontrada (erro protocolar TaskNotFound).
/// </summary>
public sealed class RepositorioTarefasA2A(TimeProvider relogio)
{
    public static readonly TimeSpan Retencao = TimeSpan.FromHours(1);
    public const int Capacidade = 1000;

    private readonly ConcurrentDictionary<string, (JsonObject Tarefa, DateTimeOffset Expira)> _tarefas = new(StringComparer.Ordinal);

    public void Guardar(JsonObject tarefa)
    {
        var id = tarefa["id"]?.GetValue<string>() ?? throw new ArgumentException("Tarefa sem id.", nameof(tarefa));
        var agora = relogio.GetUtcNow();
        Limpar(agora);
        _tarefas[id] = ((JsonObject)tarefa.DeepClone(), agora + Retencao);
    }

    public JsonObject? Obter(string id)
    {
        if (!_tarefas.TryGetValue(id, out var item))
        {
            return null;
        }

        if (item.Expira <= relogio.GetUtcNow())
        {
            _tarefas.TryRemove(id, out _);
            return null;
        }

        return (JsonObject)item.Tarefa.DeepClone();
    }

    private void Limpar(DateTimeOffset agora)
    {
        foreach (var (id, item) in _tarefas)
        {
            if (item.Expira <= agora)
            {
                _tarefas.TryRemove(id, out _);
            }
        }

        var excedente = _tarefas.Count - Capacidade + 1;
        if (excedente <= 0)
        {
            return;
        }

        foreach (var id in _tarefas.OrderBy(t => t.Value.Expira).Take(excedente).Select(t => t.Key).ToList())
        {
            _tarefas.TryRemove(id, out _);
        }
    }
}
