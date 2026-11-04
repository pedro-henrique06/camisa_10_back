using Camisa10.Domain.Model;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>Compras para a casa do jogador (ver <see cref="Equipment"/>).</summary>
public sealed partial class GameEngine
{
    private void Buy(string id)
    {
        if (s.Status != CareerStatus.Active) throw new GameRuleException("A carreira já terminou.");
        var item = Equipment.Find(id) ?? throw new GameRuleException("Equipamento não encontrado.");
        if (s.Equipment.Contains(id)) throw new GameRuleException("Você já tem " + item.Name.ToLowerInvariant() + ".");
        if (item.Requires != null && !s.Equipment.Contains(item.Requires))
            throw new GameRuleException($"Antes, compre {Equipment.Find(item.Requires)!.Name.ToLowerInvariant()}.");
        if (s.Money < item.Price) throw new GameRuleException($"Faltam R$ {Fmt.Num(Round1(item.Price - s.Money))} mi para comprar {item.Name.ToLowerInvariant()}.");
        s.Money = Round1(s.Money - item.Price);
        s.Equipment.Add(id);
        s.Headlines.Add(item.PerSeason ? $"{s.Name} contrata {item.Name.ToLowerInvariant()}" : $"{s.Name} monta em casa: {item.Name.ToLowerInvariant()}");
    }
}
