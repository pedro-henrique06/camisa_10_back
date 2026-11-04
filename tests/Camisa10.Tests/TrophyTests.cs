using Camisa10.Domain.Views;

namespace Camisa10.Tests;

public class TrophyTests
{
    [Fact]
    public void Sala_de_trofeus_bate_com_os_titulos_da_carreira()
    {
        var total = 0;
        for (var seed = 1; seed <= 8; seed++)
        {
            var (c, _) = GameSimulationTests.PlayToEnd(seed * 31, "Atacante", "Flamengo");
            Assert.Equal(c.Titles, c.Trophies.Count(x => x.Kind != "award"));
            Assert.Equal(c.NationalTitles, c.Trophies.Count(x => x.Kind == "national"));
            Assert.Equal(c.Ballons, c.Trophies.Count(x => x.Kind == "award"));
            Assert.All(c.Trophies, x => Assert.False(string.IsNullOrEmpty(x.Team)));

            var room = ViewBuilder.Build(c).Hud.Trophies;
            Assert.Equal(c.Trophies.Count, room.Groups.Sum(g => g.Count));
            Assert.Equal(0, room.Untracked);
            Assert.All(room.Groups, g => Assert.Equal(g.Count, g.Wins.Count));
            total += c.Trophies.Count;
        }
        Assert.True(total > 5, "carreiras com títulos");
    }
}
