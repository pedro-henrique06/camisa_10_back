#nullable enable
using Camisa10.Domain.Model;
namespace Camisa10.Domain.Game;

using static EventDef;

/// <summary>
/// Vida fora de campo por fase da carreira: base (até 18), começo (19 a 24), auge (25 a 30) e veterano (31+).
/// Marcos da vida (carteira, casa própria, casamento, filho, curso de treinador...) acontecem uma vez só
/// (marcados com Flag) e alguns têm continuação (FollowUp).
/// </summary>
public static partial class Events
{
    private static bool Teen(Career s) => s.Age <= 18;
    private static bool Young(Career s) => s.Age >= 19 && s.Age <= 24;
    private static bool Prime(Career s) => s.Age >= 25 && s.Age <= 30;
    private static bool Veteran(Career s) => s.Age >= 31;

    public static readonly IReadOnlyList<EventDef> Life = new List<EventDef>
    {
        // ---------------------------------------------------------------- base (15 a 18 anos)
        Ev("v_prova", "Semana de provas na escola", "O treino coincide com a prova de matemática, e a professora não quer saber de futebol.", s => Teen(s),
            O("Estudar de madrugada", "Passa raspando, mas passa.", new Fx { Fitness = -3, Family = 4, Morale = 1 }),
            O("Pedir para fazer a prova outro dia", "A escola deixa, desde que você traga a carta do clube.", new Fx { Trust = 1, Family = 1 }, 0.6, "A diretora nega, e você fica de recuperação.", new Fx { Family = -5, Morale = -3 }),
            O("Faltar à prova pelo treino", "O técnico gosta. Sua mãe, nem um pouco.", new Fx { Trust = 3, Family = -6 })),
        Ev("v_aloja", "Saudade no alojamento", "Primeira noite longe de casa. O beliche range e o celular não para de vibrar com mensagem da família.", s => Teen(s) && s.Base && !s.Has("aloja"),
            O("Ligar para casa e chorar um pouco", "A voz da sua mãe acalma. Amanhã é outro dia.", new Fx { Morale = 3, Family = 5, Flag = "aloja" }),
            O("Fazer amizade com o colega de quarto", "Vocês viram a noite conversando sobre os sonhos de cada um.", new Fx { Friends = 6, Locker = 4, Fitness = -1, Flag = "aloja" }),
            O("Pensar em desistir", "A noite é longa, mas você fica.", new Fx { Morale = -6, Flag = "aloja" })),
        Ev("v_chut1", "A primeira chuteira boa", "A sua está com a sola descolando. A nova, da vitrine, custa o salário do seu pai.", s => Teen(s) && !s.Has("chut1"),
            O("Juntar dinheiro fazendo bico", "Três fins de semana lavando carro. A chuteira tem outro gosto.", new Fx { Morale = 5, Fitness = -2, Flag = "chut1" }),
            O("Aceitar a ajuda do tio", "Ele faz questão. Você promete retribuir.", new Fx { Family = 4, Morale = 3, Flag = "chut1" }),
            O("Colar a sola de novo", "Aguenta mais um mês. Talvez.", new Fx { Morale = -2 })),
        Ev("v_pelada", "Pelada no bairro", "Os amigos de infância chamam para o racha de sábado, valendo um refrigerante.", s => Teen(s),
            O("Ir e jogar de verdade", "Você joga solto e lembra por que ama futebol.", new Fx { Morale = 6, Friends = 6, Fitness = -2 }, 0.85, "Uma entrada mais dura e o tornozelo reclama.", new Fx { Fitness = -8, Trust = -2 }),
            O("Ir só para assistir", "Resenha garantida, sem risco.", new Fx { Friends = 4, Morale = 2 }),
            O("Recusar: o clube proíbe", "Os amigos acham que você mudou.", new Fx { Friends = -5, Trust = 1 })),
        Ev("v_namoro1", "Primeiro namoro", "A menina da escola assiste ao jogo da base e espera você na saída.", s => Teen(s) && s.Age >= 16 && !s.Has("namoro1") && s.Love.Stage == "solteiro",
            O("Chamar para tomar um sorvete", "Você fica vermelho, mas dá certo.", new Fx { Morale = 7, Flag = "namoro1" }),
            O("Fingir que não viu", "Vergonha falou mais alto.", new Fx { Morale = -2, Flag = "namoro1" })),
        Ev("v_onibus", "Duas horas de ônibus", "Do bairro ao CT são três conduções. O cansaço aparece no treino.", s => Teen(s) && s.Base,
            O("Estudar no caminho", "As horas no ônibus viram horas de estudo.", new Fx { Family = 3, Fitness = -1 }),
            O("Dormir no ônibus", "Você chega mais descansado ao treino.", new Fx { Fitness = 2 }),
            O("Pedir carona ao colega que tem moto", "Mais rápido. E mais perigoso.", new Fx { Fitness = 1, Family = -3 }, 0.8, "Um tombo leve no asfalto molhado.", new Fx { Fitness = -6, Family = -4 })),
        Ev("v_game", "Campeonato de videogame", "O grupo do alojamento marca um torneio de madrugada. O prêmio é um ano de zoação.", s => Teen(s),
            O("Jogar até o fim", "Campeão! Mas o treino da manhã foi sofrido.", new Fx { Locker = 6, Friends = 3, Fitness = -4 }),
            O("Jogar uma rodada e dormir", "Equilíbrio é tudo.", new Fx { Locker = 3 }),
            O("Dormir cedo", "Os colegas te chamam de chato.", new Fx { Fitness = 2, Locker = -2 })),
        Ev("v_cnh", "Dezoito anos: carteira de motorista", "Todo mundo pergunta quando você vai tirar a carteira.", s => s.Age == 18 && !s.Has("cnh"),
            O("Fazer a autoescola agora", "Aprovado de primeira na baliza.", new Fx { Money = -1, Morale = 4, Flag = "cnh" }, 0.7, "Reprovado na prova prática. Fica para o mês que vem.", new Fx { Money = -1, Morale = -3, Flag = "cnh" }),
            O("Deixar para depois", "Ônibus e carona continuam valendo.", new Fx { Flag = "cnh" })),
        Ev("v_pais_sac", "O sacrifício dos pais", "Você descobre que seu pai vendeu o carro para pagar as viagens da base.", s => Teen(s) && !s.Has("sacr"),
            O("Prometer que vai dar certo", "Ele te abraça forte. A promessa pesa e motiva.", new Fx { Morale = 5, Family = 8, Flag = "sacr" }),
            O("Pensar em arrumar um emprego", "Seu pai não deixa: o seu trabalho é o futebol.", new Fx { Family = 4, Morale = -2, Flag = "sacr" })),

        // ---------------------------------------------------------------- começo da carreira (19 a 24)
        Ev("v_sozinho", "Morar sozinho", "Com o primeiro contrato, dá para alugar um apartamento perto do CT.", s => Young(s) && !s.Base && !s.Has("sozinho"),
            O("Alugar um apê só seu", "Liberdade, louça acumulada e muito silêncio.", new Fx { Morale = 4, Money = -2, Family = -2, Flag = "sozinho", FollowUp = "v_sozinho2", FollowUpIn = 20 }),
            O("Dividir com um colega do time", "Mais barato e mais divertido.", new Fx { Locker = 5, Friends = 3, Money = -1, Flag = "sozinho" }),
            O("Continuar com a família", "Comida de mãe não tem preço.", new Fx { Family = 6, Fitness = 1, Flag = "sozinho" })),
        Ev("v_sozinho2", "A geladeira vazia", "Morar sozinho cobra o preço: pizza todo dia e roupa sem lavar.", s => false,
            O("Contratar uma nutricionista", "O cardápio entra nos trilhos.", new Fx { Money = -1, Fitness = 5 }),
            O("Aprender a cozinhar", "Arroz queimado no começo, mas você pega o jeito.", new Fx { Morale = 3, Fitness = 2 }),
            O("Seguir no delivery", "Prático, mas o corpo sente.", new Fx { Fitness = -5 })),
        Ev("v_carro1", "O primeiro carro", "O primeiro salário de verdade caiu. A concessionária liga oferecendo um carro esportivo.", s => Young(s) && !s.Base && s.Money >= 3 && !s.Has("carro1"),
            O("Comprar o esportivo", "Barulho, olhares e uma parcela enorme.", new Fx { Money = -4, Fame = 2, Morale = 4, Flag = "carro1" }),
            O("Um carro simples e econômico", "Seu pai aprova. A conta também.", new Fx { Money = -1, Family = 3, Flag = "carro1" }),
            O("Guardar o dinheiro", "O empresário elogia a cabeça no lugar.", new Fx { Agent = 3, Flag = "carro1" })),
        Ev("v_salario1", "O que fazer com o primeiro salário", "É mais do que seus pais ganham em um ano.", s => Young(s) && !s.Base && !s.Has("salario1"),
            O("Quitar a casa dos pais", "Sua mãe chora no telefone.", new Fx { Money = -3, Family = 12, Morale = 5, Flag = "salario1" }),
            O("Investir com um assessor", "Rende pouco, mas rende.", new Fx { Money = 1, Agent = 2, Flag = "salario1" }),
            O("Uma viagem com os amigos", "Fotos incríveis e a conta zerada.", new Fx { Money = -2, Friends = 8, Morale = 4, Flag = "salario1" })),
        Ev("v_tatu", "Tatuagem nova", "Você quer homenagear a família com uma tatuagem no braço.", s => Young(s),
            O("Fazer a tatuagem", "Ficou linda. A família adora.", new Fx { Morale = 4, Family = 3, Fitness = -1 }),
            O("Fazer uma bem grande, nas costas", "Doeu mais do que uma entrada de carrinho.", new Fx { Morale = 3, Fame = 1, Fitness = -3 }),
            O("Deixar para as férias", "Melhor não arriscar uma infecção na temporada.", new Fx { Trust = 1 })),
        Ev("v_marca", "Marca de roupa quer te patrocinar", "Uma grife jovem quer você na campanha. Pagam bem, mas exigem posts toda semana.", s => Young(s) && s.Fame >= 12,
            O("Fechar o contrato", "Dinheiro e fama, com a agenda mais cheia.", new Fx { Money = 3, Fame = 4, Fitness = -2 }),
            O("Negociar menos posts", "Paga menos, mas cabe na rotina.", new Fx { Money = 1, Fame = 2, Agent = 2 }),
            O("Recusar: o foco é jogar", "O técnico nota o compromisso.", new Fx { Trust = 3 })),
        Ev("v_festa_fam", "Festa de um cantor famoso", "Você recebe convite para o aniversário de um sertanejo famoso, na véspera de folga.", s => Young(s) && s.Fame >= 15,
            O("Ir e tirar foto com todo mundo", "As fotos viralizam.", new Fx { Fame = 4, Morale = 4, Fitness = -3 }),
            O("Passar rápido e ir embora", "Aparece e some. Ninguém reclama.", new Fx { Fame = 2, Morale = 2 }),
            O("Ficar em casa", "Série, pipoca e cama cedo.", new Fx { Fitness = 2 })),
        Ev("v_cripto", "A moeda que vai multiplicar", "Um colega jura que uma criptomoeda nova vai dez vezes em um mês.", s => Young(s) && s.Money >= 3,
            O("Colocar uma parte", "Ela dispara! Você vende na hora certa.", new Fx { Money = 3, Morale = 3 }, 0.35, "Ela some do mapa em uma semana.", new Fx { Money = -3, Morale = -4 }),
            O("Perguntar ao seu assessor", "Ele te desaconselha. Na semana seguinte, a moeda despenca.", new Fx { Agent = 3, Morale = 1 }),
            O("Não entrar", "O colega fica chateado, mas você dorme tranquilo.", new Fx { })),
        Ev("v_cidade", "Saudade da sua cidade", "Longe de casa, você sente falta da comida, do sotaque e dos amigos de infância.", s => Young(s) && !s.Base,
            O("Trazer a família para morar perto", "A casa fica cheia e o coração, tranquilo.", new Fx { Family = 8, Money = -2, Morale = 5 }),
            O("Visitar no próximo feriado", "Dois dias de colo de mãe.", new Fx { Family = 5, Fitness = -1, Morale = 3 }),
            O("Fazer amizades na cidade nova", "Os colegas te apresentam a cidade.", new Fx { Locker = 4, Friends = 3 })),
        Ev("v_reality", "Convite para reality show", "Uma emissora quer você no programa das férias. Seriam três semanas confinado.", s => Young(s) && s.Fame >= 20 && !s.Has("reality"),
            O("Aceitar", "Você vira assunto do país inteiro.", new Fx { Fame = 8, Money = 2, Trust = -4, Fitness = -4, Flag = "reality" }),
            O("Recusar", "O clube agradece a seriedade.", new Fx { Trust = 3, Flag = "reality" })),

        // ---------------------------------------------------------------- auge (25 a 30)
        Ev("v_pedido", "Hora de pedir em casamento?", "Vocês estão juntos há anos e a família já pergunta quando sai o casamento.", s => false, // substituído pela vida amorosa (r_pedido)
            O("Pedir no gramado do estádio", "A torcida aplaude. A resposta é sim!", new Fx { Morale = 9, Fame = 3, Family = 6, Flag = "casou", FollowUp = "v_casamento", FollowUpIn = 12 }),
            O("Pedir num jantar a dois", "Simples e perfeito. Sim!", new Fx { Morale = 8, Family = 6, Flag = "casou", FollowUp = "v_casamento", FollowUpIn = 12 }),
            O("Esperar o fim da carreira", "Ela entende, mas fica pensativa.", new Fx { Morale = -2 })),
        Ev("v_casamento", "O grande dia", "A festa de casamento cai na semana do jogo mais importante do mês.", s => false,
            O("Festa grande com o elenco todo", "Inesquecível. O treino seguinte, nem tanto.", new Fx { Morale = 10, Locker = 6, Fitness = -4, Money = -3, FollowUp = "v_bebe", FollowUpIn = 36 }),
            O("Cerimônia pequena, só a família", "Íntimo e emocionante.", new Fx { Morale = 8, Family = 8, Money = -1, FollowUp = "v_bebe", FollowUpIn = 36 })),
        Ev("v_bebe", "Vem aí um bebê!", "O teste deu positivo. Você vai ser pai.", s => false,
            O("Contar para a torcida no próximo gol", "A comemoração com a mão na barriga vira a foto do ano.", new Fx { Morale = 10, Fame = 4, Fans = 5, FollowUp = "v_nasceu", FollowUpIn = 18 }),
            O("Guardar a notícia só para a família", "Um segredo bom de guardar.", new Fx { Morale = 9, Family = 8, FollowUp = "v_nasceu", FollowUpIn = 18 })),
        Ev("v_nasceu", "O bebê nasceu", "Seu filho nasceu na madrugada antes de um jogo.", s => false,
            O("Ficar no hospital e não jogar", "O clube libera. Nada é mais importante.", new Fx { Family = 12, Morale = 8, Trust = -2, Flag = "pai", FollowUp = "v_filho_estadio", FollowUpIn = 60 }),
            O("Ver o bebê e correr para o estádio", "Você joga com a cabeça no hospital.", new Fx { Family = 4, Morale = 5, Trust = 3, Fitness = -3, Flag = "pai", FollowUp = "v_filho_estadio", FollowUpIn = 60 })),
        Ev("v_noites", "Noites sem dormir", "O bebê chora a noite toda e o treino é às oito.", s => Prime(s) && s.Has("pai"),
            O("Revezar com a esposa", "Cansado, mas presente.", new Fx { Family = 5, Fitness = -3 }),
            O("Dormir no quarto de hóspedes antes dos jogos", "O corpo agradece. A esposa, nem tanto.", new Fx { Fitness = 2, Family = -4 }),
            O("Contratar uma babá noturna", "Todo mundo dorme melhor.", new Fx { Money = -2, Fitness = 1, Family = 1 })),
        Ev("v_negocio", "Abrir um negócio", "Um amigo propõe abrir uma hamburgueria com o seu nome.", s => Prime(s) && s.Money >= 6 && !s.Has("negocio"),
            O("Abrir a hamburgueria com ele (R$ 2 mi)", "Fila na porta na inauguração.", new Fx { Biz = "abrir:hamburgueria", Fame = 2, Friends = 3, Flag = "negocio" }),
            O("Abrir uma escolinha de futebol (R$ 3 mi)", "Menos lucro, mais orgulho.", new Fx { Biz = "abrir:escolinha", Fans = 5, Morale = 4, Flag = "negocio" }),
            O("Não misturar amizade e dinheiro", "O amigo entende.", new Fx { })),
        Ev("v_instituto", "Um instituto no seu bairro", "A quadra onde você começou está abandonada. Dá para reformar e criar um projeto social.", s => Prime(s) && s.Fame >= 25 && !s.Has("instituto"),
            O("Criar o instituto", "Centenas de crianças com chuteira nova.", new Fx { Money = -4, Fans = 8, Fame = 4, Morale = 6, Flag = "instituto" }),
            O("Doar uma vez e seguir", "Ajuda, sem compromisso de longo prazo.", new Fx { Money = -1, Fans = 3 }),
            O("Deixar para quando parar de jogar", "Fica a vontade de fazer.", new Fx { })),
        Ev("v_contador", "O contador encontra um problema", "Faltou declarar parte dos rendimentos do ano passado.", s => Prime(s) && !s.Base,
            O("Pagar tudo na hora, com multa", "Dói, mas fica tudo certo.", new Fx { Money = -3, Agent = 2 }),
            O("Trocar de contador", "O novo é mais caro e mais cuidadoso.", new Fx { Money = -1, Agent = 1 }),
            O("Deixar para depois", "O problema cresce na gaveta.", new Fx { Money = -6, Morale = -4 }, 0.5, "Por sorte, era um erro do próprio contador.", new Fx { })),
        Ev("v_comercial", "Comercial de TV", "Uma marca de cerveja quer você no comercial do verão.", s => Prime(s) && s.Fame >= 30,
            O("Gravar o comercial", "Você vira meme nacional, no bom sentido.", new Fx { Money = 3, Fame = 5, Fitness = -1 }),
            O("Pedir para trocar por um refrigerante", "A marca topa. Sua imagem agradece.", new Fx { Money = 2, Fame = 3, Fans = 2 }),
            O("Recusar", "Você não quer associar o nome à bebida.", new Fx { Fans = 2, Trust = 1 })),
        Ev("v_ferias_ilha", "Férias numa ilha", "Duas semanas de folga. A esposa quer praia, o preparador quer treino leve.", s => Prime(s) && !s.Base && s.Love.Stage == "casado",
            O("Praia e descanso total", "Você volta renovado, com dois quilos a mais.", new Fx { Morale = 8, Family = 5, Fitness = -3 }),
            O("Praia com personal trainer", "O equilíbrio perfeito.", new Fx { Morale = 5, Fitness = 3, Money = -2 }),
            O("Ficar treinando no CT", "O técnico adora. A família, não.", new Fx { Trust = 3, Family = -6 })),
        Ev("v_irmao_jog", "Seu irmão quer ser jogador", "O caçula da família também joga bola e pede ajuda para conseguir um teste.", s => Prime(s) && !s.Has("irmao_jog"),
            O("Indicar ao seu clube", "Ele passa no teste por mérito próprio.", new Fx { Family = 8, Morale = 4, Flag = "irmao_jog" }, 0.5, "Ele não passa, e o clima em casa fica pesado.", new Fx { Family = -3, Morale = -3, Flag = "irmao_jog" }),
            O("Pagar uma escolinha boa para ele", "Ele vai crescer no tempo dele.", new Fx { Money = -1, Family = 5, Flag = "irmao_jog" }),
            O("Dizer a verdade: estude primeiro", "Ele fica magoado, mas sua mãe concorda.", new Fx { Family = 2, Morale = -1, Flag = "irmao_jog" })),

        // ---------------------------------------------------------------- veterano (31+)
        Ev("v_apos", "Pensando na aposentadoria", "O corpo demora mais para se recuperar e a imprensa já pergunta sobre o fim.", s => Veteran(s) && !s.Has("apos_pensou"),
            O("Planejar os próximos anos com calma", "Um plano tira o peso das costas.", new Fx { Morale = 4, Agent = 2, Flag = "apos_pensou" }),
            O("Dizer que vai jogar até os 40", "A torcida adora. O corpo, nem tanto.", new Fx { Fans = 4, Fitness = -2, Flag = "apos_pensou" }),
            O("Evitar o assunto", "A dúvida continua lá.", new Fx { Morale = -3, Flag = "apos_pensou" })),
        Ev("v_curso_tec", "Curso de treinador", "A federação abre turma para a licença de técnico, com aulas à noite.", s => Veteran(s) && !s.Has("curso_tec"),
            O("Fazer o curso", "Você passa a ver o jogo com outros olhos.", new Fx { Skill = 1, Trust = 4, Fitness = -2, Flag = "curso_tec" }),
            O("Deixar para quando parar", "Primeiro as chuteiras.", new Fx { Flag = "curso_tec" })),
        Ev("v_tv", "Convite para comentar na TV", "Uma emissora quer você como comentarista convidado em um jogo da Copa.", s => Veteran(s) && s.Fame >= 30,
            O("Aceitar", "Você leva jeito, e o país inteiro comenta.", new Fx { Fame = 4, Money = 1, Fitness = -1 }),
            O("Recusar por enquanto", "Ainda tem muito futebol nas pernas.", new Fx { Trust = 1 })),
        Ev("v_garoto", "O garoto da base", "Um menino de 16 anos te lembra você mesmo e pede conselhos depois do treino.", s => Veteran(s) && !s.Base,
            O("Virar mentor dele", "Ele cresce rápido, e você também aprende.", new Fx { Locker = 6, Morale = 5, Trust = 2 }),
            O("Dar uma chuteira e um abraço", "Ele nunca vai esquecer.", new Fx { Locker = 3, Fans = 2 }),
            O("Dizer que agora não dá", "Ele fica sem graça.", new Fx { Locker = -3 })),
        Ev("v_joelho", "O joelho reclama", "A lesão antiga volta a doer nos dias frios.", s => Veteran(s),
            O("Fortalecimento todo dia com o fisioterapeuta", "Disciplina que prolonga a carreira.", new Fx { Fitness = 4, Morale = -1 }),
            O("Infiltração para aguentar o jogo", "Resolve hoje. Amanhã, quem sabe.", new Fx { Fitness = 2, Wear = 3 }),
            O("Pedir para descansar uma partida", "O técnico entende, mas o titular é outro.", new Fx { Fitness = 6, Trust = -3 })),
        Ev("v_homenagem", "Homenagem no clube que te revelou", "O clube da sua base quer dar o seu nome a um campo do CT.", s => Veteran(s) && !s.Has("homenagem"),
            O("Ir à cerimônia com a família", "Você chora vendo as fotos de quando tinha 14 anos.", new Fx { Morale = 9, Family = 6, Fans = 4, Flag = "homenagem" }),
            O("Mandar um vídeo de agradecimento", "Simples, mas emociona.", new Fx { Morale = 4, Fans = 2, Flag = "homenagem" })),
        Ev("v_biografia", "Uma biografia sobre você", "Uma editora quer contar a sua história, da pelada no bairro aos estádios.", s => Veteran(s) && s.Fame >= 25 && !s.Has("biografia"),
            O("Contar tudo, inclusive os erros", "O livro vira best-seller e emociona muita gente.", new Fx { Fame = 6, Fans = 5, Money = 2, Locker = -2, Flag = "biografia" }),
            O("Uma versão leve, só com as glórias", "Vende bem, sem polêmica.", new Fx { Fame = 3, Money = 2, Flag = "biografia" }),
            O("Não é a hora", "A história ainda não terminou.", new Fx { Flag = "biografia" })),
        Ev("v_pais_saude", "A saúde dos seus pais", "Seu pai fez exames e precisa de uma cirurgia simples, longe de onde você joga.", s => Veteran(s) && !s.Has("pais_saude"),
            O("Pedir dispensa ao clube e ir ficar com ele", "A cirurgia dá certo. Ele segura sua mão o tempo todo.", new Fx { Family = 10, Morale = 5, Trust = -2, Flag = "pais_saude" }),
            O("Pagar o melhor hospital e acompanhar por vídeo", "Tudo dá certo, mas você queria estar lá.", new Fx { Money = -3, Family = 4, Morale = -1, Flag = "pais_saude" })),
        Ev("v_amigo_para", "Seu amigo de geração se aposenta", "O parceiro de tantos anos anuncia a aposentadoria e te convida para o jogo de despedida.", s => Veteran(s) && !s.Has("despedida_amigo"),
            O("Jogar a despedida dele", "Gols, abraços e saudade antecipada.", new Fx { Morale = 6, Friends = 6, Fitness = -2, Flag = "despedida_amigo" }),
            O("Mandar uma mensagem emocionada", "Ele responde com uma foto de vocês dois na base.", new Fx { Friends = 3, Morale = 2, Flag = "despedida_amigo" })),
        Ev("v_filho_estadio", "Seu filho no estádio", "Pela primeira vez, seu filho vai te ver jogar. Ele está com a sua camisa.", s => false,
            O("Entrar em campo com ele no colo", "A foto vai para a parede da sala.", new Fx { Morale = 10, Family = 8, Fans = 4 }),
            O("Fazer um gol para ele", "Você aponta para a arquibancada, mesmo sem saber se ele viu.", new Fx { Morale = 8, Family = 5 })),

        // ---------------------------------------------------------------- a vida com o cachorro (uma vez)
        Ev("v_cao2", "O cachorro aprontou", "Você chega do treino e o sofá está destruído. Ele te olha abanando o rabo.", s => false,
            O("Contratar um adestrador", "Em um mês, ele vira um lorde.", new Fx { Money = -1, Morale = 3 }),
            O("Levar ele para correr todo dia", "Os dois ficam em forma.", new Fx { Fitness = 3, Morale = 4 }),
            O("Rir e comprar um sofá novo", "Coisa de família.", new Fx { Money = -1, Morale = 5 })),
    };

    /// <summary>Ids dos eventos de vida (entram no sorteio pessoal).</summary>
    public static readonly HashSet<string> LifeIds = Life.Select(e => e.Id).ToHashSet();

    private static IReadOnlyList<EventDef>? _every;

    /// <summary>Catálogo completo: os eventos originais e os de vida por idade.</summary>
    public static IReadOnlyList<EventDef> Every => _every ??= All.Concat(Life).Concat(CareerMoves).Concat(Romance).Concat(BusinessEvents).Concat(StoryEvents).ToList();
}
