#nullable enable
using Camisa10.Domain.Model;
namespace Camisa10.Domain.Game;

using static EventDef;

/// <summary>
/// Histórias que fazem cada carreira ser diferente:
/// <list type="bullet">
/// <item>a origem (o_*): abre a carreira e libera arcos próprios;</item>
/// <item>arcos (a_*): histórias em várias partes, com consequências que voltam meses ou temporadas depois
/// (o FollowUp agenda a continuação, às vezes com destinos diferentes conforme a escolha e o d20);</item>
/// <item>reputação (p_*): eventos que só aparecem para quem tem certa fama (honesto, polêmico, solidário...).</item>
/// </list>
/// {amigo} é o amigo de infância, {nome} o jogador.
/// </summary>
public static partial class Events
{
    private static bool Org(Career s, string id) => s.Origin == id;
    private static bool Pro(Career s) => !s.Base && s.Age >= 19;

    public static readonly IReadOnlyList<EventDef> StoryEvents = new List<EventDef>
    {
        // ================================================================ origens (abrem a carreira)
        Ev("o_comunidade", "Cria da comunidade", "Você cresceu jogando descalço no campinho de terra. No dia em que entrou na base, a rua inteira foi te ver sair de casa.", s => false,
            O("Prometer voltar para ajudar o bairro", "O bairro inteiro guarda a promessa.", new Fx { Fans = 3, Morale = 4, Rep = "solidario:2", Flag = "promessa_bairro" }),
            O("Prometer tirar a família dali", "A mãe chora de orgulho.", new Fx { Family = 6, Morale = 4, Rep = "ambicao:2" }),
            O("Só pensar no próximo treino", "Cabeça no futebol.", new Fx { Rep = "disciplina:2", Trust = 2 })),
        Ev("o_filho_craque", "Filho de ex-jogador", "Na apresentação da base, um repórter pergunta: \"Você vai ser melhor que o seu pai?\"", s => false,
            O("\"Vou ser melhor, sim\"", "A frase vira manchete e a pressão aumenta.", new Fx { Fame = 3, Morale = 2, Rep = "ambicao:2,polemica:1" }),
            O("\"Quero fazer a minha própria história\"", "Resposta madura, elogiada até pelo pai.", new Fx { Family = 4, Rep = "carater:1" }),
            O("Deixar o pai responder por você", "O pai rouba a cena, e você fica na sombra.", new Fx { Morale = -3, Agent = 3 })),
        Ev("o_rico", "Família rica", "No primeiro dia, você chega à base de carro importado. Os colegas vieram de ônibus.", s => false,
            O("Pedir para o motorista te deixar de ônibus daqui para frente", "Os colegas começam a te respeitar.", new Fx { Locker = 6, Rep = "carater:1,disciplina:1" }),
            O("Pagar um churrasco para o elenco", "Todo mundo come, mas tem quem torça o nariz.", new Fx { Locker = 2, Money = -1, Rep = "solidario:1" }),
            O("Não ligar para o que pensam", "Você segue do seu jeito.", new Fx { Locker = -4, Rep = "polemica:1" })),
        Ev("o_interior", "Menino do interior", "A cidade fez uma festa de despedida. O prefeito promete um busto na praça se você chegar à seleção.", s => false,
            O("Prometer voltar campeão", "A cidade cola o seu pôster em cada padaria.", new Fx { Fans = 3, Morale = 4, Rep = "lealdade:2" }),
            O("Levar o pai junto para a cidade grande", "Ele arruma emprego perto do clube.", new Fx { Family = 6, Money = -1 }),
            O("Ir sozinho e aprender a se virar", "A saudade aperta, mas você amadurece.", new Fx { Morale = -2, Rep = "disciplina:2" })),
        Ev("o_avo", "Criado pela avó", "A sua avó juntou dinheiro a vida inteira para a sua primeira chuteira. Hoje ela te entrega um envelope com o que sobrou.", s => false,
            O("Recusar e dizer que agora é a sua vez de cuidar dela", "Ela chora e te abraça.", new Fx { Family = 8, Rep = "carater:2,solidario:1", Flag = "avo_viva" }),
            O("Aceitar e prometer devolver em dobro", "Você guarda o envelope como amuleto.", new Fx { Money = 1, Morale = 4, Flag = "avo_viva" })),
        Ev("o_igreja", "Filho de pastor", "O seu pai quer que você leia um versículo para a igreja antes de ir para a concentração da base.", s => false,
            O("Ler com orgulho", "A igreja inteira passa a te acompanhar.", new Fx { Fans = 3, Family = 4, Rep = "carater:1" }),
            O("Pedir para manter a fé mais reservada", "O pai entende, meio decepcionado.", new Fx { Family = -2, Rep = "polemica:-1" })),
        Ev("o_peneiras", "Dez \"nãos\" depois", "Na décima primeira peneira, você passou. O olheiro que te aprovou diz: \"Muitos desistem na terceira\".", s => false,
            O("Guardar a lista dos dez clubes que te recusaram", "Ela vira o seu combustível.", new Fx { Morale = 6, Rep = "ambicao:2", Flag = "lista_peneiras" }),
            O("Agradecer a cada um que te ajudou no caminho", "Gente boa não esquece.", new Fx { Friends = 5, Rep = "carater:1" })),
        Ev("o_influencer", "Famoso nas redes", "Uma marca de energético quer pagar para você postar vídeos toda semana, inclusive no dia dos jogos da base.", s => false,
            O("Aceitar o contrato", "Dinheiro e seguidores. O técnico da base não gosta.", new Fx { Money = 2, Fame = 3, Trust = -4, Rep = "ambicao:2,disciplina:-1" }),
            O("Recusar e focar no futebol", "Você posta menos, mas treina mais.", new Fx { Trust = 4, Rep = "disciplina:2" }),
            O("Aceitar, mas nunca no dia de jogo", "O meio-termo agrada quase todo mundo.", new Fx { Money = 1, Fame = 2 })),

        // ================================================================ arco do amigo de infância
        Ev("a_amigo1", "{amigo} pede ajuda", "{amigo}, seu amigo de infância, se envolveu com gente perigosa. Ele diz que precisa de R$ 1 mi para sair dessa.",
            s => s.Age >= 17 && s.Age <= 24 && s.Money >= 1.2 && !s.Has("amigo"),
            O("Dar o dinheiro e arrumar um emprego para ele", "{amigo} jura que vai mudar.", new Fx { Money = -1, Friends = 6, Rep = "solidario:3", Flag = "amigo", FollowUp = "a_amigo2", FollowUpIn = 60 }),
            O("Negar, mas conversar com a família dele", "Você tenta ajudar de outro jeito.", new Fx { Friends = 2, Rep = "carater:1", Flag = "amigo", FollowUp = "a_amigo_preso", FollowUpIn = 50 }),
            O("Se afastar dele de vez", "Melhor não misturar.", new Fx { Friends = -6, Rep = "solidario:-2", Flag = "amigo", FollowUp = "a_amigo_preso", FollowUpIn = 40 })),
        Ev("a_amigo2", "{amigo} está trabalhando", "Faz meses que {amigo} trabalha como seu motorista. Hoje ele confessa que antigos \"sócios\" voltaram a procurá-lo.", s => false,
            O("Pagar um advogado e protegê-lo", "{amigo} se livra dos problemas de vez.", new Fx { Money = -1, Morale = 4, Rep = "solidario:2", Flag = "amigo_salvo" }, 0.65,
                "Não adiantou: {amigo} é preso com eles, e o seu carro aparece na foto.", new Fx { Fans = -4, Morale = -6, Flag = "amigo_preso", Gossip = "Motorista de {nome} é preso; carro do jogador aparece na operação" }),
            O("Mandar ele embora para se proteger", "{amigo} vai embora magoado.", new Fx { Friends = -5, Rep = "solidario:-1", FollowUp = "a_amigo_preso", FollowUpIn = 30 })),
        Ev("a_amigo_preso", "{amigo} foi preso", "A notícia chega pelo grupo da família: {amigo} foi preso.", s => false,
            O("Visitar ele na cadeia", "Ele chora quando te vê.", new Fx { Morale = -3, Friends = 4, Rep = "carater:1", Flag = "amigo_preso", FollowUp = "a_amigo_volta", FollowUpIn = 200 }),
            O("Pagar a defesa", "O advogado consegue reduzir a pena.", new Fx { Money = -1, Rep = "solidario:2", Flag = "amigo_preso", FollowUp = "a_amigo_volta", FollowUpIn = 120 }),
            O("Seguir a vida", "Você evita pensar nisso.", new Fx { Morale = -2, Flag = "amigo_preso" })),
        Ev("a_amigo_volta", "{amigo} saiu da cadeia", "{amigo} está livre e te espera no portão do CT. Diz que mudou e quer trabalhar no seu instituto ou nas suas empresas.", s => false,
            O("Dar uma nova chance", "Hoje ele é o funcionário mais dedicado que você tem.", new Fx { Friends = 6, Rep = "solidario:2", Flag = "amigo_salvo", Gossip = "Ex-detento, amigo de infância de {nome} ganha emprego do craque" }),
            O("Ajudar com dinheiro, mas de longe", "Ele agradece e some.", new Fx { Money = -0.5 })),

        // ================================================================ apostas e manipulação
        Ev("a_aposta1", "Uma proposta indecente", "Um homem te aborda no estacionamento: R$ 2 mi para você levar um cartão amarelo no primeiro tempo do próximo jogo.", s => Pro(s) && !s.Has("aposta"),
            O("Aceitar: é só um cartão", "O dinheiro cai numa conta no exterior.", new Fx { Money = 2, Rep = "carater:-5", Flag = "aposta", FollowUp = "a_aposta2", FollowUpIn = 90 }),
            O("Recusar e ir embora", "Você finge que nada aconteceu.", new Fx { Flag = "aposta", Rep = "carater:1" }),
            O("Denunciar à polícia e ao clube", "Você vira testemunha numa operação contra a máfia das apostas.", new Fx { Fame = 4, Fans = 5, Rep = "carater:5", Flag = "aposta", FollowUp = "a_aposta_ameaca", FollowUpIn = 40, Gossip = "{nome} denuncia esquema de apostas e vira herói da torcida" })),
        Ev("a_aposta2", "A polícia bate na porta", "Uma operação contra manipulação de resultados encontrou mensagens com o seu número.", s => false,
            O("Confessar e colaborar com a investigação", "Pena menor: 10 jogos de suspensão e uma multa pesada.", new Fx { Injury = 10, Money = -3, Fans = -12, Fame = 3, Rep = "carater:2", Gossip = "{nome} confessa envolvimento em esquema de apostas" }),
            O("Negar tudo", "Sem provas suficientes, o caso é arquivado.", new Fx { Morale = -4 }, 0.45,
                "As provas aparecem. Você é banido do futebol.", new Fx { End = "ban", Fans = -30, Gossip = "BANIDO! {nome} é condenado por manipulação de resultados" })),
        Ev("a_aposta_ameaca", "Ameaças", "Depois da denúncia, você recebe ameaças anônimas.", s => false,
            O("Contratar seguranças", "A família fica mais tranquila.", new Fx { Money = -1, Family = 3 }),
            O("Ir a público e falar sobre as ameaças", "A imprensa abraça a causa.", new Fx { Fame = 3, Fans = 3, Rep = "polemica:2" })),

        // ================================================================ doping
        Ev("a_dop1", "A \"vitamina\" milagrosa", "Um preparador particular oferece um suplemento que \"ninguém detecta\" e acelera a recuperação.", s => Pro(s) && !s.Has("dop_oferta"),
            O("Tomar", "Você nunca se sentiu tão forte.", new Fx { Fitness = 15, Rep = "carater:-3", Flag = "dop_oferta", FollowUp = "a_dop2", FollowUpIn = 35 }),
            O("Recusar", "Melhor não arriscar.", new Fx { Flag = "dop_oferta", Rep = "disciplina:1" }),
            O("Levar ao médico do clube", "Era uma substância proibida. O médico te agradece.", new Fx { Trust = 5, Flag = "dop_oferta", Rep = "carater:2,disciplina:2" })),
        Ev("a_dop2", "Exame antidoping", "Você foi sorteado para o exame antidoping depois do jogo.", s => false,
            O("Fazer o exame e torcer", "Negativo. O preparador tinha razão.", new Fx { Morale = 2 }, 0.5,
                "Positivo. Suspensão de 15 jogos e o nome em todos os jornais.", new Fx { Injury = 15, Fans = -10, Fame = 2, Trust = -10, Flag = "doping", Gossip = "Doping! {nome} é pego no exame e pega suspensão" }),
            O("Fingir uma lesão para escapar do exame", "O médico desconfia, mas libera.", new Fx { Trust = -5, Rep = "carater:-2" }, 0.6,
                "A manobra é descoberta: suspensão em dobro.", new Fx { Injury = 20, Fans = -14, Trust = -12, Flag = "doping", Gossip = "{nome} tenta fugir do antidoping e é punido" })),

        // ================================================================ mentor
        Ev("a_mentor1", "O veterano quer te ensinar", "O jogador mais experiente do elenco diz que vê muito dele em você e quer te treinar depois dos treinos.", s => Pro(s) && s.Age <= 22 && !s.Has("mentor"),
            O("Aceitar e ficar depois de todo treino", "Você aprende truques que nenhum técnico ensina.", new Fx { Skill = 2, Fitness = -2, Locker = 4, Rep = "disciplina:3", Flag = "mentor", FollowUp = "a_mentor2", FollowUpIn = 250 }),
            O("Agradecer, mas seguir o seu caminho", "Ele respeita.", new Fx { Flag = "mentor" }),
            O("Rir da ideia com os amigos", "A história chega até ele.", new Fx { Locker = -6, Rep = "carater:-1,polemica:1", Flag = "mentor" })),
        Ev("a_mentor2", "O mentor virou técnico", "O veterano que te ensinou agora é técnico e quer você como líder do time dele.", s => false,
            O("Aceitar o convite e jogar para ele", "Você vira o braço direito do técnico.", new Fx { Trust = 10, Locker = 5, Flag = "mentor_tecnico", Rep = "lealdade:2" }),
            O("Agradecer, mas ficar onde está", "Ele entende e segue torcendo por você.", new Fx { Morale = 2 })),

        // ================================================================ saúde mental
        Ev("a_mente1", "Você não consegue dormir", "Faz semanas que a ansiedade não te deixa dormir antes dos jogos.", s => Pro(s) && s.Morale <= 45 && !s.Has("mente"),
            O("Procurar uma psicóloga", "Aos poucos, a cabeça volta para o lugar.", new Fx { Morale = 10, Money = -0.3, Flag = "mente", FollowUp = "a_mente2", FollowUpIn = 80 }),
            O("Esconder e treinar mais", "O corpo sente antes da cabeça.", new Fx { Fitness = -6, Morale = -4, Flag = "mente", FollowUp = "a_mente3", FollowUpIn = 40 }),
            O("Desabafar com a família", "Ajuda, mas não resolve tudo.", new Fx { Family = 4, Morale = 4, Flag = "mente" })),
        Ev("a_mente2", "Falar em público?", "Um programa de TV quer que você conte como venceu a ansiedade.", s => false,
            O("Contar a sua história", "Milhares de mensagens de jovens agradecendo.", new Fx { Fans = 8, Fame = 4, Rep = "solidario:2", Flag = "mente_forte" }),
            O("Manter isso só para você", "Privacidade também é cuidado.", new Fx { Morale = 2 })),
        Ev("a_mente3", "Crise de pânico no vestiário", "Minutos antes do jogo, você não consegue respirar.", s => false,
            O("Pedir para não jogar e buscar ajuda", "O clube apoia e você começa o tratamento.", new Fx { Trust = -2, Morale = 8, Flag = "mente_forte" }),
            O("Entrar em campo assim mesmo", "Você joga mal e é substituído no intervalo.", new Fx { Morale = -8, Fans = -3 })),

        // ================================================================ política, música e mídia
        Ev("a_pol1", "Convite para a política", "Um partido quer você como candidato a deputado na próxima eleição.", s => Pro(s) && s.Age >= 27 && s.Fame >= 45 && !s.Has("politica"),
            O("Aceitar a candidatura", "A campanha começa, e metade da torcida não gosta.", new Fx { Fans = -6, Rep = "polemica:3,ambicao:2", Flag = "politica", FollowUp = "a_pol2", FollowUpIn = 60 }),
            O("Declarar apoio a um candidato", "Você divide a torcida e vira assunto da semana.", new Fx { Fans = -4, Fame = 3, Rep = "polemica:4", Flag = "politica", Gossip = "{nome} declara voto e divide a torcida" }),
            O("Ficar fora da política", "O futebol já dá trabalho demais.", new Fx { Flag = "politica", Rep = "polemica:-1" })),
        Ev("a_pol2", "Dia da eleição", "As urnas fecharam. Você conseguiu conciliar treinos e campanha?", s => false,
            O("Acompanhar a apuração com a família", "ELEITO! Você vai cumprir o mandato depois de pendurar as chuteiras.", new Fx { Fame = 6, Morale = 8, Flag = "eleito", Gossip = "{nome} é eleito deputado" }, 0.55,
                "Não deu. Faltaram votos.", new Fx { Morale = -6, Fame = 1 })),
        Ev("a_mus1", "Uma música com um cantor famoso", "Um cantor de pagode quer gravar uma música com você e lançar no verão.", s => Pro(s) && s.Fame >= 25 && !s.Has("musica"),
            O("Gravar a música", "Você passa dias no estúdio.", new Fx { Fitness = -2, Flag = "musica", FollowUp = "a_mus2", FollowUpIn = 30 }),
            O("Recusar: a voz é do cantor", "Você manda um abraço no vídeo dele.", new Fx { Flag = "musica" })),
        Ev("a_mus2", "A música foi lançada", "Chegou o dia do lançamento.", s => false,
            O("Fazer a dancinha na comemoração do gol", "Vira hit! A música toca em todas as festas.", new Fx { Fame = 8, Money = 2, Flag = "cantor", Gossip = "Hit do verão: música de {nome} bate recorde" }, 0.5,
                "Ninguém ouviu. Os memes, todo mundo viu.", new Fx { Fame = 2, Morale = -3, Rep = "polemica:1", Gossip = "Música de {nome} vira piada nas redes" }),
            O("Deixar a música falar por si", "Faz um sucesso modesto.", new Fx { Fame = 3, Money = 0.5 })),
        Ev("a_jorn1", "O jornalista que te persegue", "Um comentarista famoso te critica toda semana na TV. Hoje ele disse que você é \"supervalorizado\".", s => Pro(s) && s.Fame >= 20 && !s.Has("jornalista"),
            O("Responder nas redes", "A treta viraliza.", new Fx { Fame = 3, Rep = "polemica:3", Flag = "jornalista", FollowUp = "a_jorn2", FollowUpIn = 45, Gossip = "Treta! {nome} rebate comentarista nas redes" }),
            O("Ignorar e responder em campo", "O silêncio também fala.", new Fx { Rep = "polemica:-1,disciplina:1", Flag = "jornalista" }),
            O("Convidar o jornalista para um café", "A conversa surpreende os dois.", new Fx { Rep = "carater:2", Flag = "jornalista", FollowUp = "a_jorn3", FollowUpIn = 45 })),
        Ev("a_jorn2", "A guerra com a imprensa", "Depois da treta, a imprensa inteira está de olho em cada erro seu.", s => false,
            O("Boicotar entrevistas", "O clube é multado e o técnico não gosta.", new Fx { Trust = -5, Money = -0.5, Rep = "polemica:3" }),
            O("Pedir desculpas publicamente", "A poeira baixa.", new Fx { Fame = -1, Rep = "carater:1,polemica:-2" })),
        Ev("a_jorn3", "O jornalista mudou de lado", "O comentarista que te criticava agora te defende na TV.", s => false,
            O("Agradecer em público", "A imprensa passa a te tratar melhor.", new Fx { Fame = 3, Fans = 2 })),

        // ================================================================ torcida e lealdade
        Ev("a_torc1", "A torcida organizada", "Líderes da organizada querem que você pague a viagem deles para a final fora de casa.", s => Pro(s) && s.Fans >= 50 && !s.Has("organizada"),
            O("Pagar a viagem", "Você vira o queridinho da arquibancada.", new Fx { Money = -0.5, Fans = 6, Rep = "lealdade:2", Flag = "organizada", FollowUp = "a_torc2", FollowUpIn = 50 }),
            O("Recusar com educação", "Eles não gostam nada.", new Fx { Fans = -3, Flag = "organizada", FollowUp = "a_torc3", FollowUpIn = 30 })),
        Ev("a_torc2", "Briga na viagem", "A organizada que você ajudou se envolveu numa briga violenta na viagem.", s => false,
            O("Condenar a violência publicamente", "A organizada se revolta, mas o resto do país aplaude.", new Fx { Fans = -4, Fame = 2, Rep = "carater:2" }),
            O("Ficar calado", "As fotos com você na sede da torcida circulam.", new Fx { Rep = "carater:-2,polemica:2", Gossip = "{nome} é cobrado por ligação com organizada envolvida em briga" })),
        Ev("a_torc3", "Pichação no muro do CT", "Pintaram o seu nome com xingamentos no muro do CT.", s => false,
            O("Fazer as pazes com uma visita à sede", "Clima resolvido.", new Fx { Fans = 4 }),
            O("Responder com gols", "Dois gols no clássico seguinte e a pichação é apagada.", new Fx { Morale = 3 }, 0.5,
                "Você vai mal e a pressão aumenta.", new Fx { Morale = -5, Fans = -3 })),
        Ev("a_coracao", "O clube onde tudo começou", "O clube que te revelou está quase falindo e pode fechar as portas da base.", s => Pro(s) && s.Age >= 27 && s.Money >= 8 && !s.Has("coracao"),
            O("Doar R$ 5 mi para salvar a base", "O CT ganha o seu nome.", new Fx { Money = -5, Fans = 8, Fame = 3, Rep = "lealdade:4,solidario:3", Flag = "salvou_clube", Gossip = "{nome} doa R$ 5 mi e salva o clube que o revelou" }),
            O("Organizar um jogo beneficente", "Estádio lotado e dinheiro para alguns meses.", new Fx { Fans = 4, Fitness = -2, Rep = "lealdade:2,solidario:1", Flag = "coracao" }),
            O("Não é problema seu", "O clube fecha a base meses depois.", new Fx { Fans = -5, Rep = "lealdade:-3", Flag = "coracao" })),
        Ev("a_rival_proposta", "Proposta do maior rival", "O maior rival do seu clube oferece o dobro do salário. A sua torcida nunca perdoaria.", s => Pro(s) && s.Rep.Loyalty >= 3 && !s.Has("rival_prop"),
            O("Recusar e beijar o escudo", "Você vira ídolo eterno.", new Fx { Fans = 12, Rep = "lealdade:4", Flag = "rival_prop" }),
            O("Usar a proposta para renovar com aumento", "O clube cobre, mas a torcida fica sabendo.", new Fx { SalaryMult = 1.3, Fans = -4, Rep = "ambicao:2,lealdade:-1", Flag = "rival_prop" })),

        // ================================================================ arcos das origens
        Ev("a_pai_emp", "Seu pai quer ser seu empresário", "Seu pai, ex-jogador, quer cuidar da sua carreira no lugar do empresário.", s => Org(s, "filho_craque") && Pro(s) && !s.Has("pai_emp"),
            O("Aceitar", "Ninguém conhece o futebol como ele.", new Fx { Agent = 8, Family = 4, Flag = "pai_emp", FollowUp = "a_pai_briga", FollowUpIn = 150 }),
            O("Manter o empresário", "O pai fica magoado.", new Fx { Family = -6, Flag = "pai_emp" })),
        Ev("a_pai_briga", "Briga por dinheiro com o pai", "Você descobre que o seu pai ficou com uma comissão alta demais no último contrato.", s => false,
            O("Conversar e perdoar", "Família é família.", new Fx { Family = 4, Money = -1 }),
            O("Demitir o pai como empresário", "Vocês ficam meses sem se falar.", new Fx { Family = -10, Agent = -5, Gossip = "Racha na família: {nome} demite o pai de empresário" })),
        Ev("a_rico_empresa", "A empresa da família", "Seu pai quer que você assuma a empresa da família quando parar de jogar, e que já comece a ir às reuniões.", s => Org(s, "rico") && s.Age >= 24 && !s.Has("rico_emp"),
            O("Começar a ir às reuniões", "Você aprende a administrar, e o patrimônio da família passa a ser seu também.", new Fx { Money = 10, Fitness = -3, Flag = "rico_emp", Rep = "ambicao:2" }),
            O("Dizer que a sua vida é o futebol", "O pai respeita, mas não esconde a tristeza.", new Fx { Family = -4, Rep = "disciplina:1", Flag = "rico_emp" })),
        Ev("a_avo_doente", "Sua avó ficou doente", "A sua avó foi internada. O tratamento particular custa caro e ela pede para você não faltar aos jogos.", s => Org(s, "avo") && s.Age >= 20 && s.Has("avo_viva") && !s.Has("avo_doente"),
            O("Pagar o melhor tratamento e ficar com ela", "Ela se recupera e volta para a arquibancada.", new Fx { Money = -2, Family = 8, Trust = -3, Flag = "avo_doente" }, 0.75,
                "Ela não resistiu. Você joga o próximo jogo por ela.", new Fx { Money = -2, Morale = -12, Family = 4, Flag = "avo_doente", Gossip = "{nome} homenageia a avó que o criou" }),
            O("Jogar, como ela pediu", "Você faz um gol e corre para a câmera com a foto dela.", new Fx { Morale = 4, Fans = 4, Flag = "avo_doente" })),
        Ev("a_interior_seca", "Seca na sua cidade", "A seca destruiu as plantações da sua cidade natal. O prefeito te liga pedindo ajuda.", s => Org(s, "interior") && Pro(s) && s.Money >= 3 && !s.Has("seca"),
            O("Enviar caminhões de água e cestas básicas", "A cidade nunca vai esquecer.", new Fx { Money = -2, Fans = 5, Rep = "solidario:3,lealdade:1", Flag = "seca" }),
            O("Fazer uma campanha nas redes", "Os seguidores doam, você divulga.", new Fx { Fame = 2, Rep = "solidario:1", Flag = "seca" }),
            O("Agora não dá", "A cidade fica decepcionada.", new Fx { Rep = "solidario:-2,lealdade:-1", Flag = "seca" })),
        Ev("a_fe_pastor", "O pastor pede uma oferta", "Um pastor da igreja do seu pai pede uma oferta de R$ 3 mi para construir um templo.", s => Org(s, "igreja") && Pro(s) && s.Money >= 4 && !s.Has("oferta"),
            O("Doar", "O templo ganha uma placa com o seu nome.", new Fx { Money = -3, Family = 4, Rep = "solidario:2", Flag = "oferta" }),
            O("Doar para um hospital no lugar", "O seu pai entende o gesto.", new Fx { Money = -2, Fans = 4, Rep = "solidario:3", Flag = "oferta" }),
            O("Recusar", "O assunto vira fofoca na igreja.", new Fx { Family = -4, Flag = "oferta" })),
        Ev("a_peneira_volta", "Um dos dez clubes te quer", "Um dos clubes que te reprovaram na peneira agora oferece uma fortuna por você.", s => Org(s, "peneiras") && Pro(s) && s.Overall >= 70 && !s.Has("peneira_volta"),
            O("Recusar e mandar um recado", "\"Vocês tiveram a chance.\" A frase viraliza.", new Fx { Fame = 4, Morale = 6, Rep = "polemica:2", Flag = "peneira_volta", Gossip = "{nome} recusa clube que o reprovou: \"Vocês tiveram a chance\"" }),
            O("Usar a proposta para valorizar o passe", "O seu clube aumenta o seu salário.", new Fx { SalaryMult = 1.2, Rep = "ambicao:1", Flag = "peneira_volta" })),
        Ev("a_cancelado", "Um tweet antigo", "Desenterraram uma publicação ofensiva que você fez aos 14 anos. A internet quer te cancelar.", s => Org(s, "influencer") && Pro(s) && !s.Has("cancelado"),
            O("Pedir desculpas sinceras", "A maioria aceita.", new Fx { Fame = -2, Rep = "carater:2", Flag = "cancelado" }),
            O("Dizer que era só uma criança", "Metade concorda, metade não.", new Fx { Fans = -3, Rep = "polemica:2", Flag = "cancelado" }),
            O("Apagar as redes por um mês", "Você some e o assunto morre.", new Fx { Fame = -4, Morale = 3, Flag = "cancelado" })),

        // ================================================================ só para certas reputações
        Ev("p_luxo", "Uma marca de luxo te quer", "Uma grife internacional quer você como rosto da nova campanha. Eles só fecham com atletas de imagem limpa.",
            s => Pro(s) && s.Fame >= 35 && s.Rep.Character >= 4 && s.Rep.Controversy < 6 && !s.Has("luxo"),
            O("Assinar o contrato", "Outdoors pelo mundo inteiro.", new Fx { Money = 6, Fame = 5, Flag = "luxo" }),
            O("Pedir para doar parte para o seu projeto social", "A marca adora a ideia.", new Fx { Money = 4, Fame = 5, Fans = 3, Rep = "solidario:2", Flag = "luxo" })),
        Ev("p_bet", "Patrocínio de casa de apostas", "Uma casa de apostas oferece muito dinheiro para estampar o seu nome.",
            s => Pro(s) && s.Fame >= 20 && s.Rep.Character < 4 && !s.Has("bet"),
            O("Aceitar", "Dinheiro fácil, imagem arranhada.", new Fx { Money = 4, Rep = "carater:-2,ambicao:2", Flag = "bet" }),
            O("Recusar", "Você não quer o seu nome nisso.", new Fx { Rep = "carater:2", Flag = "bet" })),
        Ev("p_balada_flagra", "Vídeo da balada vazou", "Um vídeo seu numa festa às 4 da manhã, véspera de jogo, está circulando.",
            s => Pro(s) && (s.Traits.Party >= 4 || s.Rep.Discipline <= -3),
            O("Pedir desculpas ao clube", "Multa e banco no próximo jogo.", new Fx { Money = -0.5, Trust = -3, Rep = "disciplina:1" }),
            O("Dizer que a vida pessoal é sua", "A torcida não gosta nada.", new Fx { Fans = -5, Rep = "polemica:2,disciplina:-1", Gossip = "{nome} é flagrado em festa na véspera de jogo e debocha das críticas" })),
        Ev("p_capitao_moral", "O elenco te escolhe", "Depois de uma crise no vestiário, os jogadores pedem que você fale em nome deles com a diretoria.",
            s => Pro(s) && s.Rep.Character >= 5 && s.Locker >= 55,
            O("Aceitar e cobrar a diretoria", "Os salários atrasados são pagos.", new Fx { Locker = 8, Trust = -2, Rep = "lealdade:1" }),
            O("Mediar com calma", "Todo mundo sai satisfeito.", new Fx { Locker = 5, Trust = 3 })),
        Ev("p_projeto", "Seu projeto social cresceu", "O projeto que você apoia já atende centenas de crianças e quer abrir um núcleo em outra cidade.",
            s => Pro(s) && s.Rep.Charity >= 5 && s.Money >= 3 && !s.Has("instituto"),
            O("Fundar um instituto com o seu nome", "Um legado para além do futebol.", new Fx { Money = -3, Fans = 8, Fame = 3, Flag = "instituto", Gossip = "{nome} inaugura instituto que vai atender mil crianças" }),
            O("Ajudar com uma doação", "O núcleo novo abre no ano seguinte.", new Fx { Money = -1, Fans = 3 })),
        Ev("p_mercenario", "Faixa no estádio", "A torcida estendeu uma faixa: \"Mercenário não veste a nossa camisa\".",
            s => Pro(s) && s.Rep.Loyalty <= -3,
            O("Beijar o escudo no próximo gol", "Metade acredita.", new Fx { Fans = 3 }, 0.5, "Ninguém acredita, e as vaias aumentam.", new Fx { Fans = -4 }),
            O("Responder que é profissional", "As vaias continuam.", new Fx { Fans = -3, Rep = "polemica:1" })),
        Ev("p_treino_extra", "O técnico te usa de exemplo", "Na reunião do elenco, o técnico mostra os seus números de treino como exemplo para todos.",
            s => Pro(s) && s.Rep.Discipline >= 5,
            O("Agradecer e chamar os mais novos para treinar com você", "Você vira referência no elenco.", new Fx { Locker = 5, Trust = 4, Rep = "carater:1" }),
            O("Ficar quieto", "Uns te admiram, outros ficam com ciúmes.", new Fx { Trust = 3, Locker = -2 })),
    };

    public static readonly HashSet<string> StoryIds = StoryEvents.Select(e => e.Id).ToHashSet();
}
