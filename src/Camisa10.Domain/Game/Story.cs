using Camisa10.Domain.Model;

namespace Camisa10.Domain.Game;

/// <summary>
/// O que faz cada carreira ser única: a origem sorteada no começo, a reputação que as escolhas constroem
/// (e que muda o que acontece depois) e o epílogo, que conta como a história terminou.
/// </summary>
public static class Story
{
    /// <param name="Setup">Ajustes iniciais da origem (dinheiro, fama, família...).</param>
    public sealed record Origin(string Id, string Name, string Icon, string Text, Action<Career> Setup);

    public static readonly IReadOnlyList<Origin> Origins = new List<Origin>
    {
        new("comunidade", "Cria da comunidade", "🏘️", "Você cresceu jogando descalço no campinho de terra da comunidade. A família tem pouco, mas o bairro inteiro torce por você.",
            c => { c.Money = 1; c.Family = 60; c.Fans = 55; }),
        new("filho_craque", "Filho de ex-jogador", "👨‍👦", "Seu pai foi ídolo nos anos 90. O sobrenome abre portas, mas todo mundo compara você com ele.",
            c => { c.Fame = 10; c.Agent = 60; c.Money = 8; c.Morale = 55; }),
        new("rico", "Família rica", "💎", "Seu pai é empresário e nunca faltou nada. No vestiário, tem quem ache que você está ali só pelo dinheiro.",
            c => { c.Money = 25; c.Locker = 42; c.Fans = 45; }),
        new("interior", "Menino do interior", "🌾", "Você cresceu numa cidade pequena, acordando cedo para ajudar na roça. A cidade inteira para quando você joga.",
            c => { c.Fitness = 96; c.Money = 2; c.Family = 58; c.Rep.Discipline = 2; }),
        new("avo", "Criado pela avó", "👵", "Foi a sua avó quem te criou e te levava aos treinos de ônibus. Tudo o que você faz é por ela.",
            c => { c.Family = 70; c.Money = 1; c.Morale = 65; }),
        new("igreja", "Filho de pastor", "⛪", "Criado na igreja, você aprendeu cedo a disciplina e a fé. Todo domingo, a comunidade ora por você.",
            c => { c.Rep.Character = 3; c.Rep.Discipline = 2; c.Family = 62; }),
        new("peneiras", "Reprovado em dez peneiras", "🥅", "Dez clubes disseram não. Você chegou à base do jeito mais difícil e não esquece quem duvidou.",
            c => { c.Morale = 72; c.Rep.Discipline = 3; c.Rep.Ambition = 2; c.Money = 2; }),
        new("influencer", "Famoso nas redes", "📱", "Seus vídeos de embaixadinha viralizaram antes do primeiro jogo. Milhões te seguem, e metade quer te ver errar.",
            c => { c.Fame = 16; c.Money = 6; c.Rep.Controversy = 2; c.Locker = 45; }),
    };

    private static readonly Dictionary<string, Origin> ById = Origins.ToDictionary(o => o.Id);
    public static Origin? Find(string? id) => id == null ? null : ById.GetValueOrDefault(id);

    /// <summary>
    /// Destino: cada carreira vive só uma parte das histórias. Cerca de 40% dos eventos fora de campo e metade
    /// dos arcos ficam de fora, sorteados pela semente da carreira (sempre os mesmos para ela). Treinos, jogos
    /// e as histórias da origem do jogador sempre podem aparecer.
    /// </summary>
    public static bool Fated(Career c, string eventId)
    {
        var arc = eventId.StartsWith("a_") || eventId.StartsWith("p_");
        var offField = arc || eventId.StartsWith("v_") || eventId.StartsWith("r_") || eventId.StartsWith("b_") || GameData.Personal.Contains(eventId);
        if (!offField || eventId.StartsWith("r_conhece") || eventId.StartsWith("r_bebe")) return true;
        var h = 17;
        foreach (var ch in eventId) h = unchecked(h * 31 + ch);
        h = unchecked(h ^ c.WorldSeed * 16777619);
        return Math.Abs(h % 100) >= (arc ? 50 : 40);
    }

    /// <summary>Muda a reputação a partir de "carater:2,polemica:-1".</summary>
    public static void Apply(Reputation r, string spec)
    {
        foreach (var part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split(':');
            if (kv.Length != 2 || !int.TryParse(kv[1], out var v)) continue;
            switch (kv[0])
            {
                case "carater": r.Character += v; break;
                case "lealdade": r.Loyalty += v; break;
                case "polemica": r.Controversy += v; break;
                case "solidario": r.Charity += v; break;
                case "ambicao": r.Ambition += v; break;
                case "disciplina": r.Discipline += v; break;
            }
        }
    }

    /// <summary>Rótulos de reputação, do mais forte para o mais fraco (ex.: "Coração de ouro").</summary>
    public static List<(string Icon, string Label, string Hint)> Tags(Career c)
    {
        var r = c.Rep;
        var list = new List<(int Strength, string Icon, string Label, string Hint)>();
        void Add(int v, int at, string icon, string label, string hint) { if (v >= at) list.Add((v, icon, label, hint)); }
        Add(r.Character, 6, "🤝", "Exemplo de caráter", "Clubes e patrocinadores confiam em você");
        Add(-r.Character, 5, "🃏", "Fama de malandro", "Patrocinadores fogem e a imprensa fica de olho");
        Add(r.Loyalty, 6, "💚", "Ídolo fiel", "A torcida te defende em qualquer fase");
        Add(-r.Loyalty, 5, "💰", "Mercenário", "A torcida desconfia quando você chega");
        Add(r.Controversy, 6, "🔥", "Polêmico", "Mais manchetes, menos propostas de clubes grandes");
        Add(-r.Controversy, 6, "🤫", "Discreto", "Quase não sai nas revistas");
        Add(r.Charity, 6, "❤️‍🩹", "Coração de ouro", "Projetos sociais e a admiração do público");
        Add(-r.Charity, 5, "🧊", "Mão de vaca", "Ninguém lembra de você quando o assunto é ajudar");
        Add(r.Ambition, 6, "🚀", "Ambicioso", "Atrás de dinheiro e títulos, custe o que custar");
        Add(r.Discipline, 6, "🎯", "Profissional exemplar", "Técnicos adoram e as propostas pagam mais");
        Add(-r.Discipline, 5, "🍾", "Indisciplinado", "Técnicos desconfiam e as propostas pagam menos");
        return list.OrderByDescending(x => x.Strength).Select(x => (x.Icon, x.Label, x.Hint)).ToList();
    }

    /// <summary>Multiplicador de salário nas propostas: profissional e honesto ganha mais, polêmico e indisciplinado, menos.</summary>
    public static double SalaryFactor(Career c) =>
        Math.Clamp(1 + (c.Rep.Discipline + c.Rep.Character) / 80.0 - Math.Max(0, c.Rep.Controversy - 3) / 60.0, .8, 1.25);

    /// <summary>Clube grande recusa jogador muito polêmico ou indisciplinado.</summary>
    public static bool BigClubsWary(Career c) => c.Rep.Controversy >= 8 || c.Rep.Discipline <= -6;

    /// <summary>Como a história termina: frases montadas a partir de tudo o que o jogador viveu.</summary>
    public static List<string> Epilogue(Career c, double netWorth)
    {
        var lines = new List<string>();
        var origin = Find(c.Origin);
        var name = c.Name;
        if (origin != null)
            lines.Add(origin.Id switch
            {
                "comunidade" => c.Has("instituto") || c.Rep.Charity >= 6 ? $"O menino do campinho de terra voltou para a comunidade e construiu o futuro de centenas de crianças." : $"Da comunidade para o mundo: {name} nunca esqueceu de onde veio.",
                "filho_craque" => c.Titles >= 3 || c.Ballons > 0 ? "No fim, ninguém mais falava do pai: agora o pai é que era conhecido como o pai de " + name + "." : "A comparação com o pai acompanhou a carreira inteira.",
                "rico" => c.Rep.Discipline >= 4 ? "Provou para o vestiário que não estava ali pelo dinheiro da família." : "Para muitos, seguiu sendo o filho do empresário que jogava bola.",
                "interior" => "A cidade natal ganhou uma rua com o seu nome e o campo da roça virou estádio.",
                "avo" => c.Has("avo_viva") ? "A avó assistiu da arquibancada a cada passo da carreira." : "Cada gol foi dedicado à avó que o levava aos treinos de ônibus.",
                "igreja" => c.Rep.Character >= 6 ? "Virou exemplo dentro e fora de campo, como a família sonhava." : "A fé foi o porto seguro nos dias difíceis.",
                "peneiras" => "Os dez clubes que disseram não precisaram explicar por que deixaram passar " + name + ".",
                "influencer" => c.Fame >= 60 ? "Os seguidores viraram torcida, e o vídeo de embaixadinha virou lenda." : "Muitos lembram mais dos vídeos do que dos gols.",
                _ => ""
            });
        if (c.EndReason == "ban") lines.Add("Banido do futebol por envolvimento com apostas, a carreira terminou num tribunal.");
        if (c.Has("doping")) lines.Add("O caso de doping ficou para sempre na biografia.");
        if (c.Has("eleito")) lines.Add($"Depois do futebol, assumiu o mandato e trocou o gramado pela política.");
        if (c.Has("cantor")) lines.Add("A música gravada no auge ainda toca nas festas.");
        if (c.Has("salvou_clube")) lines.Add("O clube onde tudo começou só existe até hoje por causa do dinheiro que ele colocou.");
        if (c.Has("mente_forte")) lines.Add("Falar abertamente sobre saúde mental ajudou milhares de jovens atletas.");
        if (c.Has("mentor_tecnico")) lines.Add("Seguiu os passos do mentor e virou referência para os mais novos.");
        if (c.Has("amigo_salvo")) lines.Add($"{c.Buddy ?? "O amigo de infância"}, que ele tirou do crime, continua ao seu lado.");
        else if (c.Has("amigo_preso")) lines.Add($"{c.Buddy ?? "O amigo de infância"} continua preso. Até hoje {name} se pergunta se podia ter ajudado.");

        var kids = c.Love.Children;
        var kidsText = kids.Count == 1 ? (kids[0].Sex == "f" ? $"da filha {kids[0].Name}" : $"do filho {kids[0].Name}") : $"dos {kids.Count} filhos";
        if (c.Love.Stage == "casado")
            lines.Add(kids.Count > 0 ? $"Ao lado de {c.Love.Partner} e {kidsText}, aproveita a vida longe dos holofotes." : $"Casado com {c.Love.Partner}, aproveita a vida longe dos holofotes.");
        else if (kids.Count == 1)
            lines.Add($"{(kids[0].Sex == "f" ? "A filha" : "O filho")} {kids[0].Name} é o maior título da sua vida.");
        else if (kids.Count > 1)
            lines.Add($"Os {kids.Count} filhos são o maior título da sua vida.");
        else if (c.Love.Cheats >= 2)
            lines.Add("As revistas de fofoca sentiram falta dos escândalos.");

        if (netWorth >= 150) lines.Add($"Com R$ {Fmt.Num(Math.Round(netWorth))} mi de patrimônio, virou um dos ex-jogadores mais ricos do país.");
        else if (c.Companies.Any(x => x.Status == "ativa")) lines.Add("Hoje cuida das empresas que abriu durante a carreira.");
        else if (netWorth < 3) lines.Add("O dinheiro da carreira acabou, e a vida depois do futebol não foi fácil.");

        var tags = Tags(c);
        if (tags.Count > 0) lines.Add($"Será lembrado como: {string.Join(", ", tags.Take(3).Select(t => t.Label.ToLowerInvariant()))}.");
        return lines.Where(l => l.Length > 0).ToList();
    }
}
