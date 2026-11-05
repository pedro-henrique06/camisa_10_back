using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Xunit.Abstractions;

namespace Camisa10.Tests;

/// <summary>Vida fora de campo: muda com a idade e não repete os mesmos eventos toda hora.</summary>
public class LifeEventsTests(ITestOutputHelper output)
{
    private static readonly HashSet<string> Generic = new() { "cac", "ro", "cano", "n_pai" };

    private static bool IsLife(string? id) => id != null && (GameData.Personal.Contains(id) || Events.LifeIds.Contains(id));

    /// <summary>Eventos pessoais vistos numa carreira: (id, idade), uma vez por exibição.</summary>
    private static List<(string Id, int Age)> Seen(int seed)
    {
        var seen = new List<(string, int)>();
        GameEvent? last = null;
        GameSimulationTests.PlayToEnd(seed, "Atacante", "Flamengo", s =>
        {
            if (s.Phase == GamePhase.Event && s.Current != null && !ReferenceEquals(s.Current, last) && IsLife(s.Current.EventId))
            {
                last = s.Current;
                seen.Add((s.Current.EventId!, s.Age));
            }
        });
        return seen;
    }

    [Fact]
    public void Eventos_pessoais_variam_com_a_idade_e_repetem_pouco()
    {
        var all = new List<(string Id, int Age)>();
        var worstRepeat = 0;
        var genericTotal = 0;
        var careers = 12;
        for (var seed = 1; seed <= careers; seed++)
        {
            var seen = Seen(seed);
            all.AddRange(seen);
            var counts = seen.GroupBy(x => x.Id).Select(g => g.Count()).DefaultIfEmpty(0).Max();
            worstRepeat = Math.Max(worstRepeat, counts);
            genericTotal += seen.Count(x => Generic.Contains(x.Id));
            output.WriteLine($"carreira {seed}: {seen.Count} eventos pessoais, {seen.Select(x => x.Id).Distinct().Count()} diferentes, " +
                $"o mais repetido {counts}x, cachorro/assalto/cano/pai {seen.Count(x => Generic.Contains(x.Id))}x");
        }
        var distinctRatio = all.Select(x => x.Id).Distinct().Count();
        output.WriteLine($"total: {all.Count}, diferentes: {distinctRatio}, pior repetição: {worstRepeat}, genéricos por carreira: {genericTotal / (double)careers:0.0}");

        // eventos da fase certa da vida
        Assert.DoesNotContain(all, x => x.Id is "v_prova" or "v_aloja" or "v_pelada" && x.Age > 18);
        Assert.DoesNotContain(all, x => x.Id is "v_apos" or "v_curso_tec" or "v_garoto" && x.Age < 31);
        Assert.Contains(all, x => x.Id.StartsWith("v_") && x.Age <= 18);
        Assert.Contains(all, x => x.Id.StartsWith("v_") && x.Age >= 31);
        // o cachorro é adotado uma vez só; os imprevistos aparecem pouco
        Assert.True(worstRepeat <= 6, $"um evento apareceu {worstRepeat} vezes numa carreira");
        // antes desta mudança: 34 por carreira, com o mesmo evento até 17 vezes
        Assert.True(genericTotal / (double)careers <= 8, "cachorro, assalto, cano e pai aparecem demais");
        Assert.True(all.Select(x => x.Id).Distinct().Count() >= 80, "pouca variedade de eventos pessoais");
    }
}
