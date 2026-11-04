#nullable enable
using Camisa10.Domain.Model;
namespace Camisa10.Domain.Game;

using static EventDef;

/// <summary>
/// Decisões de carreira (vestiário, técnico, contrato, imprensa) e lances de jogo de cada posição.
/// As opções com chance e atributo são resolvidas no d20: d20 + bônus do atributo contra a dificuldade.
/// </summary>
public static partial class Events
{
    // ---------------------------------------------------------------- decisões de carreira
    public static readonly IReadOnlyList<EventDef> CareerMoves = new List<EventDef>
    {
        Ev("c_banco", "Você vai começar no banco", "O técnico avisa na véspera: o titular amanhã é o reserva que vem treinando melhor.", s => !s.Base && s.Age >= 19,
            O("Treinar dobrado para recuperar a vaga", "No treino seguinte, você voa. A vaga volta.", new Fx { Trust = 5, Fitness = -3, Skill = 1 }, 0.55, "O corpo cansa e o rendimento cai mais.", new Fx { Fitness = -6, Trust = -2 }, "fis"),
            O("Conversar com o técnico", "Ele explica o que espera de você. Clareza ajuda.", new Fx { Trust = 3, Morale = 1 }),
            O("Reclamar com a imprensa", "A manchete sai, mas o técnico não esquece.", new Fx { Fame = 2, Trust = -8, Locker = -3 })),
        Ev("c_improv", "Improvisado em outra posição", "Com o time cheio de desfalques, o técnico quer você jogando fora da sua posição no próximo jogo.", s => !s.Base,
            O("Aceitar e dar o seu melhor", "Você surpreende todo mundo.", new Fx { Trust = 6, Locker = 3 }, 0.5, "Você fica perdido em campo e é substituído no intervalo.", new Fx { Trust = 1, Morale = -4 }, "pas"),
            O("Aceitar, mas pedir para voltar depois", "Combinado. Ele gosta da franqueza.", new Fx { Trust = 3 }),
            O("Recusar", "O técnico escala outro e não te olha no treino.", new Fx { Trust = -7, Morale = -1 })),
        Ev("c_tecnico_novo", "Técnico novo no clube", "A diretoria demite o técnico. O substituto chega dizendo que todo mundo começa do zero.", s => !s.Base,
            O("Mostrar serviço no primeiro treino", "Ele anota o seu nome logo de cara.", new Fx { Trust = 8, Fitness = -2 }, 0.55, "Você exagera, erra tudo e vira piada no treino.", new Fx { Trust = -4, Morale = -3 }, "rit"),
            O("Pedir uma conversa individual", "Ele explica o esquema e onde você encaixa.", new Fx { Trust = 4 }),
            O("Esperar para ver", "Ele escala quem chamou atenção.", new Fx { Trust = -2 })),
        Ev("c_concorrente", "O clube contrata alguém para a sua posição", "O reforço chega com festa no aeroporto e camisa já pronta.", s => !s.Base && s.Age >= 20,
            O("Encarar a disputa no treino", "Você não perde uma dividida. A vaga é sua.", new Fx { Trust = 7, Skill = 1, Fitness = -2 }, 0.5, "Ele vai melhor nos treinos e você perde espaço.", new Fx { Trust = -6, Morale = -5 }, "dri"),
            O("Receber bem o novo colega", "O vestiário nota a sua grandeza.", new Fx { Locker = 6, Morale = 1 }),
            O("Pedir ao empresário para procurar outro clube", "O empresário começa a ligar para os clubes.", new Fx { Agent = 3, Trust = -4, Leave = 1 })),
        Ev("c_renova", "Proposta de renovação", "A diretoria oferece mais três anos de contrato, mas com o mesmo salário de hoje.", s => !s.Base && s.Contract <= 2,
            O("Renovar e mostrar fidelidade", "A torcida canta o seu nome.", new Fx { Contract = 3, Fans = 6, Trust = 3 }),
            O("Pedir aumento na mesa", "A diretoria cede e o salário sobe.", new Fx { Contract = 3, SalaryMult = 1.25, Trust = 1 }, 0.5, "A negociação trava e o clima esfria.", new Fx { Trust = -3, Agent = -2 }, "pas"),
            O("Deixar o contrato correr", "Você fica livre para ouvir propostas.", new Fx { Agent = 4, Fans = -4 })),
        Ev("c_coletiva", "Pergunta polêmica na coletiva", "Um repórter pergunta se você concorda com a escalação do técnico na última derrota.", s => !s.Base && s.Fame >= 8,
            O("Defender o técnico", "O técnico agradece no vestiário.", new Fx { Trust = 5, Fame = 1 }),
            O("Responder com sinceridade", "A frase vira manchete o dia inteiro.", new Fx { Fame = 4, Trust = -5, Fans = 2 }),
            O("Desconversar com uma piada", "A sala inteira ri. Assunto encerrado.", new Fx { Fame = 2, Locker = 2 })),
        Ev("c_vazou", "A conversa com outro clube vazou", "Um site publica que você conversou com um rival. A torcida não gostou.", s => !s.Base && s.Fame >= 10,
            O("Negar tudo publicamente", "A poeira baixa. Por enquanto.", new Fx { Fans = 2, Agent = -2 }, 0.6, "Aparece uma foto do encontro. Pior ainda.", new Fx { Fans = -8, Trust = -3 }),
            O("Admitir e dizer que o foco é aqui", "A sinceridade convence parte da torcida.", new Fx { Fans = -2, Trust = 2 }),
            O("Deixar o empresário cuidar", "Ele aproveita o barulho para valorizar você.", new Fx { Agent = 4, Fans = -4, Rumor = 1 })),
        Ev("c_cobrador", "Quem bate as faltas?", "O cobrador oficial saiu. O técnico vai decidir no treino de bola parada.", s => !s.Base && s.Position is "Meia" or "Ponta" or "Atacante" or "Lateral",
            O("Pedir a responsabilidade e bater no treino", "Três no ângulo. A bola parada é sua.", new Fx { Trust = 5, GoalBonus = 1, Fame = 1 }, 0.45, "Duas na barreira e uma na arquibancada.", new Fx { Trust = -2, Morale = -2 }, "chu"),
            O("Deixar para o veterano", "O vestiário respeita.", new Fx { Locker = 3 })),
        Ev("c_emprest", "Empréstimo para ganhar minutos", "Você quase não joga. O clube sugere um empréstimo para um time menor por uma temporada.", s => !s.Base && s.Age <= 22 && s.Trust < 55,
            O("Aceitar o empréstimo", "O empresário vai atrás de um clube que te dê minutos.", new Fx { Leave = 1, Morale = 2 }),
            O("Ficar e brigar por espaço", "Você treina como nunca.", new Fx { Trust = 4, Fitness = -2 }, 0.5, "Continua no banco e a frustração cresce.", new Fx { Morale = -5 }, "fis"),
            O("Pedir mais chances ao técnico", "Ele promete minutos nos próximos jogos.", new Fx { Trust = 2 })),
        Ev("c_lider", "O técnico quer um líder", "Depois de uma sequência ruim, o técnico pede que você fale com o grupo antes do jogo.", s => !s.Base && s.Age >= 26,
            O("Discursar no vestiário", "O time entra em campo mordido.", new Fx { Locker = 8, Trust = 5, Morale = 3 }, 0.6, "As palavras não saem como você queria.", new Fx { Locker = -2, Morale = -2 }, "pas"),
            O("Liderar pelo exemplo, sem discurso", "Você é o primeiro a chegar e o último a sair.", new Fx { Trust = 4, Fitness = -1 }),
            O("Dizer que não é o seu perfil", "O técnico procura outro.", new Fx { Trust = -2 })),
        Ev("c_protesto", "Protesto da torcida no CT", "Depois de três derrotas, a torcida organizada cerca o ônibus do time.", s => !s.Base,
            O("Descer e conversar com os torcedores", "Eles respeitam a coragem.", new Fx { Fans = 8, Morale = 1 }, 0.6, "A conversa esquenta e vira bate-boca.", new Fx { Fans = -6, Morale = -5 }, "fis"),
            O("Ficar no ônibus com o grupo", "A polícia abre caminho.", new Fx { Locker = 3, Fans = -2 }),
            O("Postar um pedido de calma", "Divide opiniões.", new Fx { Fame = 1 })),
        Ev("c_joia", "\"A nova joia do futebol\"", "A imprensa só fala de você depois de uma grande atuação.", s => !s.Base && s.Age <= 22 && s.Fame >= 8,
            O("Manter os pés no chão", "Os veteranos elogiam a humildade.", new Fx { Locker = 5, Trust = 3 }),
            O("Aproveitar os holofotes", "Seguidores dobram. A cobrança também.", new Fx { Fame = 6, Locker = -3 }),
            O("Contratar um assessor de imprensa", "Entrevistas com hora marcada.", new Fx { Fame = 3, Money = -1, Agent = 2 })),
        Ev("c_selecao_dor", "A seleção chama, mas a coxa dói", "A convocação chegou na mesma semana em que a coxa começou a incomodar.", s => !s.Base && s.Fame >= 20 && s.Age >= 20,
            O("Esconder a dor e se apresentar", "Você joga e brilha.", new Fx { Fame = 5, Fitness = -4 }, 0.5, "A dor vira lesão no primeiro treino.", new Fx { Fitness = -10, Injury = 1, Trust = -2 }, "fis"),
            O("Avisar o departamento médico", "Você fica fora desta, mas inteiro.", new Fx { Fitness = 4, Fame = -1 }),
            O("Fazer tratamento intensivo e ir", "Você chega no limite, mas chega.", new Fx { Fitness = -2, Fame = 3 })),
        Ev("c_veterano_mentor", "O garoto pede a sua vaga", "Um jovem da base vem muito bem e o técnico pensa em dar a ele a vaga de titular.", s => !s.Base && s.Age >= 30,
            O("Brigar pela vaga em campo", "A experiência fala mais alto.", new Fx { Trust = 5, Fitness = -3 }, 0.45, "O garoto é mais rápido e fica com a vaga.", new Fx { Trust = -4, Morale = -4 }, "rit"),
            O("Ajudar o garoto a crescer", "O vestiário te vê como um capitão sem braçadeira.", new Fx { Locker = 8, Trust = 2 }),
            O("Pedir para ser usado nos jogos grandes", "O técnico topa.", new Fx { Trust = 1, Fitness = 2 })),
        Ev("c_bonus", "Contrato por produtividade", "O clube propõe um salário fixo menor e um bônus a cada gol ou assistência.", s => !s.Base && s.Age >= 20 && !s.Has("bonus"),
            O("Aceitar a aposta", "Agora cada gol vale mais no fim do mês.", new Fx { SalaryMult = 0.85, GoalBonus = 2, Flag = "bonus" }),
            O("Manter o salário de hoje", "Segurança acima de tudo.", new Fx { Flag = "bonus" })),
    };

    // ---------------------------------------------------------------- lances de jogo por posição
    // "pa_" = ataque, "pd_" = defesa, "pc_" = meio-campo (o foco do campinho sai do prefixo)
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<EventDef>> MatchByPosition = new Dictionary<string, IReadOnlyList<EventDef>>
    {
        ["Atacante"] = new List<EventDef>
        {
            Ev("pa_1x1", "Mano a mano com o zagueiro", "A bola chega no seu pé e só o zagueiro está entre você e o goleiro.", s => true,
                O("Partir para o drible", "Você deixa o zagueiro no chão e bate cruzado. Gol!", new Fx { Goals = 1, Fame = 5, Morale = 5 }, 0.4, "O zagueiro antecipa e sai jogando.", new Fx { Morale = -3 }, "dri"),
                O("Bater antes que ele chegue", "O chute rasteiro surpreende o goleiro. Gol!", new Fx { Goals = 1, Fame = 3, Morale = 4 }, 0.38, "Bloqueado na hora.", new Fx { Morale = -2 }, "chu"),
                O("Proteger e esperar o meia", "Você segura e toca para o meia, que marca.", new Fx { Assists = 1, Trust = 3 }, 0.45, "Desarmado pelas costas.", new Fx { Trust = -2 }, "fis")),
            Ev("pa_cab", "Cruzamento na pequena área", "O ponta vai à linha de fundo e levanta na cabeça.", s => true,
                O("Subir mais que o zagueiro", "Testada firme no canto. Gol!", new Fx { Goals = 1, Fame = 4, Morale = 4 }, 0.4, "O zagueiro chega junto e afasta.", new Fx { Morale = -2 }, "fis"),
                O("Antecipar no primeiro pau", "Desvio de leve e a bola morre na rede.", new Fx { Goals = 1, Fame = 4, Morale = 4 }, 0.35, "Você chega atrasado.", new Fx { Morale = -2 }, "rit"),
                O("Ajeitar de cabeça para trás", "O volante chega batendo. Gol, com a sua assistência!", new Fx { Assists = 1, Trust = 3 }, 0.45, "A bola sobra para a defesa.", new Fx { }, "pas")),
            Ev("pa_contra", "Contra-ataque em velocidade", "Lançamento longo e você sai em disparada com o zagueiro na cola.", s => true,
                O("Correr até o fim e tocar na saída do goleiro", "Na corrida, ninguém te pega. Gol!", new Fx { Goals = 1, Fame = 5, Morale = 5 }, 0.42, "O zagueiro chega no carrinho.", new Fx { Morale = -2 }, "rit"),
                O("Esperar o companheiro e rolar", "Dois contra um e ele só empurra.", new Fx { Assists = 1, Trust = 3 }, 0.5, "O passe sai forte demais.", new Fx { Trust = -2 }, "pas")),
        },
        ["Ponta"] = new List<EventDef>
        {
            Ev("pa_ponta_linha", "Na linha de fundo", "Você ganha do lateral na corrida e chega à linha de fundo.", s => true,
                O("Cruzar rasteiro para trás", "O atacante só empurra. Assistência sua!", new Fx { Assists = 1, Trust = 3, Morale = 3 }, 0.45, "A zaga corta antes.", new Fx { }, "pas"),
                O("Cortar para dentro e bater", "Chute colocado no ângulo. Golaço!", new Fx { Goals = 1, Fame = 6, Morale = 5 }, 0.32, "Por cima do gol.", new Fx { Morale = -2 }, "chu"),
                O("Driblar mais um e entrar na área", "Você passa por dois e toca na saída do goleiro.", new Fx { Goals = 1, Fame = 5, Morale = 5 }, 0.3, "O segundo marcador chega e toma a bola.", new Fx { Morale = -3 }, "dri")),
            Ev("pa_ponta_1x1", "Um contra um com o lateral", "Você recebe aberto, de frente para o lateral.", s => true,
                O("Partir em velocidade pela linha", "Você passa como um raio e cruza na cabeça do atacante. Gol!", new Fx { Assists = 1, Trust = 3 }, 0.45, "O lateral acompanha e põe para escanteio.", new Fx { }, "rit"),
                O("Pedalada e drible para dentro", "O lateral cai sentado. Você chuta e marca!", new Fx { Goals = 1, Fame = 5, Morale = 5 }, 0.33, "O lateral não cai na finta.", new Fx { Morale = -2 }, "dri"),
                O("Tabelar com o meia", "A tabela sai perfeita e você bate cruzado. Gol!", new Fx { Goals = 1, Fame = 3, Morale = 4 }, 0.38, "O meia erra o retorno.", new Fx { }, "pas")),
            Ev("pd_ponta_volta", "O lateral deles sobe", "O lateral adversário avança e você precisa decidir se volta para marcar.", s => true,
                O("Voltar e fechar o corredor", "Você desarma e sai em velocidade.", new Fx { Trust = 5, Fitness = -2 }, 0.5, "Você chega tarde e o cruzamento sai.", new Fx { Trust = -2 }, "rit"),
                O("Ficar na frente esperando o contra-ataque", "Arriscado, mas se roubarem a bola você está livre.", new Fx { Fame = 1 }, 0.4, "Eles cruzam por ali e quase marcam.", new Fx { Trust = -4 }, "def")),
        },
        ["Meia"] = new List<EventDef>
        {
            Ev("pc_meia_lanc", "O passe que decide", "Você recebe no meio, de frente para o jogo, e vê o atacante se mexendo.", s => true,
                O("Enfiar a bola entre os zagueiros", "Passe milimétrico. O atacante sai cara a cara e marca!", new Fx { Assists = 1, Trust = 4, Fame = 2 }, 0.45, "A bola corre demais e o goleiro sai.", new Fx { }, "pas"),
                O("Inverter para o ponta", "Mudança de lado que abre a defesa.", new Fx { Trust = 3 }, 0.6, "O lançamento sai pela lateral.", new Fx { Trust = -1 }, "pas"),
                O("Chutar de fora da área", "Bomba no ângulo! Golaço!", new Fx { Goals = 1, Fame = 6, Morale = 5 }, 0.25, "Na arquibancada.", new Fx { Morale = -2 }, "chu")),
            Ev("pa_meia_falta", "Falta na entrada da área", "A barreira se arma. A bola é sua.", s => true,
                O("Bater por cima da barreira", "No ângulo, sem chance para o goleiro. Gol!", new Fx { Goals = 1, Fame = 6, Morale = 6 }, 0.3, "Na barreira.", new Fx { Morale = -1 }, "chu"),
                O("Rolar para o companheiro chegar batendo", "Jogada ensaiada. Gol, com a sua assistência!", new Fx { Assists = 1, Trust = 4 }, 0.38, "O chute dele sai fraco.", new Fx { }, "pas"),
                O("Cruzar na área", "O zagueiro sobe e cabeceia para a rede.", new Fx { Assists = 1, Trust = 3 }, 0.35, "O goleiro sai e segura.", new Fx { }, "pas")),
            Ev("pd_meia_press", "Pressão na saída de bola", "O time adversário tenta sair jogando e a bola passa perto de você.", s => true,
                O("Dar o bote no volante", "Você rouba a bola na entrada da área deles.", new Fx { Trust = 4, Fitness = -1 }, 0.5, "Ele gira e escapa.", new Fx { Trust = -2 }, "fis"),
                O("Fechar a linha de passe", "O passe não sai e eles chutam para frente.", new Fx { Trust = 3 }, 0.6, "Eles acham o espaço.", new Fx { }, "def")),
        },
        ["Volante"] = new List<EventDef>
        {
            Ev("pd_vol_roubo", "O meia deles vem conduzindo", "O camisa 10 adversário avança em direção à área.", s => true,
                O("Dar o carrinho", "Bola limpa! A torcida vibra.", new Fx { Trust = 5, Fans = 2 }, 0.45, "Falta e cartão amarelo.", new Fx { Trust = -3, Morale = -2 }, "def"),
                O("Acompanhar e esperar o erro", "Ele erra o passe e você recupera.", new Fx { Trust = 4 }, 0.55, "Ele acha o atacante livre.", new Fx { Trust = -3 }, "rit"),
                O("Fazer a falta tática", "Cartão amarelo, mas o contra-ataque morre ali.", new Fx { Trust = 2, Locker = 2 })),
            Ev("pc_vol_lanc", "Lançamento da intermediária", "Você recupera a bola e vê o atacante disparando nas costas da zaga.", s => true,
                O("Lançar de primeira", "Lançamento de 40 metros na medida. Gol do atacante!", new Fx { Assists = 1, Trust = 4, Fame = 2 }, 0.35, "Forte demais, direto para o goleiro.", new Fx { }, "pas"),
                O("Tocar curto e girar o jogo", "O time respira e controla a partida.", new Fx { Trust = 3 }),
                O("Arriscar de longe", "A bola explode na trave e entra! Golaço!", new Fx { Goals = 1, Fame = 6, Morale = 5 }, 0.2, "Na arquibancada.", new Fx { Morale = -2 }, "chu")),
        },
        ["Lateral"] = new List<EventDef>
        {
            Ev("pd_lat_ponta", "O ponta deles vem para cima", "O ponta mais rápido do campeonato parte em velocidade no seu lado.", s => true,
                O("Acompanhar na corrida", "Você chega junto e põe para escanteio.", new Fx { Trust = 4 }, 0.5, "Ele te passa na velocidade e cruza.", new Fx { Trust = -3 }, "rit"),
                O("Dar o bote", "Desarme limpo e saída rápida!", new Fx { Trust = 5, Fans = 2 }, 0.42, "Ele espera o bote e passa.", new Fx { Trust = -4 }, "def"),
                O("Fechar por dentro e oferecer a linha", "Ele vai para a linha e o cruzamento sai fraco.", new Fx { Trust = 3 }, 0.55, "O cruzamento sai perfeito.", new Fx { }, "def")),
            Ev("pa_lat_apoio", "Apoio pela lateral", "O corredor está livre. Você sobe em velocidade.", s => true,
                O("Cruzar da linha de fundo", "Na cabeça do atacante. Gol!", new Fx { Assists = 1, Trust = 3 }, 0.42, "A zaga afasta.", new Fx { }, "pas"),
                O("Cortar para dentro e chutar", "Chute cruzado no canto. Gol de lateral!", new Fx { Goals = 1, Fame = 5, Morale = 5 }, 0.25, "Pra fora.", new Fx { }, "chu"),
                O("Tabelar com o ponta", "Vocês trocam e o ponta marca.", new Fx { Assists = 1, Trust = 3 }, 0.4, "A tabela não sai.", new Fx { }, "pas")),
        },
        ["Zagueiro"] = new List<EventDef>
        {
            Ev("pd_zag_corrida", "Atacante deles na corrida", "Lançamento nas suas costas e o centroavante sai em velocidade.", s => true,
                O("Correr e chegar no carrinho", "Carrinho perfeito. Bola para fora!", new Fx { Trust = 5, Fans = 3 }, 0.42, "Você chega atrasado: falta perigosa.", new Fx { Trust = -3 }, "rit"),
                O("Usar o corpo e proteger o gol", "Ombro com ombro, ele perde o tempo da bola.", new Fx { Trust = 4 }, 0.5, "Ele aguenta a trombada e chuta.", new Fx { Trust = -3 }, "fis"),
                O("Fazer a linha de impedimento", "Bandeira levantada! Impedimento.", new Fx { Trust = 4 }, 0.45, "Ele estava em posição legal.", new Fx { Trust = -5 }, "def")),
            Ev("pd_zag_aerea", "Bola aérea na sua área", "Escanteio contra. O centroavante deles tem quase dois metros.", s => true,
                O("Disputar no alto", "Você sobe mais e afasta de cabeça.", new Fx { Trust = 4 }, 0.5, "Ele ganha no alto e cabeceia.", new Fx { Trust = -3 }, "fis"),
                O("Marcar no corpo e não deixar ele subir", "Ele nem consegue saltar.", new Fx { Trust = 3 }, 0.55, "Pênalti! Você puxou a camisa.", new Fx { Trust = -5, Morale = -3 }, "def")),
            Ev("pa_zag_escanteio", "Escanteio a favor", "Você sobe para a área adversária no escanteio.", s => true,
                O("Atacar a primeira trave", "Cabeçada no canto. Gol de zagueiro!", new Fx { Goals = 1, Fame = 4, Morale = 5 }, 0.3, "Para fora, raspando.", new Fx { }, "fis"),
                O("Escorar para o meio da área", "O atacante completa. Assistência sua!", new Fx { Assists = 1, Trust = 3 }, 0.35, "A zaga afasta.", new Fx { }, "pas")),
        },
    };

    /// <summary>Ids das decisões de carreira (entram no sorteio de carreira).</summary>
    public static readonly HashSet<string> CareerMoveIds = CareerMoves.Select(e => e.Id).ToHashSet();
}
