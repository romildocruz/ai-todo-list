## Context

Projeto novo (greenfield) de estudo, executado 100% localmente no Windows, sem Docker. Motivação e escopo em `proposal.md`; requisitos em `specs/`. Ambiente detectado: .NET SDK 10.0.402 (também 8.0.426), Node v26.7.0, OpenSpec 1.8.0; `mongod` **não** está no PATH (instalação/serviço a verificar).

## Goals / Non-Goals

**Goals:**
- Arquitetura simples, didática e testável: vertical slices + CQRS explícito.
- Regras de negócio em um só lugar (domínio), validação no servidor como fonte da verdade.
- Passo a passo reproduzível de execução e testes locais.

**Non-Goals:**
- MediatR/mensageria, bancos separados de leitura/escrita, Event Sourcing, repositório genérico.
- Paginação, drag and drop, autenticação, Docker, nuvem (evoluções futuras).

## Architecture

### Visão geral

```mermaid
flowchart LR
  UI[React + Mantine<br/>Kanban]
  subgraph Api[AiTodo.Api]
    EP[Endpoints FastEndpoints<br/>Validators · Program.cs]
  end
  subgraph App[AiTodo.Application]
    BUS[Command Bus<br/>FastEndpoints]
    CH[Command handlers]
    QH[Query handlers]
    RP[ITodoReadRepository]
  end
  subgraph Dom[AiTodo.Domain]
    AGG[Todo aggregate · TodoId<br/>ITodoRepository]
  end
  subgraph Inf[AiTodo.Infrastructure]
    REPO[TodoRepository<br/>TodoReadRepository<br/>BsonClassMap · índices]
  end
  DB[(MongoDB local<br/>AiTodoDb.Todos)]
  UI -- HTTP/JSON --> EP
  EP -- ExecuteAsync --> BUS
  BUS --> CH
  BUS --> QH
  CH --> AGG
  QH --> RP
  REPO -. implementa .-> AGG
  REPO -. implementa .-> RP
  REPO --> DB
  EP -. composição DI .-> REPO
```

### Fluxo de Commands (escrita)

```mermaid
sequenceDiagram
  participant C as Cliente
  participant E as Endpoint (FastEndpoints)
  participant V as Validator
  participant H as CommandHandler
  participant T as Todo (domínio)
  participant M as MongoDB
  C->>E: POST/PUT/PATCH/DELETE
  E->>V: valida request (formato do id, tamanhos, enum)
  V-->>E: erros? -> 400 ProblemDetails
  E->>H: command.ExecuteAsync() (command bus)
  H->>M: carrega Todo (update/status/delete)
  M-->>H: Todo ou nulo (-> 404)
  H->>T: aplica regra (Rename/ChangeStatus), atualiza UpdatedAt se mudou
  H->>M: persiste (insert/replace/delete)
  H-->>E: Result<TodoDto>
  E-->>C: 201/200/204
```

### Fluxo de Queries (leitura)

```mermaid
sequenceDiagram
  participant C as Cliente
  participant E as Endpoint
  participant H as QueryHandler
  participant M as MongoDB
  C->>E: GET /api/todos?status&search | GET /api/todos/{id}
  E->>E: valida id/status (sem tocar no banco se inválido)
  E->>H: query.ExecuteAsync() (command bus)
  H->>M: Find(filtro, sort createdAt desc)
  M-->>H: documentos
  H-->>E: lista/DTO (ou NotFound)
  E-->>C: 200 / 404
```

### Modelo de dados

```mermaid
erDiagram
  TODOS {
    ObjectId _id PK
    string title "1..100, trim"
    string description "0..500, opcional"
    string status "ToDo | Doing | Done"
    date createdAt "UTC"
    date updatedAt "UTC"
  }
```

Índices: `{ createdAt: -1 }` e `{ status: 1, createdAt: -1 }`. Sem índice de texto: a busca por título usa regex literal escapado, case-insensitive (adequado à escala didática).

### Estrutura proposta (referência para a implementação)

```
ai-todo-list/
  Directory.Build.props  Directory.Packages.props   (versões centralizadas, warnings como erro)
  src/
    AiTodo.Domain/          Todo (aggregate), TodoId, TodoStatus, TodoRules, DomainException,
                            ITodoRepository          — ZERO pacotes
    AiTodo.Application/     Features/Todos/{Commands,Queries}/<Operação>/ (XCommand|XQuery + XHandler)
                            TodoResponse, ITodoReadRepository, Result, DataStoreUnavailableException
                            — pacote: FastEndpoints.Messaging.Core (só abstrações)
    AiTodo.Infrastructure/  Mongo/ (MongoOptions, TodoBsonMapping, TodoRepository, TodoReadRepository,
                            MongoIndexInitializer, MongoCall) · DependencyInjection.AddInfrastructure()
    AiTodo.Api/             Endpoints/Todos/<Operação>/ (XEndpoint, XRequest, XValidator)
                            Common/ (Problem Details, exceções) · Program.cs
  tests/
    AiTodo.Domain.UnitTests/   AiTodo.Application.UnitTests/   AiTodo.Api.IntegrationTests/
    AiTodo.Tests.Common/       (dublês compartilhados: FakeClock, InMemoryTodoRepository)
  web/                      (Vite + React + TS)
  docs/  openspec/
```

## Decisions

1. **CQRS com o Command Bus nativo do FastEndpoints (sem MediatR).** Commands e Queries implementam `ICommand<TResult>` e são tratados por `ICommandHandler<TCommand,TResult>` (pacote `FastEndpoints.Messaging.Core`, só abstrações, na Application); o endpoint monta o command/query e chama `ExecuteAsync()`. Queries ficam em `Queries/` e leem direto do Mongo pela interface `ITodoReadRepository`, devolvendo DTOs sem reconstruir o agregado. *Por quê:* determinado pelo `CLAUDE.md` do projeto; remove o dispatcher próprio (menos código) e evita MediatR. *Alternativas:* handlers/dispatcher próprios (adotados nas etapas 4-6 e substituídos nesta refatoração); MediatR (descartado).
2. **N-Layers / Clean Architecture com organização por feature dentro de cada camada.** `Domain ← Application ← Api` e `Infrastructure → Application/Domain` (a Api referencia a Infrastructure só para a composição no DI). Cada operação tem sua pasta em `Application/Features/Todos/{Commands|Queries}/<Operação>` e em `Api/Endpoints/Todos/<Operação>`. As regras de dependência são verificadas por testes (`DependencyRulesTests`). *Alternativa anterior:* vertical slices num único projeto (substituída por exigência do `CLAUDE.md`).
3. **Quatro projetos de produção e três de testes** (`Domain.UnitTests`, `Application.UnitTests`, `Api.IntegrationTests`), mais `Tests.Common` com os dublês compartilhados (não é projeto de teste). Pacotes com versão centralizada em `Directory.Packages.props`.
4. **DDD no `Todo`.** Aggregate root com construtor privado, fábrica estática `Todo.Create(TodoId, ...)`, setters privados e `Update`/`ChangeStatus` que retornam se houve mudança e atualizam `UpdatedAt` apenas nesse caso. `TodoId` é um value object tipado (24 hex, normalizado em minúsculas); a geração do id é do repositório (`ITodoRepository.NextId()`), mantendo o Domain sem tipos do driver. Normalização e limites ficam em `TodoRules`, compartilhados com os validators. Violação de invariante lança `DomainException`.
5. **Acesso a dados na Infrastructure com `MongoDB.Driver`.** `TodoRepository` (escrita, agregado) e `TodoReadRepository` (leitura, `TodoReadModel` → `TodoResponse`), sem repositório genérico nem unit of work. Mapeamento por `BsonClassMap` (nunca atributos): `TodoId` ↔ ObjectId via serializer próprio, status como string, datas UTC. `IMongoClient` singleton. Falhas de indisponibilidade do driver são traduzidas em `DataStoreUnavailableException` (`MongoCall`), para que Api e Application não conheçam o Mongo. `UpdatedAt` vem de `TimeProvider` (testável).
6. **Validação com FluentValidation via FastEndpoints (`Validator<T>`).** O formato do `id` (ObjectId de 24 hex) é validado antes de chegar ao handler → 400. Enum `status` inválido → 400 (binding estrito de enum como string).
7. **Erros: exceções de domínio + Result pattern, expostos como Problem Details (RFC 9457).** `DomainException` para invariante violada (rede de segurança atrás dos validators → 400); `Result` (Success/NotFound) para o desfecho esperado "não encontrado"; `DataStoreUnavailableException` → 503; demais exceções → 500 genérico, tudo via tratador global. Validação → 400 (`errors` por campo), não encontrado → 404.
8. **Swagger:** `FastEndpoints.Swagger` (NSwag) em Development, com `Summary`/`Produces` por endpoint.
9. **CORS por configuração:** `Cors:AllowedOrigins` (padrão `http://localhost:5173`).
10. **Configuração:** `appsettings.json` (valores seguros/sem segredos) + `appsettings.Development.json.example`, variáveis de ambiente (`Mongo__ConnectionString`) e `dotnet user-secrets` quando houver credenciais. `MongoOptions` validado na subida (`ValidateOnStart`). Timeouts curtos (`ServerSelectionTimeout` ~3 s em dev). Criação de índices ao subir, tolerante a banco offline (log + nova tentativa preguiçosa) para que a API suba e responda 503 nas rotas de dados.
11. **Agrupamento por status no cliente.** A API lista em ordem `createdAt desc`; o front agrupa nas 3 colunas. *Por quê:* um contrato simples, sem endpoint extra; sem paginação, o volume é pequeno.
12. **Sem paginação / sem drag and drop (decidido com o usuário).** `GET /api/todos` retorna array simples; contrato deixa espaço para futura evolução (`?page`, `?pageSize`) sem quebrar rotas. DnD (ex.: dnd-kit) registrado como evolução; mudança de status via menu/botões no cartão.
13. **Mutação sem otimismo no front.** O cartão só muda de coluna após a resposta da API (simples e consistente com a spec); recarga/invalidação do cache após cada mutação.
14. **Front-end:** Vite + React + TS estrito; **Mantine** (componentes, modais, `@mantine/notifications`, `@mantine/form`); **TanStack Query** para estado de servidor, loading/erro e invalidação (alternativa: `useEffect`+estado, descartada por duplicar lógica); **React Router** só se houver mais de uma página (inicialmente uma única rota `/`, adiado); cliente HTTP com `fetch` tipado em `web/src/api`. `VITE_API_BASE_URL` em `.env.example`. Textos de interface em pt-BR centralizados em um módulo de mensagens; identificadores em inglês.
15. **Testes back-end:** xUnit. `Domain.UnitTests` (agregado, `TodoId`, regras); `Application.UnitTests` (handlers com `InMemoryTodoRepository` e `FakeClock`); `Api.IntegrationTests` (pipeline HTTP real via `WebApplicationFactory` com repositório em memória, validators, 500/503, Swagger, CORS, regras de dependência e integração com MongoDB real). A integração usa banco `AiTodoDb_Tests_<guid>` criado/descartado por execução, numa instância **local** informada em `MongoTests__ConnectionString`; sem a variável os testes são ignorados com mensagem explícita e nunca usam o banco de dev. **Sem Testcontainers/Docker** (restrição do projeto). **Testes front-end:** Vitest + RTL + `msw` (ou mock de `fetch`) para a API.
16. **Versões:** na etapa 1 da implementação, validar via `dotnet package search`/NuGet e `npm view` as versões estáveis compatíveis com `net10.0` (FastEndpoints, FastEndpoints.Swagger, FastEndpoints.Messaging.Core, MongoDB.Driver, xUnit) e com o Node instalado (Vite, React, Mantine, TanStack Query, Vitest, RTL). Não fixar versões antes dessa verificação; registrá-las no README e em `Directory.Packages.props`. Usar `net10.0` como TFM.

## MongoDB local no Windows (guia que será escrito no README)

- Verificar: `Get-Service MongoDB` (serviço padrão do instalador MSI), `Get-Command mongod, mongosh`, `Test-NetConnection localhost -Port 27017`.
- Iniciar/parar: `Start-Service MongoDB` / `Stop-Service MongoDB` (PowerShell como administrador).
- Se ausente: instalar MongoDB Community Server (MSI) com "Install as a Service"; opcional MongoDB Compass e `mongosh`. Confirmar com `mongosh "mongodb://localhost:27017" --eval "db.runCommand({ping:1})"`.
- Banco/coleção são criados automaticamente na primeira escrita; índices criados pela API.
- Com autenticação: `mongodb://usuario:senha@localhost:27017/?authSource=admin` via user-secrets/variável de ambiente.

## Execução local (resumo dos comandos)

```powershell
# API
dotnet restore
dotnet run --project src/AiTodo.Api          # Swagger em /swagger
dotnet test                                  # integração com Mongo: defina MongoTests__ConnectionString
# Front-end
cd web; npm install; copy .env.example .env; npm run dev; npm test
```

## Rastreabilidade

| Spec | Decisões | Tarefas (tasks.md) |
|---|---|---|
| todo-management | 2, 4, 6, 11 | 3.x, 4.x, 6.x |
| todo-api | 1, 2, 7, 8, 9, 12 | 4.x, 5.x, 6.x |
| todo-persistence | 5, 10, 15 | 2.x, 3.x, 6.x |
| kanban-ui | 12, 13, 14 | 7.x, 8.x, 9.x |
| quality-and-local-setup | 10, 15, 16 | 1.x, 6.x, 9.x, 10.x |

## Risks / Trade-offs

- [Compatibilidade de pacotes com .NET 10] → validar versões na etapa 1 antes de adicionar dependências.
- [MongoDB não instalado/sem serviço] → guia de verificação/instalação; API sobe sem banco e responde 503.
- [Regex para busca não usa índice] → aceitável no volume didático; índice de texto/Atlas Search como evolução.
- [Sem paginação] → listas grandes degradam; contrato permite adicionar paginação sem quebrar clientes.
- [Concorrência (last-write-wins)] → sem versionamento nesta versão; evolução possível com campo `version`.
- [Testes de integração dependem de MongoDB local] → banco dedicado por execução, skip explícito quando não configurado.
- [Mais projetos e indireção do que um CRUD exigiria] → adotado de propósito (objetivo didático, `CLAUDE.md`); dependências verificadas por `DependencyRulesTests` e a lógica concentrada no agregado.
- [Command Bus do FastEndpoints acopla os handlers ao framework] → apenas as abstrações (`FastEndpoints.Messaging.Core`) entram na Application.

## Open Questions

- Texto exato das mensagens pt-BR da API (erros de validação): padrão em pt-BR no servidor ou apenas no front? (Assumido: mensagens de API em pt-BR, definidas nos validators.)
- Nome final do namespace/solução (`AiTodo.*` assumido).
