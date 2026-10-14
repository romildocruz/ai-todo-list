# Arquitetura

.NET 10 · N-Layers · Clean Architecture · DDD · CQRS
Web: FastEndpoints · Banco: MongoDB (MongoDB.Driver)

## Projetos (src/)
- `AiTodo.Domain`
  Aggregates, Entities, Value Objects, Domain Events, interfaces de repositório
  (`IPedidoRepository`), exceções e regras de negócio.
  ZERO dependências de pacotes (nada de MongoDB.Driver, nem atributos BSON).
- `AiTodo.Application`
  Commands, Queries e seus Handlers, DTOs/Responses, interfaces de serviços,
  Event Handlers. Depende só de Domain.
  Pacotes: FastEndpoints.Messaging.Core (apenas as abstrações ICommand/ICommandHandler/IEvent).
- `AiTodo.Infrastructure`
  Implementação dos repositórios, `MongoDbContext`/`IMongoDatabase`, mapeamentos BSON
  (via `BsonClassMap`, não atributos), índices, serviços externos.
  Depende de Application e Domain.
- `AiTodo.Api`
  Endpoints FastEndpoints, Validators, Processors, DI, Program.cs.
  Referencia Application e Infrastructure (Infrastructure só para composição no DI).
- `tests/`: `AiTodo.Domain.UnitTests`, `AiTodo.Application.UnitTests`, `AiTodo.Api.IntegrationTests`
  (MongoDB local dedicado a testes, via `MongoTests__ConnectionString`; **sem Testcontainers/Docker**,
  restrição do projeto). `AiTodo.Tests.Common` guarda só os dublês compartilhados (FakeClock, repositório em memória).

## Regras de dependência
Api → Application → Domain ; Infrastructure → Application/Domain.
Domain não conhece ninguém. Application não conhece Infrastructure nem Mongo.

## CQRS com FastEndpoints
- NÃO usar MediatR. Usar o Command Bus nativo: `ICommand<T>` / `ICommandHandler<TCommand,T>`.
- Command = altera estado, retorna no máximo Id ou Result. Query = só leitura, nunca altera estado.
- Queries também usam o Command Bus (`ICommand<TResponse>`), nomeadas `XxxQuery`
  e ficam em pasta `Queries/`. Podem ler direto do Mongo via interface de leitura
  (`IPedidoReadRepository`) retornando DTOs, sem passar pelo Aggregate.
- Um endpoint por arquivo, classe fina: valida, monta o Command/Query, chama
  `ExecuteAsync()` e mapeia a resposta. Sem regra de negócio no endpoint.
- Validators: FluentValidation (`Validator<TRequest>`) na camada Api para requests HTTP;
  validações de negócio ficam no Domain.
- Domain Events: publicados com `IEvent`/`PublishAsync` e tratados em `IEventHandler<T>`
  na Application.
- Preocupações transversais (log, auditoria): Pre/Post Processors do FastEndpoints.

## Organização por feature
Application/Features/Pedidos/Commands/CriarPedido/{CriarPedidoCommand,CriarPedidoHandler}.cs
Application/Features/Pedidos/Queries/ObterPedido/{ObterPedidoQuery,ObterPedidoHandler}.cs
Api/Endpoints/Pedidos/CriarPedido/{CriarPedidoEndpoint,CriarPedidoRequest,CriarPedidoValidator}.cs

## DDD
- Repositório apenas por Aggregate Root. Aggregates com construtor privado,
  factory method estático e setters privados; invariantes protegidas.
- Value Objects imutáveis (record). Ids tipados quando fizer sentido.
- Regras de negócio dentro do Aggregate, nunca nos handlers.

## MongoDB
- Mapeamento em Infrastructure com `BsonClassMap` (Domain permanece limpo).
- Um repositório por Aggregate; coleção por Aggregate. Índices criados na Infrastructure.
- Transações só quando necessário (exigem replica set); preferir design de
  Aggregate que garanta consistência em um único documento.
- Sem Unit of Work estilo EF; commit é a própria operação do repositório.
- Handlers de leitura podem usar projeções (`Projection`) para evitar carregar o documento inteiro.

## Convenções
- Nomes de código em inglês; mensagens ao usuário (API e UI) em português do Brasil.
  `CancellationToken` em toda chamada async.
- Nullable habilitado, `TreatWarningsAsErrors`, `Directory.Build.props` e
  `Directory.Packages.props` (versões centralizadas).
- Erros: exceções de domínio (`DomainException`) para invariantes violadas; `Result` (Success/NotFound)
  para desfechos esperados, como "não encontrado". Falha de infraestrutura chega como
  `DataStoreUnavailableException` (neutra; a Infrastructure traduz o driver). A Api converte tudo em
  Problem Details: 400 (validação/domínio), 404, 503 (armazenamento indisponível) e 500 genérico.
- Após cada mudança: `dotnet build` e `dotnet test`.