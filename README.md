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

## Estrutura

```
src/
  Camisa10.Domain/         regras do jogo, sem dependência de banco
    Model/                 Career (documento salvo) e tipos internos
    Game/                  GameEngine (fluxo, escolhas, mercado, copas, temporada)
                           Events.Generated.cs (catálogo de ~190 eventos)
                           GameData.cs (clubes, seleções, pesos por posição)
    Views/                 o que a API devolve para o app desenhar cada tela
  Camisa10.Api/            ASP.NET Core + EF Core/MongoDB
    Data/                  Camisa10DbContext
    Endpoints/             rotas HTTP
tests/Camisa10.Tests/      simulação de carreiras + testes de integração
tools/gen-events.js        gera o catálogo de eventos a partir do index.html original
```

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
| GET | `/api/meta` | clubes de base, seleções e posições |
| GET | `/api/careers?status=Active` | carreiras do jogador (`Active` ou `Finished`) |
| POST | `/api/careers` | nova carreira `{ name, club, nation, position }` |
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
