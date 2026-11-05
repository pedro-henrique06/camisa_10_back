#nullable enable
using Camisa10.Domain.Model;
namespace Camisa10.Domain.Game;

using static EventDef;

/// <summary>
/// Vida amorosa por estágio: solteiro → ficando → namorando → noivo → casado (ou separado). Os textos usam
/// {par} (quem está com o jogador), {prof} (a profissão) e {nome} (o jogador). Os eventos com condição
/// "s => false" só aparecem agendados (FollowUp) por outro evento.
/// </summary>
public static partial class Events
{
    private static bool Stage(Career s, params string[] stages) => stages.Contains(s.Love.Stage);
    private static bool Committed(Career s) => GameEngine.CommittedStages.Contains(s.Love.Stage) && s.Love.Stage != "ficando";
    private static int Years(Career s) => s.Season - s.Love.Since;

    public static readonly IReadOnlyList<EventDef> Romance = new List<EventDef>
    {
        // ---------------------------------------------------------------- solteiro
        Ev("r_conhece", "Alguém chamou a sua atenção", "Num evento de patrocinador você conhece {par}, {prof}. A conversa flui e o tempo passa voando.",
            s => s.Age >= 17 && Stage(s, "solteiro", "separado") && (s.Love.Stage == "solteiro" || Years(s) >= 1),
            O("Puxar conversa com confiança", "{par} ri das suas piadas e passa o telefone.", new Fx { LoveStage = "ficando", Morale = 4, FollowUp = "r_encontro", FollowUpIn = 4, Gossip = "{nome} troca olhares com uma {prof} em evento de patrocinador" }, 0.55,
                "Você trava e fala só de futebol. {par} vai embora cedo.", new Fx { Morale = -2 }),
            O("Pedir para um amigo apresentar", "O amigo arma tudo e vocês saem juntos.", new Fx { LoveStage = "ficando", Friends = 2, FollowUp = "r_encontro", FollowUpIn = 6 }),
            O("Deixar passar: o foco é o futebol", "O técnico agradece. O coração, nem tanto.", new Fx { Trust = 1 })),
        Ev("r_balada", "Balada com famosos", "Você foi chamado para a festa de um cantor famoso. Vai estar cheio de gente conhecida e de câmeras.",
            s => s.Age >= 18 && Stage(s, "solteiro", "separado") && s.Fame >= 15,
            O("Curtir a noite toda", "Fotos, música e uma ressaca daquelas.", new Fx { Morale = 5, Fitness = -4, Fame = 2, Gossip = "{nome} curte balada com famosos e sai só às 5 da manhã" }),
            O("Passar rápido e ir embora", "Aparece, posa para fotos e dorme cedo.", new Fx { Fame = 1 }),
            O("Recusar o convite", "Noite tranquila em casa.", new Fx { Fitness = 2 })),

        // ---------------------------------------------------------------- ficando
        Ev("r_encontro", "Primeiro encontro com {par}", "Vocês marcaram de sair. Onde levar {par}?", s => false,
            O("Jantar num restaurante caro", "Vinho, vista bonita e conversa até tarde.", new Fx { Money = -1, Morale = 4, Love = 15, FollowUp = "r_namoro", FollowUpIn = 10, Gossip = "{nome} janta com {par}, {prof}, em restaurante badalado" }),
            O("Programa simples: pastel e cinema", "{par} adora a simplicidade.", new Fx { Love = 14, Morale = 3, FollowUp = "r_namoro", FollowUpIn = 10 }, 0.8,
                "O filme é ruim e o papo não engata.", new Fx { Love = 4, FollowUp = "r_namoro", FollowUpIn = 14 }),
            O("Levar para ver o seu treino", "{par} assiste da arquibancada e manda fotos suas jogando.", new Fx { Love = 8, Trust = 1, FollowUp = "r_namoro", FollowUpIn = 10 })),
        Ev("r_namoro", "Vai virar namoro?", "Faz semanas que vocês se veem toda folga. {par} pergunta o que vocês são.", s => false,
            O("Pedir {par} em namoro", "{par} diz sim com um sorriso enorme.", new Fx { LoveStage = "namorando", Love = 12, Morale = 6, Gossip = "É oficial! {nome} assume namoro com {par}" }, 0.75,
                "{par} quer ir com mais calma.", new Fx { Love = -6, Morale = -2 }),
            O("Assumir nas redes sociais", "Uma foto juntos e a internet vai à loucura.", new Fx { LoveStage = "namorando", Love = 8, Fame = 3, Gossip = "{nome} posta foto com {par} e confirma romance" }),
            O("Continuar só ficando", "{par} fica chateada com a indefinição.", new Fx { Love = -10, FollowUp = "r_namoro", FollowUpIn = 30 })),
        Ev("r_ficando_msg", "Mensagem de {par} no dia do jogo", "\"Boa sorte hoje! Vou estar torcendo.\" O jogo é daqui a duas horas.", s => Stage(s, "ficando"),
            O("Responder com carinho e chamar para sair depois", "Vocês comemoram juntos depois do jogo.", new Fx { Love = 8, Morale = 3 }),
            O("Pedir em namoro de uma vez", "Na saída do estádio, com a medalha no peito. Sim!", new Fx { LoveStage = "namorando", Love = 10, Gossip = "{nome} começa a namorar {par} depois de vitória" }, 0.6,
                "{par} acha cedo demais.", new Fx { Love = -5 }),
            O("Visualizar e não responder", "Foco total no jogo.", new Fx { Love = -9, Trust = 1 })),

        // ---------------------------------------------------------------- namorando
        Ev("r_aniv", "Aniversário de namoro", "Hoje faz um ano que vocês estão juntos.", s => Stage(s, "namorando") && Years(s) >= 1,
            O("Dar uma joia de presente", "{par} fica emocionada.", new Fx { Money = -2, Love = 12, Gossip = "{nome} presenteia {par} com joia de luxo no aniversário de namoro" }),
            O("Escrever uma carta e cozinhar o jantar", "O presente mais sincero do mundo.", new Fx { Love = 12, Morale = 3 }),
            O("Esquecer a data", "{par} passa o dia esperando uma mensagem.", new Fx { Love = -15, Morale = -2 })),
        Ev("r_familia", "Conhecer a família de {par}", "O almoço de domingo na casa dos pais de {par} é no dia seguinte a um jogo fora.", s => Stage(s, "namorando"),
            O("Ir, mesmo cansado", "A sogra te adora. O sogro pede uma camisa autografada.", new Fx { Love = 10, Family = 3, Fitness = -1 }),
            O("Remarcar para outro dia", "{par} entende, mas fica decepcionada.", new Fx { Love = -7 })),
        Ev("r_ciume", "{par} sente ciúmes das fãs", "Comentários de fãs nas suas fotos estão incomodando {par}.", s => Committed(s),
            O("Conversar e mostrar que não tem nada", "A conversa aproxima vocês.", new Fx { Love = 6 }),
            O("Fechar os comentários das redes", "Menos fãs, mais paz em casa.", new Fx { Love = 8, Fame = -2 }),
            O("Dizer que é exagero", "A discussão vai até de madrugada.", new Fx { Love = -12, Fitness = -2 })),
        Ev("r_distancia", "A agenda de jogos afasta vocês", "Viagens, concentração e treinos. {par} reclama que vocês quase não se veem.", s => Committed(s),
            O("Levar {par} na próxima viagem", "Vocês aproveitam cada folga juntos.", new Fx { Love = 9, Money = -1, Fitness = -1 }),
            O("Videochamada toda noite", "Não é a mesma coisa, mas ajuda.", new Fx { Love = 4 }),
            O("Dizer que o futebol vem primeiro", "O técnico gosta. {par}, nem um pouco.", new Fx { Love = -12, Trust = 2 })),
        Ev("r_morar", "Morar juntos?", "{par} vive mais na sua casa do que na própria.", s => Stage(s, "namorando") && Years(s) >= 1 && !s.Has("morando"),
            O("Chamar {par} para morar com você", "Duas escovas de dente no banheiro.", new Fx { Love = 12, Family = 2, Flag = "morando", Gossip = "{nome} e {par} vão morar juntos" }),
            O("Ainda é cedo", "{par} respeita, mas esperava outra resposta.", new Fx { Love = -6 })),
        Ev("r_pedido", "Hora de pedir {par} em casamento?", "Vocês estão juntos há um bom tempo e a família já pergunta quando sai o casamento.",
            s => Stage(s, "namorando") && Years(s) >= 1 && s.Love.Affection >= 60,
            O("Pedir no gramado, depois do jogo", "O estádio inteiro aplaude. Sim!", new Fx { LoveStage = "noivo", Love = 10, Fans = 3, Fame = 3, FollowUp = "r_casamento", FollowUpIn = 12, Gossip = "{nome} pede {par} em casamento no meio do gramado!" }, 0.85,
                "{par} fica sem graça com tanta gente olhando e pede para conversar em casa.", new Fx { Love = -8, Fame = 2, Gossip = "Saia justa: pedido de casamento de {nome} em campo não sai como planejado" }),
            O("Pedir num jantar a dois", "Simples e perfeito. Sim!", new Fx { LoveStage = "noivo", Love = 10, FollowUp = "r_casamento", FollowUpIn = 12, Gossip = "{nome} fica noivo de {par}" }, 0.9,
                "{par} pede mais um tempo antes de casar.", new Fx { Love = -5 }),
            O("Esperar o fim da carreira", "{par} entende, mas fica pensativa.", new Fx { Love = -6 })),

        // ---------------------------------------------------------------- noivo e casado
        Ev("r_casamento", "O casamento com {par}", "Chegou o grande dia, e a festa cai na semana de um jogo importante.", s => false,
            O("Festa enorme com o elenco e famosos", "Inesquecível. O treino seguinte, nem tanto.", new Fx { LoveStage = "casado", Love = 12, Money = -3, Locker = 5, Fitness = -3, Gossip = "Casamento de {nome} e {par} reúne craques e famosos" }),
            O("Cerimônia pequena, só a família", "Íntimo e emocionante.", new Fx { LoveStage = "casado", Love = 12, Family = 8, Gossip = "{nome} e {par} se casam em cerimônia discreta" }),
            O("Adiar o casamento", "{par} chora a noite inteira.", new Fx { Love = -18, Morale = -4, FollowUp = "r_casamento", FollowUpIn = 30 })),
        Ev("r_crise", "Crise em casa", "Vocês andam brigando por qualquer coisa. {par} diz que se sente sozinha.", s => Stage(s, "casado") && Years(s) >= 1,
            O("Fazer terapia de casal", "Aos poucos, a conversa volta.", new Fx { Love = 12, Money = -1 }),
            O("Uma viagem só vocês dois", "Duas semanas e a paixão reacende.", new Fx { Love = 10, Fitness = -2, Money = -1, Gossip = "{nome} e {par} curtem viagem romântica no Caribe" }),
            O("Mergulhar no trabalho", "Os problemas continuam em casa.", new Fx { Love = -14, Trust = 2 })),
        Ev("r_bodas", "Aniversário de casamento", "Mais um ano ao lado de {par}.", s => Stage(s, "casado") && Years(s) >= 1,
            O("Preparar uma surpresa", "Jantar à luz de velas e uma música especial.", new Fx { Love = 10, Morale = 3, Gossip = "{nome} faz surpresa romântica para {par} no aniversário de casamento" }),
            O("Esquecer a data", "{par} não fala com você o dia inteiro.", new Fx { Love = -14 })),

        // ---------------------------------------------------------------- filhos (sem limite, um por temporada)
        Ev("r_bebe_plano", "Aumentar a família?", "Depois de um almoço cheio de crianças na casa de amigos, {par} pergunta se vocês não querem ter {mais}um bebê. Hoje vocês têm {filhos}.",
            s => Committed(s) && s.Age >= 20 && !s.Love.Expecting && !s.Love.Adopting && s.Love.Children.All(c => c.BornSeason < s.Season),
            O("Vamos tentar!", "Vocês decidem que é a hora.", new Fx { Baby = "esperando", Love = 8, Morale = 3, FollowUp = "r_gravidez", FollowUpIn = 5 }, 0.75,
                "Ainda não veio, mas vocês seguem tentando.", new Fx { Love = 4 }),
            O("Adotar uma criança", "Depois de meses de processo, a criança chega em casa.", new Fx { Baby = "adotar", Love = 8, Family = 6, Morale = 6, Money = -1, FollowUp = "r_nome", FollowUpIn = 0 }),
            O("Agora não, o foco é a carreira", "{par} entende, mas o assunto volta.", new Fx { Love = -4 })),
        Ev("r_gravidez", "Vem aí {mais}um bebê!", "O teste deu positivo: {mae} está grávida{gemeos}.", s => false,
            O("Contar para a torcida no próximo gol", "A comemoração com a mão na barriga vira a foto do ano.", new Fx { Morale = 8, Fame = 3, Fans = 4, FollowUp = "r_nasceu", FollowUpIn = 18, Gossip = "{nome} comemora gol com a mão na barriga: vem aí {mais}um herdeiro!" }),
            O("Guardar a notícia só para a família", "Um segredo bom de guardar.", new Fx { Morale = 7, Family = 6, FollowUp = "r_nasceu", FollowUpIn = 18 })),
        Ev("r_nasceu", "Chegou a hora!", "{mae} entrou em trabalho de parto na madrugada antes de um jogo.", s => false,
            O("Ficar no hospital e não jogar", "O clube libera. Nada é mais importante.", new Fx { Family = 12, Morale = 8, Trust = -2, FollowUp = "r_nome", FollowUpIn = 0 }),
            O("Ver o bebê e correr para o estádio", "Você joga com a cabeça no hospital.", new Fx { Family = 4, Morale = 5, Trust = 3, Fitness = -3, FollowUp = "r_nome", FollowUpIn = 0 })),
        Ev("r_nome", "Qual vai ser o nome?", "{sexo} Agora vocês precisam escolher o nome.", s => false,
            O("{op0}", "Bem-vindo à família!", new Fx { Baby = "nome0", Morale = 3 }),
            O("{op1}", "Bem-vindo à família!", new Fx { Baby = "nome1", Morale = 3 }),
            O("{op2}", "Bem-vindo à família!", new Fx { Baby = "nome2", Morale = 3 }),
            O("{op3}", "Bem-vindo à família!", new Fx { Baby = "nome3", Morale = 3 })),

        // ---------------------------------------------------------------- os filhos crescendo
        Ev("r_filho_doente", "{filho} está com febre alta", "É véspera de clássico e {filho} passou a tarde no pronto-socorro.", s => Kid(s, "r_filho_doente"),
            O("Passar a noite no hospital", "{filho} melhora de manhã. Você joga sem dormir.", new Fx { Family = 8, Fitness = -4 }),
            O("Deixar com a família e se concentrar", "Você joga bem, mas o coração ficou em casa.", new Fx { Family = -5, Trust = 2 })),
        Ev("r_filho_aniv", "Aniversário de {filho}", "A festa de {filho} cai no mesmo dia de um jogo fora de casa.", s => Kid(s, "r_filho_aniv"),
            O("Pedir liberação e ir à festa", "{filho} nunca vai esquecer o dia.", new Fx { Family = 8, Trust = -2 }),
            O("Jogar e prometer um gol para {filho}", "Gol! Você corre para a câmera e faz a dedicatória.", new Fx { Family = 4, Fans = 3, Morale = 4 }, 0.5,
                "Sem gol, e {filho} chora no telefone.", new Fx { Family = -4, Morale = -2 }),
            O("Mandar um presente caro", "O presente é lindo, mas {filho} queria você lá.", new Fx { Money = -1, Family = 1 })),
        Ev("r_filho_escola", "A escola de {filho}", "Chegou a hora de escolher a escola de {filho}.", s => Kid(s, "r_filho_escola"),
            O("Escola particular bilíngue", "Cara, mas com estrutura de primeira.", new Fx { Money = -2, Family = 4 }),
            O("A escola do bairro, perto de casa", "{filho} faz amigos de verdade e você leva todo dia que pode.", new Fx { Family = 5, Fans = 1 }),
            O("Escola com time de futebol", "{filho} já sai de chuteira na mochila.", new Fx { Family = 3, Morale = 2 })),
        Ev("r_filho_bola", "{filho} quer ser jogador", "{filho} não larga a bola e diz que quer jogar igual a você.", s => Kid(s, "r_filho_bola"),
            O("Treinar junto no quintal", "As tardes mais felizes da semana.", new Fx { Family = 7, Morale = 4, Fitness = -1 }),
            O("Colocar na escolinha da base do clube", "Os jornais adoram a história.", new Fx { Family = 3, Fame = 2, Gossip = "Filho de peixe: {filho} entra na escolinha e segue os passos de {nome}" }),
            O("Pedir para focar nos estudos", "{filho} fica chateado, mas obedece.", new Fx { Family = -3 })),
        Ev("r_filho_teen", "{filho} aprontou na escola", "A diretora ligou: {filho} se meteu numa confusão e pode ser suspenso.", s => Kid(s, "r_filho_teen"),
            O("Conversar com calma", "Vocês conversam como nunca antes.", new Fx { Family = 6 }),
            O("Castigo: sem celular por um mês", "{filho} fica bravo, mas a lição fica.", new Fx { Family = -2, Morale = 1 }),
            O("Defender {filho} publicamente", "Vira notícia, e nem sempre boa.", new Fx { Family = 2, Fans = -2, Gossip = "{nome} sai em defesa de {filho} após confusão na escola" })),

        // ---------------------------------------------------------------- tentações e escândalos
        Ev("r_tentacao", "Uma investida na concentração", "Uma fã conseguiu entrar no hotel e bate na porta do seu quarto, na véspera do jogo.", s => Committed(s),
            O("Fechar a porta e ligar para {par}", "Você conta tudo e {par} adora a sinceridade.", new Fx { Love = 6, Fitness = 1 }),
            O("Conversar um pouco no corredor", "Ela vai embora e ninguém vê nada.", new Fx { Morale = 1 }, 0.65,
                "Alguém filma a conversa e o vídeo vaza.", new Fx { Love = -14, Fans = -2, Gossip = "Fã é vista saindo do andar de {nome} na concentração" }),
            O("Trair {par}", "Ninguém ficou sabendo. Por enquanto.", new Fx { Morale = -3, Fitness = -2, Flag = "traiu", FollowUp = "r_culpa", FollowUpIn = 15 }, 0.5,
                "Um paparazzi estava no hotel.", new Fx { Love = -40, Fans = -4, Fame = 3, Flag = "traiu", FollowUp = "r_flagra", FollowUpIn = 1, Gossip = "FLAGRA! {nome} é fotografado com outra mulher na véspera do jogo" })),
        Ev("r_ex", "Mensagem da ex", "Uma ex-namorada manda mensagem: \"Saudades... vamos tomar um café?\"", s => Committed(s) && s.Love.Milestones.Count(m => m.Icon == "💔") > 0,
            O("Não responder e apagar", "Página virada.", new Fx { Love = 3 }),
            O("Contar para {par}", "{par} agradece a confiança.", new Fx { Love = 6 }),
            O("Marcar o café escondido", "O café vira um jantar.", new Fx { Morale = -2, Flag = "traiu", FollowUp = "r_culpa", FollowUpIn = 12 }, 0.55,
                "Alguém tira foto de vocês dois no restaurante.", new Fx { Love = -35, Flag = "traiu", FollowUp = "r_flagra", FollowUpIn = 1, Gossip = "FLAGRA! {nome} janta com a ex às escondidas" })),
        Ev("r_culpa", "A culpa pesa", "Desde aquela noite, você não consegue olhar nos olhos de {par}.", s => false,
            O("Contar a verdade para {par}", "Depois de muito choro, {par} decide te dar outra chance.", new Fx { Love = -20, Morale = -3 }, 0.45,
                "{par} faz as malas na mesma noite.", new Fx { LoveStage = "separado", Morale = -8, Gossip = "{par} termina com {nome} após confissão de traição" }),
            O("Guardar o segredo", "O segredo continua guardado.", new Fx { Morale = -3 }, 0.6,
                "Uma revista descobre e publica tudo.", new Fx { Love = -40, Fans = -4, FollowUp = "r_flagra", FollowUpIn = 1, Gossip = "Revista revela: {nome} traiu {par}" })),
        Ev("r_flagra", "{par} viu as fotos", "As fotos estão em todos os sites de fofoca. {par} te espera na sala, em silêncio.", s => false,
            O("Pedir perdão e prometer mudar", "{par} aceita tentar de novo, mas a confiança vai demorar a voltar.", new Fx { Love = -5, Morale = -4, Gossip = "{par} dá nova chance a {nome} depois da traição" }, 0.4,
                "{par} não consegue perdoar.", new Fx { LoveStage = "separado", Morale = -8, Gossip = "Fim! {par} termina com {nome} depois do flagra" }),
            O("Negar tudo", "{par} acredita, por enquanto.", new Fx { Love = -10 }, 0.25,
                "Novas fotos aparecem e a mentira cai.", new Fx { LoveStage = "separado", Fans = -4, Morale = -8, Gossip = "{nome} nega traição, mas novas fotos aparecem" }),
            O("Assumir o erro e terminar", "Vocês se separam de vez.", new Fx { LoveStage = "separado", Morale = -6, Gossip = "{nome} e {par} anunciam separação" })),
        Ev("r_termino", "{par} quer terminar", "{par} diz que a relação esfriou e que não aguenta mais.", s => false,
            O("Lutar pela relação", "Vocês conversam a noite toda e decidem recomeçar.", new Fx { Love = 25, Morale = 2 }, 0.45,
                "Não tem volta. {par} vai embora.", new Fx { LoveStage = "separado", Morale = -6, Gossip = "Acabou: {nome} e {par} terminam o relacionamento" }),
            O("Aceitar o fim", "Cada um segue o seu caminho.", new Fx { LoveStage = "separado", Morale = -4, Gossip = "{nome} e {par} terminam o relacionamento" })),

        // ---------------------------------------------------------------- separado
        Ev("r_recomeco", "Vida nova depois do fim", "Já faz um tempo desde a separação de {par}.", s => Stage(s, "separado") && Years(s) >= 1,
            O("Seguir em frente", "Você volta a sorrir.", new Fx { LoveStage = "solteiro", Morale = 5 }),
            O("Tentar reatar com {par}", "{par} aceita um café... e vocês voltam!", new Fx { LoveStage = "namorando", Love = 45, Morale = 6, Gossip = "Voltaram! {nome} e {par} reatam o namoro" }, 0.3,
                "{par} já está com outra pessoa.", new Fx { LoveStage = "solteiro", Morale = -4 })),
    };

    /// <summary>Faixa de idade dos filhos para cada evento (o evento escolhe um filho nessa faixa).</summary>
    public static readonly Dictionary<string, (int Min, int Max)> KidAges = new()
    {
        ["r_filho_doente"] = (0, 8), ["r_filho_aniv"] = (1, 15), ["r_filho_escola"] = (4, 7), ["r_filho_bola"] = (6, 13), ["r_filho_teen"] = (13, 18),
    };

    private static bool Kid(Career s, string id) =>
        KidAges[id] is var (min, max) && s.Love.Children.Any(c => s.Age - c.BornAge >= min && s.Age - c.BornAge <= max);

    public static readonly HashSet<string> RomanceIds = Romance.Select(e => e.Id).ToHashSet();
}
