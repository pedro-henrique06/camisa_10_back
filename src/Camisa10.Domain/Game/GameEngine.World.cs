using Camisa10.Domain.Model;
using Camisa10.Domain.World;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>Ligas do mundo: clube atual, divisão, sorteio de clubes, copas e tabela.</summary>
public sealed partial class GameEngine
{
    /// <summary>Divisões do mundo na temporada atual desta carreira.</summary>
    public WorldLayout Layout() => WorldSim.Layout(s.WorldSeed, Math.Max(1, s.Season), s.ClubSeasons);

    /// <summary>
    /// Completa carreiras antigas (de antes das ligas do mundo): semente e ids dos clubes pelo nome.
    /// </summary>
    public void EnsureWorld()
    {
        if (s.WorldSeed == 0) s.WorldSeed = StableSeed(s.Id);
        s.BaseClubId ??= World.World.ClubByName(s.BaseClub)?.Id;
        if (s.ClubId == null || World.World.Club(s.ClubId) == null)
        {
            var byName = World.World.ClubByName(CleanClub(s.Club));
            s.ClubId = byName?.Id ?? (s.Base ? s.BaseClubId : PickWorldClub(s.Tier).Id);
            if (byName == null && !s.Base && s.ClubId != null) s.Club = World.World.Club(s.ClubId)!.Name;
        }
        if (s.LeagueName == null) RefreshClub();
        EnsureCalendar();
    }

    /// <summary>
    /// Carreiras salvas antes do calendário com copas: monta as competições da temporada.
    /// Não usa sorteio, então pode rodar também ao só abrir a carreira.
    /// </summary>
    public void EnsureCalendar()
    {
        if (s.Competitions.Count != 0 || s.Season <= 0 || s.Status != CareerStatus.Active) return;
        if (s.LeagueName == null) RefreshClub();
        BuildCompetitions();
    }

    private static int StableSeed(string text)
    {
        unchecked
        {
            var h = 17;
            foreach (var ch in text) h = h * 31 + ch;
            return h == 0 ? 1 : h;
        }
    }

    /// <summary>Atualiza divisão, liga e nível a partir do clube atual na temporada atual.</summary>
    private void RefreshClub()
    {
        var id = s.ClubId ?? s.BaseClubId;
        if (id == null) return;
        var layout = Layout();
        var p = layout.Of(id);
        if (p == null) return;
        s.Division = p.Value.Division;
        s.LeagueName = World.World.Country(p.Value.Iso).Leagues[p.Value.Division].Name;
        if (!s.Base) s.Tier = layout.TierOf(id);
    }

    /// <summary>Nível do clube na temporada atual.</summary>
    private int TierOfClub(string clubId) => Layout().TierOf(clubId);

    /// <summary>
    /// Sorteia um clube do mundo com o nível pedido. Clubes do mesmo país e da mesma confederação
    /// aparecem mais, e clubes fictícios aparecem menos.
    /// </summary>
    private WorldClub PickWorldClub(int tier)
    {
        var layout = Layout();
        var myIso = s.ClubId != null ? layout.Of(s.ClubId)?.Iso : null;
        var myConfed = myIso != null ? World.World.Country(myIso).Confederation : null;
        for (var t = tier; t >= 0; t--)
        {
            var list = layout.ByTier[t].Where(c => c.Id != s.ClubId).ToList();
            if (list.Count == 0) continue;
            // Mais chance para o próprio país, a mesma confederação, ligas fortes e clubes reais.
            double W(WorldClub c)
            {
                var country = World.World.Country(c.CountryIso);
                return (c.Fictional ? .05 : 1) *
                       (c.CountryIso == myIso ? 25 : country.Confederation == myConfed ? 3 : 1) *
                       (.2 + country.Quality);
            }
            var total = list.Sum(W);
            var r = Rand() * total;
            foreach (var c in list)
            {
                r -= W(c);
                if (r <= 0) return c;
            }
            return list[^1];
        }
        return World.World.AllClubs.First();
    }

    /// <summary>Muda o jogador de clube (transferência, empréstimo ou evento).</summary>
    private void MoveToClub(WorldClub club)
    {
        s.ClubId = club.Id;
        s.Club = club.Name;
        RefreshClub();
        if (s.Season > 0) BuildCompetitions();
    }

    public static string? CountryOf(string club)
    {
        var c = World.World.ClubByName(CleanClub(club));
        return c == null ? null : World.World.Country(c.CountryIso).Name;
    }

    public static WorldCountry? CountryOfId(string? clubId) =>
        World.World.Club(clubId) is { } c ? World.World.Country(c.CountryIso) : null;

    // ---------------------------------------------------------------- copas
    private string NationalCupName()
    {
        if (s.Season % 4 == 0) return "Copa do Mundo";
        return World.World.CountryByName(s.Nation)?.Confederation switch
        {
            "CONMEBOL" => "Copa América",
            "CONCACAF" => "Copa Ouro",
            "AFC" => "Copa da Ásia",
            "CAF" => "Copa Africana de Nações",
            "OFC" => "Copa das Nações da OFC",
            _ => "Eurocopa"
        };
    }

    private double NationalStrength() => World.World.CountryByName(s.Nation)?.NationalStrength ?? 1;

    // ---------------------------------------------------------------- fim da temporada na liga
    /// <summary>
    /// Fim da liga: completa as rodadas que faltarem e fecha a tabela. No profissional, grava a
    /// classificação inteira para o acesso e o rebaixamento do mundo.
    /// </summary>
    private (int Rank, int Size, string? Movement, List<TableRow> Table)? FinishLeague()
    {
        if (CurrentLeague() is not { } lg) return null;
        CatchUpLeague(CompetitionOf("league")?.Days.Count ?? 0);
        if (s.LeagueTable.Count == 0) return null;
        var sorted = Sorted(s.LeagueTable);
        var size = sorted.Count;
        var rank = sorted.FindIndex(r => r.ClubId == MyLeagueClubId) + 1;
        var k = WorldSim.Swaps(size);
        var d = lg.Division;
        string? movement = null;
        if (!s.Base)
        {
            s.ClubSeasons.RemoveAll(r => r.Season == s.Season);
            s.ClubSeasons.Add(new ClubSeasonResult
            {
                Season = s.Season, ClubId = MyLeagueClubId, Division = d, Rank = rank, Size = size,
                Order = sorted.Select(r => r.ClubId).ToList()
            });
            if (d > 0 && rank <= k) movement = "up";
            else if (d < 2 && rank > size - k) movement = "down";
        }
        var table = sorted.Select((r, i) => new TableRow
        {
            Position = i + 1,
            Club = r.Club,
            Mine = r.ClubId == MyLeagueClubId,
            Zone = s.Base ? null : d > 0 && i < k ? "up" : d < 2 && i >= size - k ? "down" : null,
            Played = r.Played, Won = r.Won, Drawn = r.Drawn, Lost = r.Lost,
            GoalsFor = r.GoalsFor, GoalsAgainst = r.GoalsAgainst, Points = r.Points
        }).ToList();
        return (rank, size, movement, table);
    }
}
