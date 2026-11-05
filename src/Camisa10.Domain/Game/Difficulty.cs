using Camisa10.Domain.Model;

namespace Camisa10.Domain.Game;

/// <summary>
/// Níveis de dificuldade, escolhidos ao criar a carreira:
/// <list type="bullet">
/// <item><b>Fácil</b>: começa com dinheiro, potencial alto, salários 30% maiores, menos lesões e +2 no d20.</item>
/// <item><b>Normal</b>: o jogo como sempre foi.</item>
/// <item><b>Difícil</b>: começa pobre (origem humilde e quase sem dinheiro) na base de um clube da última
/// divisão do país (Série C), com potencial menor, salários 25% menores, mais lesões, −2 no d20 e menos
/// propostas.</item>
/// </list>
/// </summary>
public static class Difficulty
{
    public sealed record Level(string Id, string Name, string Icon, string Description);

    public static readonly IReadOnlyList<Level> Levels = new List<Level>
    {
        new("facil", "Fácil", "🙂", "Começa com dinheiro e muito potencial. Salários maiores, menos lesões e +2 nos dados."),
        new("normal", "Normal", "⚽", "A carreira como ela é: você escolhe o clube e a sorte decide a sua origem."),
        new("dificil", "Difícil", "🔥", "Você é pobre e começa na base de um time da Série C. Salários menores, mais lesões, −2 nos dados e menos propostas."),
    };

    /// <summary>Origens humildes: no difícil, a história começa por uma delas.</summary>
    public static readonly string[] PoorOrigins = { "comunidade", "avo", "peneiras", "interior" };

    public static string Normalize(string? id) => id is "facil" or "dificil" ? id : "normal";

    public static Level Of(Career c) => Levels.First(l => l.Id == c.Difficulty);

    /// <summary>Multiplicador do salário que cai na conta.</summary>
    public static double Income(Career c) => c.Difficulty switch { "facil" => 1.3, "dificil" => .75, _ => 1 };

    /// <summary>Multiplicador da chance de lesão grave.</summary>
    public static double Injury(Career c) => c.Difficulty switch { "facil" => .7, "dificil" => 1.3, _ => 1 };

    /// <summary>Somado ao modificador do d20.</summary>
    public static int Dice(Career c) => c.Difficulty switch { "facil" => 2, "dificil" => -2, _ => 0 };

    /// <summary>Propostas a menos (ou a mais) quando o mercado abre.</summary>
    public static int Offers(Career c) => c.Difficulty switch { "facil" => 1, "dificil" => -1, _ => 0 };
}
