using System.Diagnostics;
using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;

namespace Camisa10.Tests;

public class WorldTests
{
    [Fact]
    public void Mundo_tem_211_paises_com_tres_divisoes_e_ids_unicos()
    {
        Assert.Equal(211, World.Countries.Count);
        Assert.All(World.Countries, c =>
        {
            Assert.Equal(3, c.Leagues.Count);
            Assert.All(c.Leagues, l => Assert.True(l.Clubs.Count >= 8, $"{c.Name} {l.Name}"));
            Assert.False(string.IsNullOrEmpty(c.Flag));
        });
        var ids = World.AllClubs.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.True(World.AllClubs.Count(c => !c.Fictional) > 2000);
        Assert.Equal(6, World.Countries.Select(c => c.Confederation).Distinct().Count());
    }

    [Fact]
    public void Clubes_e_selecoes_de_antes_continuam_existindo()
    {
        foreach (var (club, _) in GameData.BaseClubs) Assert.NotNull(World.ClubByName(club));
        foreach (var n in new[] { "Brasil", "Argentina", "França", "Inglaterra", "Espanha", "Alemanha", "Portugal", "Itália", "Holanda", "Bélgica", "Uruguai", "Croácia", "Marrocos", "Japão", "México", "EUA" })
            Assert.NotNull(World.CountryByName(n));
        Assert.Equal("BR", World.ClubByName("Flamengo")!.CountryIso);
        Assert.Equal("UY", World.ClubByName("Nacional")!.CountryIso);
        Assert.Equal("SA", World.ClubByName("Al-Hilal")!.CountryIso);
    }

    [Fact]
    public void Niveis_seguem_liga_e_divisao()
    {
        int T(string name) { var c = World.ClubByName(name)!; return World.Tier(c.CountryIso, World.InitialDivision(c.Id), c.Prestige); }
        Assert.Equal(4, T("Real Madrid"));
        Assert.Equal(3, T("Flamengo"));
        Assert.True(T("Fortaleza") < T("Flamengo"));
        Assert.True(T("Ponte Preta") <= T("Fortaleza"));
        Assert.Equal(0, T("Lincoln Red Imps"));
    }

    [Fact]
    public void Acesso_e_rebaixamento_mantem_o_tamanho_das_ligas()
    {
        var results = new List<ClubSeasonResult>();
        var first = WorldSim.Layout(42, 1, results);
        for (var season = 2; season <= 6; season++)
        {
            var l = WorldSim.Layout(42, season, results);
            Assert.Equal(first.Where.Count, l.Where.Count);
            foreach (var (iso, divs) in l.Leagues)
                for (var d = 0; d < 3; d++)
                    Assert.Equal(first.Leagues[iso][d].Count, divs[d].Count);
            var prev = WorldSim.Layout(42, season - 1, results);
            var br = prev.Leagues["BR"];
            var moved = l.Leagues["BR"][0].Except(br[0]).Count();
            Assert.Equal(WorldSim.Swaps(br[0].Count), moved);
        }
        // mesma semente, mesmo mundo
        Assert.Equal(WorldSim.Layout(42, 5, results).Leagues["GB-ENG"][0], WorldSim.Layout(42, 5, new List<ClubSeasonResult>()).Leagues["GB-ENG"][0]);
        Assert.NotEqual(WorldSim.Layout(42, 5, results).Leagues["GB-ENG"][1], WorldSim.Layout(43, 5, results).Leagues["GB-ENG"][1]);
    }

    [Fact]
    public void Posicao_do_jogador_decide_o_acesso_do_clube()
    {
        var remoId = World.ClubByName("Náutico")!.Id; // Série B
        var results = new List<ClubSeasonResult> { new() { Season = 1, ClubId = remoId, Division = 1, Rank = 1, Size = 20 } };
        var table = WorldSim.Standings(7, 1, results, "BR", 1);
        Assert.Equal(remoId, table[0].ClubId);
        Assert.Equal(0, WorldSim.Layout(7, 2, results).Of(remoId)!.Value.Division);

        results[0].Rank = 20;
        Assert.Equal(2, WorldSim.Layout(7, 2, results).Of(remoId)!.Value.Division);
    }

    [Fact]
    public void Carreira_comeca_em_clube_ficticio_de_pais_pequeno_e_chega_ao_fim()
    {
        var club = World.Country("MS").Leagues[2].Clubs[0];
        Assert.True(club.Fictional);
        var (c, _) = GameSimulationTests.PlayToEnd(99, "Atacante", club.Id);
        Assert.Equal(GamePhase.Finished, c.Phase);
        Assert.NotEmpty(c.ClubSeasons);
        // a divisão salva em cada temporada bate com a simulação
        foreach (var r in c.ClubSeasons)
            Assert.Equal(r.Division, WorldSim.Layout(c.WorldSeed, r.Season, c.ClubSeasons).Of(r.ClubId)!.Value.Division);
    }

    [Fact]
    public void Carreira_antiga_ganha_semente_e_clube_do_mundo()
    {
        var c = new Career { Name = "Antigo", Club = "Flamengo", BaseClub = "Flamengo", Base = false, Tier = 3, Season = 3, Position = "Meia", Nation = "Brasil" };
        c.Attributes = new Attributes { Rit = 60, Chu = 60, Pas = 70, Dri = 65, Def = 50, Fis = 60 };
        new GameEngine(c, new GameRandom(1)).EnsureWorld();
        Assert.NotEqual(0, c.WorldSeed);
        Assert.Equal(World.ClubByName("Flamengo")!.Id, c.ClubId);
        Assert.NotNull(c.LeagueName);
    }

    [Fact]
    public void Busca_encontra_clubes_sem_acento()
    {
        var r = ViewBuilder.SearchClubs("sao paulo");
        Assert.Equal("São Paulo", r[0].Name);
        Assert.Contains(ViewBuilder.SearchClubs("flamen"), x => x.Name == "Flamengo");
        Assert.Empty(ViewBuilder.SearchClubs("x"));
    }

    [Fact]
    public void Simulacao_de_20_temporadas_e_rapida()
    {
        var sw = Stopwatch.StartNew();
        WorldSim.Layout(123456, 20, new List<ClubSeasonResult>());
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 2000, $"{sw.ElapsedMilliseconds} ms");
    }
}
