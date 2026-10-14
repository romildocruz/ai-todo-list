## 1. Estrutura da solução .NET 10

- [x] 1.1 Verificar pré-requisitos (`dotnet --list-sdks` com 10.x, `node --version`) e validar versões estáveis dos pacotes compatíveis com `net10.0` (FastEndpoints 8.3.0, FastEndpoints.Swagger 8.3.0, MongoDB.Driver 3.12.0, xUnit 2.9.3); versões registradas nos `.csproj`
- [x] 1.2 Criar `AiTodo.sln` e a estrutura em camadas definida no `CLAUDE.md` (refatorada após a etapa 6): `src/AiTodo.Domain`, `src/AiTodo.Application`, `src/AiTodo.Infrastructure`, `src/AiTodo.Api` (`net10.0`) e os projetos de teste `tests/AiTodo.Domain.UnitTests`, `tests/AiTodo.Application.UnitTests`, `tests/AiTodo.Api.IntegrationTests` (+ `tests/AiTodo.Tests.Common` com os dublês compartilhados); versões de pacote centralizadas em `Directory.Packages.props`
- [x] 1.3 Adicionar `.gitignore`, `.editorconfig` e Nullable/ImplicitUsings habilitados; `dotnet build` sem avisos
- [x] 1.4 Configurar `Program.cs` mínimo com FastEndpoints, Swagger (Development), CORS configurável, middleware global de exceções com Problem Details e endpoint de saúde simples

## 2. Configuração e conexão com MongoDB local

- [x] 2.1 Criar `MongoOptions` (ConnectionString, DatabaseName=`AiTodoDb`, CollectionName=`Todos`, timeouts) com validação na subida e mensagem clara se ausente
- [x] 2.2 Criar `appsettings.json` sem segredos e `appsettings.Development.json.example`; suportar variáveis de ambiente e user-secrets; ignorar arquivos locais no `.gitignore`
- [x] 2.3 Registrar `IMongoClient` (singleton) e `IMongoDatabase` no DI com timeouts curtos (a `IMongoCollection<Todo>` tipada é registrada na tarefa 3.2, quando a entidade existir)
- [x] 2.4 Criar inicializador idempotente de índices (`createdAt -1`; `status 1, createdAt -1`) tolerante a MongoDB offline
- [x] 2.5 Escrever o guia de MongoDB local no Windows (verificar serviço/porta/mongosh, instalar via MSI se ausente, connection string, credenciais opcionais, Compass)

## 3. Modelo de domínio e persistência

- [x] 3.1 Criar `TodoStatus` (`ToDo`, `Doing`, `Done`) e a entidade `Todo` com `Create`, `Update` e `ChangeStatus` (trim, `UpdatedAt` só quando há mudança, `TimeProvider`)
- [x] 3.2 Configurar o mapeamento BSON (ObjectId, status como string, datas UTC)
- [x] 3.3 Criar DTOs de resposta (`TodoResponse`) e mapeamento entidade → DTO com `id` string e datas ISO 8601 UTC
- [x] 3.4 Criar o acesso a dados (insert, get by id, replace, delete, find com filtro por status e busca literal case-insensitive ordenada por `createdAt` desc)

## 4. Commands, Queries e handlers

- [x] 4.1 Definir o fluxo CQRS: Commands e Queries como `ICommand<TResult>` tratados por `ICommandHandler<TCommand,TResult>` do Command Bus nativo do FastEndpoints (`FastEndpoints.Messaging.Core`, só na Application), o tipo `Result` (sucesso/NotFound) e `ITodoRepository` (Domain) / `ITodoReadRepository` (Application); handlers descobertos pelo FastEndpoints
- [x] 4.2 Implementar `CreateTodoCommand` + handler
- [x] 4.3 Implementar `UpdateTodoCommand` + handler (NotFound, sem alteração não muda `UpdatedAt`)
- [x] 4.4 Implementar `ChangeTodoStatusCommand` + handler
- [x] 4.5 Implementar `DeleteTodoCommand` + handler
- [x] 4.6 Implementar `GetTodoByIdQuery` e `GetTodosQuery` + handlers

## 5. Endpoints FastEndpoints

- [x] 5.1 `POST /api/todos` (201 + Location) com request e validator (título 1–100 com trim, descrição ≤ 500)
- [x] 5.2 `GET /api/todos` com `status` e `search` e validação do enum
- [x] 5.3 `GET /api/todos/{id}` com validação do formato do id antes do banco
- [x] 5.4 `PUT /api/todos/{id}` e `PATCH /api/todos/{id}/status` (200) e `DELETE /api/todos/{id}` (204)
- [x] 5.5 Padronizar erros Problem Details (400/404/500/503), mensagens em pt-BR nos validators e documentar `Summary`/`Produces` no Swagger
- [x] 5.6 Verificar manualmente no Swagger todos os endpoints contra o MongoDB local

## 6. Testes do back-end

- [x] 6.1 Testes unitários do domínio `Todo` e do `TodoId` em `AiTodo.Domain.UnitTests` (regras 1–6 e 9 da seção de regras de negócio)
- [x] 6.2 Testes unitários dos handlers (sucesso, NotFound, sem alteração) em `AiTodo.Application.UnitTests`, com `InMemoryTodoRepository` e `FakeClock`
- [x] 6.3 Testes dos validators em `AiTodo.Api.IntegrationTests` (título vazio/espaços/101 chars, descrição 501, id inválido, status inválido)
- [x] 6.4 Testes dos endpoints em `AiTodo.Api.IntegrationTests` (códigos HTTP e formato de erro, JSON malformado, 400 de domínio, 500 genérico, 503, Swagger, CORS)
- [x] 6.5 Testes de integração com MongoDB local em banco `AiTodoDb_Tests_<guid>` descartado ao final (sem Testcontainers/Docker); skip explícito se `MongoTests__ConnectionString` não estiver definida; garantir que `AiTodoDb` nunca é usado; teste de tradução de falhas em `DataStoreUnavailableException`; testes de regras de dependência entre camadas
- [x] 6.6 Executar `dotnet test` e registrar o resultado — após a refatoração para camadas; sem `MongoTests__ConnectionString`: 249 aprovados (62 Domain + 30 Application + 157 Api), 26 ignorados (integração, com mensagem explícita), 0 falhas; com `MongoTests__ConnectionString=mongodb://localhost:27017`: 283 aprovados (62 + 30 + 191), 0 ignorados, 0 falhas (14/10/2026)

## 7. Estrutura do front-end React

- [x] 7.1 Criar `web/` com Vite + React + TypeScript (strict), ESLint e Vitest + React Testing Library configurados
- [x] 7.2 Instalar Mantine (core, hooks, form, notifications) e TanStack Query; configurar providers, tema e layout responsivo
- [x] 7.3 Criar `.env.example` com `VITE_API_BASE_URL` e módulo central de mensagens pt-BR
- [x] 7.4 Criar tipos TypeScript e cliente da API (`fetch` tipado, tratamento de Problem Details)

## 8. Integração com a API

- [x] 8.1 Implementar o quadro Kanban com três colunas e cartões (título, descrição, data pt-BR)
- [x] 8.2 Implementar formulário de criação/edição com validação no cliente e exibição de erros da API por campo
- [x] 8.3 Implementar mudança de status por menu/botões no cartão e exclusão com confirmação
- [x] 8.4 Implementar busca por título (debounce) e filtro por status
- [x] 8.5 Implementar estados de carregamento, erro com "Tentar novamente", vazio e notificações de sucesso/erro
- [x] 8.6 Validar fluxo completo manualmente com API e MongoDB locais (incluindo CORS)

## 9. Testes do front-end

- [x] 9.1 Testes do cliente da API (sucesso, 400, 404, rede)
- [x] 9.2 Testes de componentes (cartão, coluna, quadro) e dos formulários
- [x] 9.3 Testes de alteração de status e de exclusão com confirmação/cancelamento
- [x] 9.4 Testes de carregamento, erro, vazio e busca sem resultados
- [x] 9.5 Executar `npm test` e `npm run build`

## 10. Validação completa e documentação de uso

- [x] 10.1 Criar `README.md` com pré-requisitos, comandos de MongoDB, API, front-end e testes (Windows/PowerShell, sem Docker)
- [x] 10.2 Percorrer os critérios de aceite das specs (matriz requisito → teste/verificação) e corrigir lacunas
- [x] 10.3 Confirmar ausência de segredos versionados e de qualquer dependência de Docker
- [x] 10.4 Registrar evoluções futuras (paginação, drag and drop, concorrência otimista, índice de texto)
