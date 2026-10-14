## Purpose

Definir o contrato REST da API de atividades: rotas, payloads, códigos HTTP, formato de erro, documentação OpenAPI e CORS de desenvolvimento.

## Requirements


### Requirement: Rotas e semântica HTTP
A API SHALL expor os endpoints abaixo com os códigos de sucesso indicados. Os corpos JSON SHALL usar `camelCase` e o `status` SHALL ser serializado como string (`"ToDo"`, `"Doing"`, `"Done"`).

| Método | Rota | Sucesso |
|---|---|---|
| POST | `/api/todos` | 201 + cabeçalho `Location` + atividade criada |
| GET | `/api/todos` | 200 + lista de atividades |
| GET | `/api/todos/{id}` | 200 + atividade |
| PUT | `/api/todos/{id}` | 200 + atividade atualizada |
| PATCH | `/api/todos/{id}/status` | 200 + atividade atualizada |
| DELETE | `/api/todos/{id}` | 204 sem corpo |

#### Scenario: Criação retorna 201
- **WHEN** `POST /api/todos` recebe `{"title":"Estudar","description":"CQRS"}`
- **THEN** a resposta é 201 com `Location: /api/todos/{id}` e corpo com a atividade (`status` = `"ToDo"`)

#### Scenario: Listagem com filtros
- **WHEN** `GET /api/todos?status=Doing&search=api` é chamado
- **THEN** a resposta é 200 com um array das atividades que atendem aos critérios

#### Scenario: Exclusão retorna 204
- **WHEN** `DELETE /api/todos/{id}` é chamado para atividade existente
- **THEN** a resposta é 204 sem corpo

#### Scenario: Alteração de status
- **WHEN** `PATCH /api/todos/{id}/status` recebe `{"status":"Done"}` para atividade existente
- **THEN** a resposta é 200 com a atividade já com status `"Done"`

### Requirement: Formato consistente de erros
A API SHALL retornar erros no formato Problem Details (RFC 9457/7807, `application/problem+json`). Erros de validação SHALL usar 400 e incluir a lista de erros por campo. Atividade inexistente SHALL usar 404. Falhas inesperadas SHALL usar 500 com mensagem genérica, sem expor detalhes internos (stack trace, connection string). Indisponibilidade do banco SHALL usar 503.

#### Scenario: Erro de validação
- **WHEN** `POST /api/todos` recebe título vazio
- **THEN** a resposta é 400 `application/problem+json` com erro associado ao campo `title`

#### Scenario: Recurso inexistente
- **WHEN** `GET /api/todos/{id}` é chamado com `id` válido que não existe
- **THEN** a resposta é 404 `application/problem+json`

#### Scenario: Identificador malformado
- **WHEN** `GET /api/todos/abc` é chamado
- **THEN** a resposta é 400 com erro no campo `id`, sem acesso ao banco

#### Scenario: Corpo JSON inválido
- **WHEN** a requisição contém JSON malformado ou `status` desconhecido
- **THEN** a resposta é 400 no mesmo formato de erro

#### Scenario: Falha inesperada
- **WHEN** ocorre uma exceção não tratada
- **THEN** a resposta é 500 `application/problem+json` com mensagem genérica e sem detalhes internos

#### Scenario: Banco indisponível
- **WHEN** o MongoDB não responde dentro do tempo limite configurado
- **THEN** a resposta é 503 `application/problem+json` e a API continua operante para novas tentativas

### Requirement: Documentação OpenAPI
A API SHALL publicar documentação OpenAPI e uma interface Swagger acessível em ambiente de desenvolvimento, descrevendo todos os endpoints, contratos, parâmetros e códigos de resposta.

#### Scenario: Swagger disponível
- **WHEN** a API é executada em ambiente de desenvolvimento e o Swagger é acessado
- **THEN** todos os seis endpoints são listados com seus esquemas e respostas

### Requirement: CORS de desenvolvimento
A API SHALL permitir requisições do front-end local por meio de uma lista de origens configurável externamente (padrão de desenvolvimento: `http://localhost:5173`). Origens não listadas MUST NOT ser permitidas.

#### Scenario: Origem permitida
- **WHEN** o front-end em `http://localhost:5173` chama a API
- **THEN** a resposta inclui os cabeçalhos CORS correspondentes

#### Scenario: Origem não listada
- **WHEN** uma origem fora da lista chama a API a partir de um navegador
- **THEN** a resposta não inclui cabeçalhos CORS permissivos

### Requirement: Organização CQRS explícita
Cada endpoint SHALL delegar a um Command (escrita) ou Query (leitura) tratado por um handler dedicado, com despacho explícito e sem biblioteca de mensageria interna. Leitura e escrita SHALL usar o mesmo banco MongoDB nesta versão.

#### Scenario: Escrita via command
- **WHEN** uma requisição de criação, edição, mudança de status ou exclusão é recebida
- **THEN** ela é executada por um Command e seu handler, e a regra de negócio reside fora do endpoint

#### Scenario: Leitura via query
- **WHEN** uma requisição de consulta ou listagem é recebida
- **THEN** ela é executada por uma Query e seu handler, sem efeitos colaterais nos dados
