using Camisa10.Domain.Model;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>Empresas e investimentos do jogador (ver <see cref="Business"/>).</summary>
public sealed partial class GameEngine
{
    private IEnumerable<Company> Active => s.Companies.Where(c => c.Status == "ativa");

    private Company? Focus => s.Companies.FirstOrDefault(c => c.Id == s.FocusCompany && c.Status == "ativa");

    /// <summary>Ruído com média 0 e desvio perto de 1 (soma de três sorteios).</summary>
    private double Noise() => (Rand() + Rand() + Rand() - 1.5) * 2;

    private void RequireActive()
    {
        if (s.Status != CareerStatus.Active) throw new GameRuleException("A carreira já terminou.");
    }

    private Company OpenCompany(string kindId)
    {
        RequireActive();
        var k = Business.Find(kindId) ?? throw new GameRuleException("Tipo de empresa não encontrado.");
        if (s.Money < k.Cost) throw new GameRuleException($"Faltam R$ {Fmt.Num(Round1(k.Cost - s.Money))} mi para abrir {k.Name.ToLowerInvariant()}.");
        var first = s.Name.Split(' ')[0];
        var last = s.Name.Split(' ')[^1];
        var names = k.Names.Select(n => (first == last ? n.Replace("{n} {s}", "{n}") : n).Replace("{s}", last).Replace("{n}", first)).ToList();
        var name = names.FirstOrDefault(n => s.Companies.All(c => c.Name != n)) ?? $"{names[0]} {s.Companies.Count + 1}";
        var c = new Company
        {
            Id = Guid.NewGuid().ToString("N")[..8], Kind = k.Id, Name = name, Invested = k.Cost, Value = Round1(k.Cost * .9), OpenedAge = s.Age
        };
        c.History.Add(new CompanyYear { Age = s.Age, Value = c.Value, Level = 1, Note = "Inauguração" });
        s.Companies.Add(c);
        s.Money = Round1(s.Money - k.Cost);
        s.Headlines.Add($"{s.Name} inaugura {name}");
        if (k.Cost >= 6) Gossip($"{{nome}} vira empresário e inaugura {name}", "negocios");
        return c;
    }

    private Company Mine(string id) =>
        s.Companies.FirstOrDefault(c => c.Id == id && c.Status == "ativa") ?? throw new GameRuleException("Empresa não encontrada.");

    private void ExpandCompany(string id)
    {
        RequireActive();
        var c = Mine(id);
        var k = Business.Find(c.Kind)!;
        if (c.Level >= Business.MaxLevel) throw new GameRuleException($"{c.Name} já tem o máximo de {k.Unit}s.");
        var cost = Business.ExpandCost(k, c.Level);
        if (s.Money < cost) throw new GameRuleException($"Faltam R$ {Fmt.Num(Round1(cost - s.Money))} mi para expandir {c.Name}.");
        s.Money = Round1(s.Money - cost);
        c.Level++;
        c.Invested = Round1(c.Invested + cost);
        c.Value = Round1(c.Value + cost * .9);
        Note(c, $"Abriu a {c.Level}ª {k.Unit}");
        s.Headlines.Add($"{c.Name} cresce: {c.Level} {k.Unit}s");
    }

    private void SellCompany(string id, double multiple = .95, string? note = null)
    {
        RequireActive();
        var c = Mine(id);
        var price = Round1(c.Value * multiple);
        s.Money = Round1(s.Money + price);
        c.Status = "vendida";
        c.SoldFor = price;
        Note(c, note ?? $"Vendida por R$ {Fmt.Num(price)} mi");
        s.Headlines.Add($"{s.Name} vende {c.Name} por R$ {Fmt.Num(price)} mi");
        if (price >= 10) Gossip($"{{nome}} fatura R$ {Fmt.Num(price)} mi com a venda de {c.Name}", "negocios");
    }

    /// <summary>Anota o fato na linha da temporada atual da empresa (ou cria uma).</summary>
    private void Note(Company c, string text)
    {
        var y = c.History.LastOrDefault(h => h.Age == s.Age);
        if (y == null) c.History.Add(new CompanyYear { Age = s.Age, Value = c.Value, Level = c.Level, Note = text });
        else
        {
            y.Note = y.Note == null ? text : y.Note + " · " + text;
            y.Value = c.Value;
            y.Level = c.Level;
        }
    }

    private Investment Holding(string kind)
    {
        var i = s.Investments.FirstOrDefault(x => x.Kind == kind);
        if (i == null) s.Investments.Add(i = new Investment { Kind = kind });
        return i;
    }

    /// <param name="tenths">Valor em décimos de milhão (10 = R$ 1 mi).</param>
    private void Invest(string kind, int tenths)
    {
        RequireActive();
        if (Business.FindAsset(kind) == null) throw new GameRuleException("Investimento não encontrado.");
        var amount = Round1(tenths / 10.0);
        if (amount <= 0) throw new GameRuleException("Escolha um valor.");
        if (s.Money < amount) throw new GameRuleException($"Você tem só R$ {Fmt.Num(s.Money)} mi.");
        var i = Holding(kind);
        i.Principal = Round1(i.Principal + amount);
        i.Value = Round1(i.Value + amount);
        s.Money = Round1(s.Money - amount);
    }

    /// <param name="tenths">Valor em décimos de milhão; nulo resgata tudo.</param>
    private void Withdraw(string kind, int? tenths)
    {
        RequireActive();
        var i = s.Investments.FirstOrDefault(x => x.Kind == kind && x.Value > 0) ?? throw new GameRuleException("Você não tem dinheiro aplicado aí.");
        var amount = tenths is { } t ? Math.Min(i.Value, Round1(t / 10.0)) : i.Value;
        if (amount <= 0) throw new GameRuleException("Escolha um valor.");
        var share = amount / i.Value;
        i.Principal = Round1(i.Principal * (1 - share));
        i.Value = Round1(i.Value - amount);
        if (i.Value < .05) { i.Value = 0; i.Principal = 0; }
        s.Money = Round1(s.Money + amount);
    }

    /// <summary>Fim de temporada: lucro de cada empresa e rendimento dos investimentos.</summary>
    internal void SeasonBusiness()
    {
        var total = 0.0;
        foreach (var c in Active.ToList())
        {
            var k = Business.Find(c.Kind)!;
            // lucro = média do setor (mais se for famoso, nas de marca) + sorte ou azar da temporada
            var z = Noise() * (c.Manager ? .6 : 1);
            var brand = Clamp(1 + k.Brand * (s.Fame - 40) / 100.0, .5, 2);
            var profit = c.Invested * (k.Rate * brand + k.Risk * .4 * z) + c.Boost - (c.Manager ? .2 : 0);
            string? note = null;
            var growth = Clamp(1.03 + k.Risk * .5 * z, .3, 2.5);
            if (k.Id == "startup")
            {
                var roll = Rand();
                if (roll < .1) { growth = R(4, 10); note = "O valor da empresa explodiu"; }
                else if (roll < .22) { growth = .15; note = "Perdeu o maior cliente"; }
            }
            c.Value = Round1(Math.Max(0, c.Value * growth));
            profit = Round1(profit);
            c.LastProfit = profit;
            c.TotalProfit = Round1(c.TotalProfit + profit);
            c.Boost = 0;
            total += profit;
            var y = c.History.LastOrDefault(h => h.Age == s.Age);
            if (y == null) c.History.Add(new CompanyYear { Age = s.Age, Profit = profit, Value = c.Value, Level = c.Level, Note = note });
            else { y.Profit = Round1(y.Profit + profit); y.Value = c.Value; y.Level = c.Level; if (note != null) y.Note = y.Note == null ? note : y.Note + " · " + note; }
            if (k.Fans > 0) s.Fans = Clamp(s.Fans + k.Fans);
            if (c.Value < c.Invested * .12)
            {
                c.Status = "falida";
                Note(c, "Faliu");
                s.Headlines.Add($"{c.Name}, de {s.Name}, fecha as portas");
                Gossip($"Prejuízo: {c.Name}, empresa de {{nome}}, vai à falência", "negocios");
            }
        }
        s.Money = Round1(Math.Max(0, s.Money + total));
        if (Active.Any() || total != 0)
            s.Headlines.Add(total >= 0 ? $"Empresas de {s.Name} lucram R$ {Fmt.Num(Round1(total))} mi na temporada"
                : $"Empresas de {s.Name} dão prejuízo de R$ {Fmt.Num(Round1(-total))} mi");

        foreach (var i in s.Investments.Where(x => x.Value > 0))
        {
            var a = Business.FindAsset(i.Kind)!;
            var r = Clamp(a.Mean + a.Spread * Noise(), a.Min, a.Max) + (i.Rebound ? .3 : 0);
            i.Rebound = false;
            i.LastReturn = Round2(r);
            i.Value = Round1(Math.Max(0, i.Value * (1 + r)));
            i.History.Add(new InvestmentYear { Age = s.Age, Value = i.Value, Return = i.LastReturn });
        }
    }

    /// <summary>Patrimônio: dinheiro + empresas ativas + investimentos.</summary>
    public double NetWorth() => Round1(s.Money + Active.Sum(c => c.Value) + s.Investments.Sum(i => i.Value));

    // ------------------------------------------------------------------ eventos de negócios

    /// <summary>Escolhe a empresa do evento e põe nome e valores no texto.</summary>
    private void FillBusiness(GameEvent ev)
    {
        var pool = ev.EventId switch
        {
            "b_crise" => Active.Where(c => c.Value < c.Invested * .8),
            "b_franquia" => Active.Where(c => c.Level >= 2 && c.Level < Business.MaxLevel),
            "b_gerente" => Active.Where(c => !c.Manager),
            _ => Active
        };
        var list = pool.ToList();
        if (list.Count > 0) s.FocusCompany = list[R(0, list.Count - 1)].Id;
        if (ev.EventId == "b_bolsa" && s.Investments.FirstOrDefault(i => i.Kind == "acoes") is { Value: > 0 } acoes)
            acoes.Value = Round1(acoes.Value * .75); // a queda acontece quando a notícia chega
        var f = Focus;
        string T(string? t) => t == null ? "" : RomanceText(t
            .Replace("{empresa}", f?.Name ?? "sua empresa")
            .Replace("{oferta}", f == null ? "" : Fmt.Num(Round1(f.Value * 1.6)))
            .Replace("{socio}", f == null ? "" : Fmt.Num(Round1(f.Value * .36)))
            .Replace("{aporte}", f == null ? "" : Fmt.Num(Round1(f.Invested * .25)))
            .Replace("{acoes}", Fmt.Num(s.Investments.FirstOrDefault(i => i.Kind == "acoes")?.Value ?? 0)));
        ev.Title = T(ev.Title);
        ev.Text = T(ev.Text);
        foreach (var o in ev.Options)
        {
            o.Label = T(o.Label);
            o.SuccessText = T(o.SuccessText);
            if (o.FailText != null) o.FailText = T(o.FailText);
        }
    }

    /// <summary>Efeito de uma decisão de negócios.</summary>
    private void ApplyBiz(string op)
    {
        if (op.StartsWith("abrir:"))
        {
            var k = Business.Find(op[6..]);
            if (k != null && s.Money >= k.Cost) OpenCompany(k.Id);
            return;
        }
        var acoes = s.Investments.FirstOrDefault(i => i.Kind == "acoes");
        switch (op)
        {
            case "aplicar_renda":
                var amount = Round1(s.Money * .3);
                if (amount > 0) Invest("renda", (int)Math.Round(amount * 10));
                return;
            case "segurar":
                if (acoes != null) acoes.Rebound = true;
                return;
            case "comprar_queda":
                var dip = Round1(Math.Min(s.Money * .3, 10));
                if (dip > 0) Invest("acoes", (int)Math.Round(dip * 10));
                Holding("acoes").Rebound = true;
                return;
            case "vender_acoes":
                if (acoes is { Value: > 0 }) Withdraw("acoes", null);
                return;
        }
        var c = Focus;
        if (c == null) return;
        var kind = Business.Find(c.Kind)!;
        switch (op)
        {
            case "vender30":
                var part = Round1(c.Value * .36);
                s.Money = Round1(s.Money + part);
                c.Value = Round1(c.Value * .7);
                c.Invested = Round1(c.Invested * .7);
                Note(c, $"Vendeu 30% para um sócio por R$ {Fmt.Num(part)} mi");
                break;
            case "injetar":
                var cash = Math.Min(s.Money, Round1(c.Invested * .25));
                s.Money = Round1(s.Money - cash);
                c.Invested = Round1(c.Invested + cash);
                c.Value = Round1(c.Value + cash);
                c.Boost = Round1(c.Boost + cash * .5);
                Note(c, $"Recebeu R$ {Fmt.Num(cash)} mi para sair da crise");
                break;
            case "cortar":
                c.Value = Round1(c.Value * .92);
                c.Boost = Round1(c.Boost + c.Invested * kind.Rate * .3);
                Note(c, "Cortou custos");
                break;
            case "fechar":
                SellCompany(c.Id, .5, "Fechada na crise");
                c.Status = "fechada";
                break;
            case "vender":
                SellCompany(c.Id, 1.6);
                break;
            case "franquia":
                var fee = Round1(Business.ExpandCost(kind, c.Level) * .3);
                s.Money = Round1(Math.Max(0, s.Money - fee));
                c.Level = Math.Min(Business.MaxLevel, c.Level + 1);
                c.Invested = Round1(c.Invested + Business.ExpandCost(kind, c.Level - 1) * .6);
                c.Value = Round1(c.Value + Business.ExpandCost(kind, c.Level - 1) * .6);
                Note(c, $"Virou franquia: {c.Level} {kind.Unit}s");
                break;
            case "gerente":
                c.Manager = true;
                s.Money = Round1(Math.Max(0, s.Money - .5));
                Note(c, "Contratou um gerente profissional");
                break;
            case "multa":
                var fine = Round1(Math.Max(.5, c.Value * .08));
                s.Money = Round1(Math.Max(0, s.Money - fine));
                Note(c, $"Multa de R$ {Fmt.Num(fine)} mi");
                break;
        }
    }
}
