using Camisa10.Domain.Model;

namespace Camisa10.Domain.Game;

/// <summary>Definição estática de um evento do catálogo, com a condição para aparecer.</summary>
public sealed class EventDef
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public required Func<Career, bool> Condition { get; init; }
    public required IReadOnlyList<EventOption> Options { get; init; }

    /// <summary>Cria uma cópia independente para colocar na tela/fila da carreira.</summary>
    public GameEvent ToEvent(bool isMatch = false) => new()
    {
        EventId = Id,
        Title = Title,
        Text = Text,
        IsMatch = isMatch,
        Options = Options.Select(o => o.Clone()).ToList()
    };

    public static EventDef Ev(string id, string title, string text, Func<Career, bool> cond, params EventOption[] options) =>
        new() { Id = id, Title = title, Text = text, Condition = cond, Options = options };

    public static EventOption O(string label, string okText, Fx ok,
        double? chance = null, string? failText = null, Fx? fail = null, string? attribute = null) =>
        new()
        {
            Label = label,
            SuccessText = okText,
            Success = ok,
            Chance = chance,
            FailText = failText,
            Fail = fail,
            Attribute = attribute
        };
}
