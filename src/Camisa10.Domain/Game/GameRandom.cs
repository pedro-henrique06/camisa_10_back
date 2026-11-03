namespace Camisa10.Domain.Game;

/// <summary>Fonte de aleatoriedade do jogo (injetável para testes com semente fixa).</summary>
public interface IGameRandom
{
    /// <summary>Número em [0, 1).</summary>
    double Next();
}

public sealed class GameRandom : IGameRandom
{
    private readonly Random _rng;
    public GameRandom(int? seed = null) => _rng = seed is { } s ? new Random(s) : Random.Shared;
    public double Next() => _rng.NextDouble();
}

internal static class Num
{
    /// <summary>Arredondamento igual ao Math.round do JavaScript (meio para cima).</summary>
    public static int Round(double x) => (int)Math.Floor(x + 0.5);
    public static double Round1(double x) => Math.Floor(x * 10 + 0.5) / 10;
    public static double Round2(double x) => Math.Floor(x * 100 + 0.5) / 100;
    public static int Clamp(int v, int a = 0, int b = 100) => Math.Max(a, Math.Min(b, v));
    public static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));
}
