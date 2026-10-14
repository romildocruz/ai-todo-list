# AI Todo List

Aplicação de estudo para gerenciar atividades em um quadro Kanban (A fazer · Em andamento · Concluído).

- **Back-end:** .NET 10, N-Layers/Clean Architecture/DDD/CQRS, FastEndpoints (Command Bus nativo, sem MediatR), MongoDB.
- **Front-end:** React + TypeScript (Vite), Mantine, TanStack Query.
- Roda 100% localmente no Windows, **sem Docker**. Arquitetura e convenções: [`CLAUDE.md`](CLAUDE.md).

```
src/        Domain · Application · Infrastructure · Api
tests/      Domain.UnitTests · Application.UnitTests · Api.IntegrationTests · Tests.Common
web/        front-end (Vite + React)
docs/       guia de MongoDB local e matriz de aceite
openspec/   especificações, design e tarefas
```

## Pré-requisitos

| Item | Versão | Verificação (PowerShell) |
|---|---|---|
| .NET SDK | 10.x | `dotnet --list-sdks` |
| Node.js + npm | 22 ou superior (testado com 26) | `node --version` |
| MongoDB Community Server | local, porta 27017, como serviço do Windows | `Get-Service MongoDB` |

## 1. MongoDB

O guia completo (verificar/iniciar o serviço, porta, `mongosh`, instalar o MSI, autenticação, Compass) está em
[`docs/mongodb-local-windows.md`](docs/mongodb-local-windows.md). O essencial:

```powershell
Get-Service MongoDB                                  # Running?
Start-Service MongoDB                                # PowerShell como administrador
Test-NetConnection localhost -Port 27017             # TcpTestSucceeded : True
```

Connection string de desenvolvimento: `mongodb://localhost:27017` · banco `AiTodoDb` · coleção `Todos`.

## 2. API

```powershell
dotnet restore
Copy-Item src\AiTodo.Api\appsettings.Development.json.example src\AiTodo.Api\appsettings.Development.json
dotnet run --project src\AiTodo.Api --launch-profile http
```

- API: <http://localhost:5258/api> · saúde: `GET /api/health` · Swagger (Development): <http://localhost:5258/swagger>
- A connection string também pode vir de user-secrets ou da variável `Mongo__ConnectionString`
  (veja o guia do MongoDB). Sem ela, a API não inicia e informa a chave ausente.
- Com o MongoDB parado a API sobe normalmente, responde 503 nas rotas de dados e cria os índices quando o banco voltar.
- O CORS libera `http://localhost:5173` (chave `Cors:AllowedOrigins`).

| Método | Rota | Resultado |
|---|---|---|
| POST | `/api/todos` | 201 + `Location` |
| GET | `/api/todos?status=&search=` | 200 (lista, mais recentes primeiro) |
| GET | `/api/todos/{id}` | 200 / 404 |
| PUT | `/api/todos/{id}` | 200 / 404 |
| PATCH | `/api/todos/{id}/status` | 200 / 404 |
| DELETE | `/api/todos/{id}` | 204 / 404 |

Erros seguem Problem Details: 400 (validação/domínio), 404, 503 (banco indisponível) e 500 genérico.

## 3. Front-end

```powershell
cd web
npm install
Copy-Item .env.example .env        # VITE_API_BASE_URL=http://localhost:5258/api
npm run dev                        # http://localhost:5173
```

Scripts: `npm run lint`, `npm test`, `npm run build`.

## 4. Testes

```powershell
# Back-end: unitários e de endpoint rodam sem MongoDB; os de integração são ignorados (com mensagem) se a variável faltar
dotnet test

# Back-end com os testes de integração (MongoDB local; usam bancos descartáveis AiTodoDb_Tests_<guid>, nunca o AiTodoDb)
$env:MongoTests__ConnectionString = "mongodb://localhost:27017"
dotnet test

# Front-end (API simulada; não precisa de MongoDB nem da API)
cd web; npm test
```

Após cada mudança, rode `dotnet build` e `dotnet test` (e `npm test` no front-end).

## 5. Rastreabilidade

[`docs/acceptance-matrix.md`](docs/acceptance-matrix.md) liga cada requisito e cenário das specs ao teste ou à verificação
que o cobre, e lista as lacunas conhecidas.

## Evoluções futuras

Fora do escopo desta versão (registradas em `openspec/changes/bootstrap-ai-todo-list/design.md`):

- **Paginação** de `GET /api/todos` (`?page`, `?pageSize`), sem quebrar o contrato atual (hoje retorna um array simples).
- **Drag and drop** entre colunas (por exemplo, dnd-kit); hoje o status muda por menu no cartão.
- **Concorrência otimista:** campo `version` no documento para evitar *last-write-wins* em edições simultâneas.
- **Índice de texto** (ou Atlas Search) para a busca por título, que hoje usa regex literal sem índice.
- Autenticação, Domain Events e auditoria via processors adicionais.
