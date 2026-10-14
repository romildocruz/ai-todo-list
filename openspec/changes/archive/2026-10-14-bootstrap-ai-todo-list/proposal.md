## Why

Projeto de estudo para praticar desenvolvimento moderno (.NET 10, CQRS, MongoDB, React) e uso de IA no ciclo de desenvolvimento. Antes de qualquer código, é preciso uma especificação consistente e rastreável (requisitos → decisões → tarefas → critérios de aceite) que oriente a implementação posterior por uma IA de programação.

## What Changes

- Definir a aplicação **AI Todo List**: gerenciamento de atividades (criar, listar, consultar, editar, alterar status, excluir, filtrar por status, pesquisar por título, agrupar por status).
- Especificar a API REST (`/api/todos`) com FastEndpoints, CQRS explícito (sem MediatR), validação e formato de erro consistente.
- Especificar a persistência em MongoDB local (banco `AiTodoDb`, coleção `Todos`, datas UTC, índices, tratamento de falhas), sem Docker.
- Especificar o front-end Kanban (React + TypeScript + Vite + Mantine) em pt-BR, consumindo exclusivamente a API.
- Definir a estratégia de testes (xUnit; Vitest + React Testing Library) incluindo integração com MongoDB dedicado a testes.
- Documentar execução local no Windows (MongoDB, API, front-end) e CORS de desenvolvimento.
- Organizar a implementação futura em 10 etapas pequenas e verificáveis.
- **Fora do escopo desta versão**: paginação, drag and drop, autenticação, Docker/Kubernetes, serviços externos de IA, mensageria, nuvem (registrados como evoluções futuras).
- Nenhum código-fonte é gerado nesta change; apenas artefatos de planejamento.

## Capabilities

### New Capabilities
- `todo-management`: regras de negócio e comportamento das operações sobre atividades (CRUD, status, filtro, busca, agrupamento), validações e cenários de erro.
- `todo-api`: contrato REST (rotas, payloads, códigos HTTP, formato de erro, Swagger/OpenAPI, CORS) e organização CQRS/FastEndpoints.
- `todo-persistence`: modelo de dados MongoDB, mapeamento de Id e datas UTC, índices, configuração externa e tratamento de falhas de conexão.
- `kanban-ui`: interface Kanban responsiva em pt-BR (cartões, formulários, confirmação de exclusão, busca/filtro, estados de carregamento/erro/vazio, feedback).
- `quality-and-local-setup`: estratégia de testes back-end/front-end, isolamento de integração com MongoDB e instruções de execução local sem Docker.

### Modified Capabilities
<!-- Nenhuma: projeto novo, sem specs existentes. -->

## Impact

- Código: nenhum nesta change; a implementação futura criará solução .NET 10 (`src/` + `tests/`) e app Vite em `web/` (a confirmar no design).
- Dependências previstas: FastEndpoints, MongoDB.Driver, FluentValidation (nativa do FastEndpoints), Swashbuckle/NSwag via FastEndpoints.Swagger, xUnit; React, Vite, Mantine, React Router, Vitest, React Testing Library. Versões a validar contra .NET 10 na etapa de design.
- Pré-requisitos locais: .NET 10 SDK (10.0.402 detectado), Node.js (v26.7.0 detectado), MongoDB Community Server local (**não detectado no PATH**; a documentação cobrirá verificação e instalação), VS Code.
- Sistemas: apenas ambiente local de desenvolvimento.
