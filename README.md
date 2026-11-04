# camisa_10_back

Backend do **Camisa 10 – Simulador de Carreira**. Aqui fica toda a lógica do jogo: o app React Native só desenha
a tela que a API devolve e envia as escolhas do jogador.

- **.NET 10** (ASP.NET Core, minimal APIs)
- **MongoDB** com **EF Core** (provedor oficial `MongoDB.EntityFrameworkCore`)
- Testes com xUnit (simulação de carreiras e integração com Mongo)

## Como rodar

### Com Docker (API + Mongo)

    docker compose up --build

A API sobe em `http://localhost:5080` e o MongoDB em `localhost:27017`.

### Local

Precisa do .NET 10 SDK e de um MongoDB rodando (por exemplo `docker run -d -p 27017:27017 mongo:8`).

    dotnet run --project src/Camisa10.Api --urls http://0.0.0.0:5080

Configuração (`appsettings.json` ou variáveis de ambiente):

| Chave | Variável de ambiente | Padrão |
| --- | --- | --- |
| `ConnectionStrings:Mongo` | `ConnectionStrings__Mongo` | `mongodb://localhost:27017` |
| `Mongo:Database` | `Mongo__Database` | `camisa10` |

O documento OpenAPI fica em `/openapi/v1.json`.

## Publicar no Railway

O repositório já tem um `railway.json` (build pelo `Dockerfile` e healthcheck em `/health`), e a API escuta
na porta da variável `PORT`, que o Railway define.

1. No projeto do Railway, crie o banco: **+ Create → Database → MongoDB**.
2. **+ Create → GitHub Repo →** `camisa_10_back`.
3. Em **Variables** do backend:

   | Variável | Valor |
   | --- | --- |
   | `ConnectionStrings__Mongo` | `${{MongoDB.MONGO_URL}}` (o nome entre chaves é o do serviço do banco) |
   | `Mongo__Database` | `camisa10` |

4. Em **Settings → Networking**, gere o domínio.

`/health` mostra se o banco conectou (`"mongo": "conectado"`). Se o Mongo não responder, a API continua no
ar, explica o erro nos **Deploy Logs** e tenta de novo sozinha.

## Estrutura

```
src/
  Camisa10.Domain/         regras do jogo, sem dependência de banco
    Model/                 Career (documento salvo) e tipos internos
    Game/                  GameEngine (fluxo, escolhas, mercado, copas, temporada)
                           Events.Generated.cs (catálogo de ~190 eventos)
                           GameData.cs (clubes, seleções, pesos por posição)
    World/                 os 211 países da FIFA, ligas A/B/C e acesso e rebaixamento
    Data/world.json        dados do mundo (gerado por tools/world/build_world.py)
    Views/                 o que a API devolve para o app desenhar cada tela
  Camisa10.Api/            ASP.NET Core + EF Core/MongoDB
    Data/                  Camisa10DbContext
    Endpoints/             rotas HTTP
tests/Camisa10.Tests/      simulação de carreiras + testes de integração
tools/gen-events.js        gera o catálogo de eventos a partir do index.html original
tools/world/               dados dos países e clubes e o gerador do world.json
```

## Ligas do mundo

O jogo tem os **211 países da FIFA**, cada um com **três divisões (A, B e C)**: cerca de 2.400 clubes reais e
5.200 fictícios. Os clubes reais vêm das listas em `tools/world/data_*.py`, conferidas com o
[openfootball](https://github.com/openfootball/football.json) (domínio público) onde ele tem a liga.
Nas divisões sem dados conhecidos, sobretudo em países pequenos, os clubes são fictícios com nomes de
cidades reais (marcados com `"f": 1` no `world.json`). Alguns clubes reais podem estar na divisão errada
por acesso ou rebaixamento recente; para corrigir, edite a lista e regere:

    python3 tools/world/build_world.py tools/world/openfootball-2025.json

- **Força do clube**: qualidade da liga do país × peso da divisão × tamanho do clube (a ordem nas listas,
  do maior para o menor, no país inteiro). Ela vira o nível de 0 a 4 usado em salários, mercado, eventos e copas.
- **Acesso e rebaixamento em todas as ligas**: ao fim de cada temporada, os 3 primeiros de cada divisão sobem
  e os 3 últimos caem (2 em ligas de 10 a 15 clubes, 1 em ligas menores). A simulação é determinística: a
  carreira guarda só a semente (`WorldSeed`) e a posição final do clube do jogador em cada temporada
  (`ClubSeasons`), e as divisões de qualquer temporada são recalculadas a partir disso.
- **Posição do clube do jogador**: parte da força do clube e melhora ou piora com o desempenho do jogador.
  Campeão é quem termina em 1º. O balanço da temporada mostra a tabela com as zonas de acesso e rebaixamento.
- **Propostas**: de clubes do mundo inteiro com o nível parecido, com mais chance para o próprio país, a mesma
  confederação, ligas fortes e clubes reais.
- **Temporada em datas do ano** (`SeasonCalendar.cs`, `GameEngine.Competitions.cs`): 24 datas por temporada,
  no máximo um jogo por data (12 da liga, ou 7 da liga e 5 do estadual; 5 da copa nacional; 7 da continental).
  As datas seguem os calendários oficiais de 2025/26 e 2026:

  | Calendário | Países | Período | Como se espalha |
  |---|---|---|---|
  | `ano` | América do Sul | janeiro a dezembro | CBF 2026: estaduais 11/jan–8/mar, Série A 28/jan–2/dez, Copa do Brasil até a final de 6/dez; Libertadores com grupos em abr–mai, oitavas em ago, quartas em set, semi em out e final em 28/nov |
  | `europeu` | Europa (exceto os abaixo), África, Oriente Médio e o resto da Ásia | agosto a maio | Champions 2025/26: fase de liga set–jan, oitavas mar, quartas abr, semi abr–mai, final 30/mai; copa nacional de out à final de maio |
  | `inverno` | Rússia, Ucrânia e Japão (J.League 2026/27) | julho a maio | pausa de inverno de dezembro ao fim de fevereiro |
  | `nordico` | Noruega, Suécia, Finlândia, Islândia, Irlanda, bálticos, Bielorrússia, Cazaquistão | março a dezembro | Eliteserien 2026: 14/mar–6/dez; nada em janeiro e fevereiro |
  | `asia` | Coreia do Sul e China | fevereiro a dezembro | K League 2026: 28/fev–6/dez |
  | `oceania` | Austrália e OFC | julho a maio | Australia Cup jul–out, A-League de outubro à grande final em maio |
  | `concacaf` | CONCACAF | julho a maio | Liga MX (Apertura e Clausura) e MLS a partir de 2027/28; Champions Cup de fevereiro à final de 30/mai |

  Estadual (Paulistão, Carioca, Mineiro, Gauchão…) só no Brasil; sem ele, essas datas são rodadas da liga.
  Copa nacional (Copa do Brasil, FA Cup, Copa do Rei…): 5 fases, da terceira fase à final. Continental
  (Libertadores, Liga dos Campeões…): 3 jogos de grupo e mata-mata das oitavas à final. Nos calendários
  nórdico, asiático e da Oceania, a copa continental (que na vida real atravessa duas temporadas) cabe
  dentro da temporada do clube.
- **Tabela ao vivo** (`GameEngine.League.cs`): a cada data da liga todos os clubes jogam uma rodada
  (método do círculo, turno e returno). O jogo do jogador usa o placar do jogo ao vivo; os outros são
  simulados pela força dos clubes. Posição final, título, acesso e rebaixamento saem dessa tabela, e a
  classificação inteira é gravada para o mundo da temporada seguinte.
- **Grupos e chaves das copas** (`GameEngine.Cups.cs`): estadual e continental com grupo de 4 (todos contra
  todos; os 2 primeiros passam) e mata-mata; copa nacional só mata-mata. A chave é inteira: o adversário de
  cada jogo do jogador sai dela, os outros jogos são simulados pela força dos times, e quem é eliminado vê a
  chave seguir até o campeão. Os torneios da seleção usam o mesmo formato (grupo e oitavas a final).
- **Estatísticas dos jogos**: jogos, gols, assistências e nota do balanço são a soma das partidas do
  jogador (parte dos gols do time é dele, pela posição; a nota de cada jogo vem dos gols, passes,
  decisões e do resultado).
- **Jogo ao vivo** (`GameEngine.Match.cs`): cada jogo tem adversário da competição, mando, placar e narração
  minuto a minuto. Entre as decisões do jogador, o jogo é simulado pela força dos times; a decisão muda o
  placar (gol, assistência, defesa, pênalti) e, na copa, define o resultado. O app mostra o campo com os
  jogadores correndo atrás da bola e a barra de narração.
  Os torneios da seleção (Copa do Mundo, Copa América, Eurocopa…) também têm o campo: o adversário é uma
  seleção da confederação (ou uma forte, na Copa do Mundo), com as cores das camisas e a narração com os
  artigos certos ("da França", "de Portugal", "dos EUA"; ver `PtNames.cs`).
  A vaga continental sai da posição na liga anterior (no Brasil: 6 na Libertadores e 6 na Sul-Americana);
  recém-promovidos ficam de fora. Eliminado, o resto da copa some do calendário. Lesionado, o time joga sem
  você. Seleções: as 211, com Copa do Mundo a cada 4 temporadas e o torneio continental de cada confederação
  nas outras, depois do dia 20.

Carreiras criadas antes das ligas do mundo ganham semente e clube do mundo (pelo nome) na primeira ação.

## Como o jogo funciona no servidor

Cada carreira é um documento na coleção `careers`. Ele guarda todo o estado (atributos, barras, clube,
fila de eventos do dia, copa em andamento etc.) e a **fase** atual, que diz qual tela o app deve mostrar:

| Fase | Tela | Ações válidas |
| --- | --- | --- |
| `Calendar` | calendário antes do dia | `Continue` |
| `Event` | evento com opções | `Choose` (índice da opção) |
| `Penalty` | cobrança de pênalti | `Choose` (0 a 5, canto do gol) com `power` (0 a 100, força da barra) |
| `Result` | resultado da escolha | `Continue` |
| `Market` | janela de transferências | `Choose` (proposta) ou `Stay` |
| `Swap` | treinador sugere mudar de posição | `Choose` (0 aceita, 1 recusa) |
| `Promotion` | promovido ao profissional | `Continue` |
| `SeasonSummary` | balanço da temporada | `Continue` |
| `Decision` | aos 34+ anos, continuar ou parar | `Continue` ou `Retire` |
| `Finished` | carreira encerrada | — |

Com a janela aberta, `SearchClub` abre o mercado a partir do calendário, de um evento ou de um resultado.

Toda ação leva a `version` da tela que o app estava mostrando. Se não bater (toque duplo, duas abas),
a API responde `409` com a tela atual e não aplica nada. A versão também é token de concorrência do EF.

### Pênalti

O jogador escolhe o canto (0 a 2 em cima, 3 a 5 embaixo) e para a barra de força.
Entre 60 e 85 é a força ideal; fraca demais facilita a defesa e forte demais pode isolar a bola,
principalmente nos cantos altos. O resultado traz `penalty` com o desfecho (`goal`, `save`, `post`
ou `out`) e o canto para onde o goleiro pulou, para o app animar a cobrança.

## API

Todas as rotas de carreira exigem o cabeçalho `X-Player-Id` (8 a 64 caracteres; o app gera um na primeira vez).

| Método | Rota | Descrição |
| --- | --- | --- |
| GET | `/api/meta` | clubes em destaque, as 211 seleções e as posições |
| GET | `/api/world` | os 211 países com as suas três divisões |
| GET | `/api/world/{iso}` | clubes das divisões A, B e C de um país (ex.: `BR`, `GB-ENG`) |
| GET | `/api/world/search?q=` | busca clubes pelo nome |
| GET | `/api/careers?status=Active` | carreiras do jogador (`Active` ou `Finished`) |
| POST | `/api/careers` | nova carreira `{ name, club, nation, position }` (`club` é o id do clube no mundo) |
| GET | `/api/careers/{id}` | tela atual |
| POST | `/api/careers/{id}/actions` | `{ type, index?, power?, version }` e devolve a nova tela |
| GET | `/api/careers/{id}/history` | evolução temporada a temporada |
| DELETE | `/api/careers/{id}` | apaga a carreira |

Exemplo:

    curl -X POST localhost:5080/api/careers -H 'X-Player-Id: meu-celular-123' \
      -H 'Content-Type: application/json' \
      -d '{"name":"Pedro","club":"Flamengo","nation":"Brasil","position":"Meia"}'

## Testes

    dotnet test

Os testes de integração com MongoDB só rodam com a variável `CAMISA10_TEST_MONGO`:

    CAMISA10_TEST_MONGO=mongodb://localhost:27017 dotnet test

Cada teste cria um banco temporário e o apaga no final.

## Catálogo de eventos

Os textos e números dos eventos vieram do `index.html` original do repositório `camisa-10`.
Para regerar depois de editar o HTML:

    node tools/gen-events.js ../camisa-10/legacy/index.html
