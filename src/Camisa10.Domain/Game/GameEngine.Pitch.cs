using Camisa10.Domain.Model;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>
/// "Oferecer-se a um clube": no mercado, o jogador escolhe o clube em que quer jogar e quanto quer ganhar.
/// O clube analisa nível, idade, fase, fama, salário pedido e multa, e responde: aceita, faz uma
/// contraproposta (salário menor, contrato curto, sem luvas) ou recusa. Clubes grandes são bem mais exigentes.
/// </summary>
public partial class GameEngine
{
    /// <summary>Quantos clubes o jogador pode procurar em cada janela.</summary>
    public const int PitchesPerWindow = 3;

    /// <summary>Pedido de salário: 0 = aceito ganhar menos, 1 = o normal do clube, 2 = quero ganhar mais.</summary>
    public static readonly double[] PitchAsk = { .85, 1, 1.3 };

    /// <summary>Identifica a janela atual (temporada e primeiro dia dela).</summary>
    private string WindowKey()
    {
        var d = Math.Max(1, s.SeasonDay);
        while (d > 1 && SeasonCalendar.WindowOpen(s.CalendarType, d - 1)) d--;
        return $"{s.Season}|{d}";
    }

    private IEnumerable<string> PitchesThisWindow()
    {
        var key = WindowKey() + "|";
        return s.PitchLog.Where(x => x.StartsWith(key, StringComparison.Ordinal)).Select(x => x[key.Length..]);
    }

    public int PitchesLeft() => s.Phase == GamePhase.Market ? Math.Max(0, PitchesPerWindow - PitchesThisWindow().Count()) : 0;

    /// <summary>Exigência do clube: o overall que ele espera de um reforço (55 a 93).</summary>
    public static int Demand(double strength) => Num.Clamp((int)Math.Round(48 + 40 * strength), 55, 93);

    private void Pitch(string clubId, int ask)
    {
        if (s.Phase != GamePhase.Market) throw new GameRuleException("Abra o mercado para se oferecer a um clube.");
        if (ask < 0 || ask >= PitchAsk.Length) throw new GameRuleException("Pedido de salário inválido.");
        var club = World.World.Club(clubId) ?? World.World.ClubByName(clubId) ?? throw new GameRuleException("Clube não encontrado.");
        if (club.Id == s.ClubId) throw new GameRuleException("Você já joga no " + club.Name + ".");
        var done = PitchesThisWindow().ToList();
        if (done.Contains(club.Id)) throw new GameRuleException($"Você já se ofereceu ao {club.Name} nesta janela.");
        if (done.Count >= PitchesPerWindow) throw new GameRuleException($"Nesta janela você já procurou {PitchesPerWindow} clubes. Espere a próxima.");
        s.PitchLog.Add($"{WindowKey()}|{club.Id}");
        if (s.PitchLog.Count > 30) s.PitchLog.RemoveRange(0, s.PitchLog.Count - 30);

        var place = Layout().Of(club.Id);
        var strength = place is { } p ? World.World.Strength(p.Iso, p.Division, club.Prestige) : .3;
        var tier = TierOfClub(club.Id);
        var demand = Demand(strength);
        var big = tier >= 3;

        // ---------------------------------------------------------------- o que o clube olha
        var reasons = new List<(double Weight, string Text)>();
        var gap = s.Overall - demand;
        var level = gap * (big ? 2.4 : 1.8);
        if (gap < 0) reasons.Add((-level, $"acha que você ainda não tem nível para o elenco (procura reforços de overall {demand}; o seu é {s.Overall})"));

        var games = s.SeasonMatches.Where(m => m.Rating > 0).ToList();
        var form = 0.0;
        if (games.Count >= 3)
        {
            var avg = games.Average(m => m.Rating);
            form = (avg - 6.6) * (big ? 8 : 6);
            if (form < -2) reasons.Add((-form, $"não gostou da sua fase (nota média {avg:0.0} na temporada)"));
        }

        // fama e títulos pesam mais para quem vende camisa no mundo inteiro
        var fame = (s.Fame - 25) / 10.0 * (big ? 1.6 : .6) + Math.Min(6, s.Titles * (big ? 1.2 : .6));

        double age = s.Age <= 21 ? 5 : s.Age <= 24 ? 2 : s.Age <= 29 ? 0 : s.Age <= 31 ? -4 : s.Age <= 33 ? -9 : -16;
        if (big) age = age < 0 ? age * 1.5 : age;
        if (age < -3) reasons.Add((-age, $"prefere não investir num jogador de {s.Age} anos"));

        var normal = SalaryFor(tier);
        var salaryAsk = Round(normal * PitchAsk[ask] * 10) / 10.0;
        var money = ask == 0 ? 6 : ask == 2 ? -9 - tier * 1.5 : 0;
        if (ask == 2) reasons.Add((-money, $"achou o salário pedido alto demais (R$ {Fmt.Num(salaryAsk)} mil por mês)"));

        // a multa rescisória: o clube precisa pagar para tirar você daqui
        var fee = Math.Max(Round(MarketValue(tier) * R(85, 115) / 100.0), (int)Math.Ceiling(s.ReleaseClause));
        var budget = MarketValue(tier) * (1.2 + tier * .25);
        var clause = fee > budget ? -Math.Min(14, (fee - budget) / Math.Max(1, budget) * 20) : 0;
        if (clause < -2) reasons.Add((-clause, $"não quer pagar a multa de R$ {Fmt.Num(fee)} mi ao {CleanClub(s.Club)}"));

        var noise = R(-6, 6);
        // elenco fechado na posição: acontece, e pesa mais nos grandes, que têm elenco cheio
        var crowded = Rand() < (big ? .3 : .15) ? -R(4, 8) : 0;
        if (crowded < 0) reasons.Add((-crowded, $"já tem o elenco fechado de {s.Position.ToLowerInvariant()}"));

        var score = level + form + fame + age + money + clause + noise + crowded;
        // quanto maior o clube, mais alta a régua e menor a margem para negociar
        var acceptAt = 2 + tier * 2.5;
        var counterAt = acceptAt - Math.Max(5, 12 - tier * 1.5);
        var name = club.Name;
        var who = big ? $"A diretoria do {name}" : $"O {name}";

        MarketOffer? offer = null;
        string verdict, text;
        if (gap <= -14 || score < counterAt)
        {
            verdict = "reject";
            var why = reasons.OrderByDescending(r => r.Weight).Select(r => r.Text).FirstOrDefault() ?? "decidiu seguir com o elenco que tem";
            text = $"{who} agradece o interesse, mas recusa: {why}.";
            s.Morale = Clamp(s.Morale - (big ? 3 : 1));
            if (big && s.Fame >= 30) s.Headlines.Add($"{name} recusa {s.Name}");
        }
        else if (score >= acceptAt)
        {
            verdict = "accept";
            offer = new MarketOffer
            {
                ClubId = club.Id, Club = name, Tier = tier, Fee = fee, Salary = salaryAsk,
                SigningBonus = Round(fee * .05 * 10) / 10.0, Contract = 3, Pitched = true, Note = "Aceitou o seu pedido"
            };
            text = $"{who} topou! Aceita pagar R$ {Fmt.Num(fee)} mi ao {CleanClub(s.Club)} e o salário de R$ {Fmt.Num(salaryAsk)} mil que você pediu, com contrato de 3 anos.";
        }
        else
        {
            verdict = "counter";
            // contraproposta: paga menos, contrato curto e sem luvas (nos grandes, mais dura)
            var cut = (big ? R(60, 72) : R(72, 85)) / 100.0;
            var counterSalary = Round(Math.Min(salaryAsk, normal) * cut * 10) / 10.0;
            var years = big || s.Age >= 30 ? 1 : 2;
            offer = new MarketOffer
            {
                ClubId = club.Id, Club = name, Tier = tier, Fee = fee, Salary = counterSalary, SigningBonus = 0, Contract = years,
                Pitched = true, Note = $"Contraproposta: salário menor, sem luvas e {years} ano{(years == 1 ? "" : "s")} de contrato"
            };
            text = big
                ? $"{who} ficou interessada, mas com condições: R$ {Fmt.Num(counterSalary)} mil por mês, sem luvas e só {years} ano de contrato para você provar seu valor."
                : $"{who} gostou, mas faz uma contraproposta: R$ {Fmt.Num(counterSalary)} mil por mês, sem luvas e {years} anos de contrato.";
        }

        s.LastPitch = new PitchReply { ClubId = club.Id, Club = name, Tier = tier, Verdict = verdict, Text = text };
        if (offer != null)
        {
            s.MarketOffers.RemoveAll(o => o.ClubId == club.Id);
            s.MarketOffers.Insert(0, offer);
        }
    }
}
