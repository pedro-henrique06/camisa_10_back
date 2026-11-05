using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Xunit.Abstractions;

namespace Camisa10.Tests;

/// <summary>Níveis de dificuldade: o difícil começa pobre num clube da Série C.</summary>
public class DifficultyTests(ITestOutputHelper output)
{
    [Fact]
    public void Dificil_comeca_pobre_na_serie_c()
    {
        var clubs = new HashSet<string>();
        for (var seed = 1; seed <= 30; seed++)
        {
            var c = GameEngine.NewCareer("p", "Teste", "Flamengo", "Brasil", "Atacante", new GameRandom(seed), "dificil");
            Assert.Equal("dificil", c.Difficulty);
            Assert.Equal(2, Domain.World.World.InitialDivision(c.BaseClubId!)); // Série C
            Assert.NotEqual("Base do Flamengo", c.Club);
            Assert.True(c.Money <= .5);
            Assert.Contains(c.Origin, Difficulty.PoorOrigins);
            clubs.Add(c.BaseClub!);
        }
        output.WriteLine(string.Join(", ", clubs));
        Assert.True(clubs.Count >= 8, "o clube da Série C é sorteado");
    }

    [Fact]
    public void Dificil_mantem_o_clube_se_ja_for_da_serie_c()
    {
        var countryC = Domain.World.World.Country("BR").Leagues[^1].Clubs[0];
        var c = GameEngine.NewCareer("p", "Teste", countryC.Id, "Brasil", "Atacante", new GameRandom(1), "dificil");
        Assert.Equal(countryC.Id, c.BaseClubId);
    }

    [Fact]
    public void Facil_e_normal()
    {
        var easy = GameEngine.NewCareer("p", "Teste", "Flamengo", "Brasil", "Atacante", new GameRandom(3), "facil");
        var normal = GameEngine.NewCareer("p", "Teste", "Flamengo", "Brasil", "Atacante", new GameRandom(3));
        Assert.Equal("Base do Flamengo", easy.Club);
        Assert.Equal("normal", normal.Difficulty);
        Assert.True(easy.Money >= 10);
        Assert.True(easy.Potential >= 82);
        Assert.Equal("Fácil", ViewBuilder.Build(easy).Hud.Story!.DifficultyName);
        Assert.Equal(3, ViewBuilder.Meta().Difficulties!.Count);
        Assert.Equal("normal", GameEngine.NewCareer("p", "T", "Flamengo", "Brasil", "Atacante", new GameRandom(1), "qualquer").Difficulty);
    }

    [Fact]
    public void Carreira_dificil_rende_menos_que_a_facil()
    {
        double Money(string level)
        {
            var total = 0.0;
            for (var seed = 1; seed <= 6; seed++)
            {
                var (c, _) = GameSimulationTests.PlayToEnd(seed, "Atacante", "Flamengo", null, level);
                total += c.Money + c.Companies.Where(x => x.Status == "ativa").Sum(x => x.Value) + c.Investments.Sum(i => i.Value);
            }
            return total / 6;
        }
        var easy = Money("facil");
        var hard = Money("dificil");
        output.WriteLine($"patrimônio médio no fim: fácil {easy:0}, difícil {hard:0}");
        Assert.True(easy > hard * 1.3);
    }
}
