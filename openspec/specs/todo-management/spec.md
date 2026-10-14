## Purpose

Define as regras de negócio e o comportamento observável do gerenciamento de atividades (todos): criação, consulta, edição, mudança de status, exclusão, filtro, pesquisa e agrupamento.

## Requirements


### Requirement: Estrutura da atividade
O sistema SHALL representar cada atividade com `id`, `title`, `description` (opcional), `status`, `createdAt` e `updatedAt`. O `status` MUST ser exatamente um de `ToDo`, `Doing` ou `Done`. `createdAt` e `updatedAt` SHALL ser datas/horas em UTC.

#### Scenario: Atividade retornada contém todos os campos
- **WHEN** uma atividade existente é consultada
- **THEN** a resposta contém `id`, `title`, `description`, `status`, `createdAt` e `updatedAt`

### Requirement: Criar atividade
O sistema SHALL permitir criar uma atividade informando `title` (obrigatório) e `description` (opcional). Toda atividade nova SHALL iniciar com status `ToDo`, e `createdAt` e `updatedAt` SHALL ser definidos pelo servidor com o mesmo valor. O cliente MUST NOT conseguir definir `id`, `status`, `createdAt` ou `updatedAt` na criação.

#### Scenario: Criação com título e descrição
- **WHEN** o cliente cria uma atividade com título "Estudar CQRS" e descrição "Ler sobre handlers"
- **THEN** a atividade é persistida com status `ToDo`, `id` gerado e datas definidas pelo servidor

#### Scenario: Criação apenas com título
- **WHEN** o cliente cria uma atividade com título "Estudar CQRS" sem descrição
- **THEN** a atividade é persistida com descrição vazia/nula e status `ToDo`

#### Scenario: Título ausente ou só com espaços
- **WHEN** o cliente cria uma atividade com título ausente, vazio ou contendo apenas espaços
- **THEN** a operação é rejeitada com erro de validação e nenhum dado é persistido

#### Scenario: Campos controlados pelo servidor são ignorados
- **WHEN** o cliente envia `status`, `createdAt` ou `id` no corpo da criação
- **THEN** esses valores não são usados; a atividade nasce com status `ToDo` e datas do servidor

### Requirement: Limites de tamanho e normalização
O sistema SHALL aplicar `trim` ao título e à descrição antes de validar e persistir. O título MUST ter no máximo 100 caracteres e a descrição no máximo 500 caracteres. Uma descrição que resulte vazia após o `trim` SHALL ser tratada como ausente.

#### Scenario: Título excede o limite
- **WHEN** o cliente envia um título com 101 caracteres
- **THEN** a operação é rejeitada com erro de validação identificando o campo `title`

#### Scenario: Espaços nas bordas são removidos
- **WHEN** o cliente envia o título "  Comprar pão  "
- **THEN** a atividade é persistida com o título "Comprar pão"

### Requirement: Consultar atividade por identificador
O sistema SHALL permitir consultar uma atividade pelo `id`. O formato do `id` MUST ser validado antes de qualquer acesso ao banco. Uma atividade inexistente SHALL resultar em erro "não encontrado".

#### Scenario: Atividade existente
- **WHEN** o cliente consulta um `id` existente
- **THEN** a atividade correspondente é retornada

#### Scenario: Identificador com formato inválido
- **WHEN** o cliente consulta um `id` que não é um identificador válido
- **THEN** a operação é rejeitada com erro de validação sem consultar o banco

#### Scenario: Atividade inexistente
- **WHEN** o cliente consulta um `id` válido que não existe
- **THEN** o sistema responde "não encontrado"

### Requirement: Listar, filtrar e pesquisar atividades
O sistema SHALL listar todas as atividades, ordenadas por `createdAt` decrescente (desempate por `id`). O sistema SHALL permitir filtrar por `status` e pesquisar por trecho do título, sem diferenciar maiúsculas/minúsculas, e os dois critérios SHALL poder ser combinados (AND). A pesquisa MUST tratar o texto informado como literal, nunca como expressão regular. Esta versão NÃO inclui paginação.

#### Scenario: Listagem sem filtros
- **WHEN** o cliente lista atividades sem parâmetros
- **THEN** todas as atividades são retornadas, da mais recente para a mais antiga

#### Scenario: Filtro por status
- **WHEN** o cliente lista com `status=Doing`
- **THEN** somente atividades com status `Doing` são retornadas

#### Scenario: Pesquisa por título
- **WHEN** o cliente pesquisa por "estudar"
- **THEN** são retornadas as atividades cujo título contém "estudar", ignorando maiúsculas/minúsculas

#### Scenario: Pesquisa com caracteres especiais
- **WHEN** o cliente pesquisa por "a.*"
- **THEN** o texto é buscado literalmente e não como expressão regular

#### Scenario: Filtro e pesquisa combinados
- **WHEN** o cliente informa `status=Done` e pesquisa "api"
- **THEN** são retornadas apenas atividades `Done` cujo título contém "api"

#### Scenario: Status de filtro inválido
- **WHEN** o cliente informa `status=Pending`
- **THEN** a operação é rejeitada com erro de validação

#### Scenario: Nenhum resultado
- **WHEN** nenhum registro atende aos critérios
- **THEN** o sistema retorna uma lista vazia com sucesso (não é erro)

### Requirement: Visualizar atividades agrupadas por status
O sistema SHALL permitir que o cliente visualize as atividades agrupadas em `ToDo`, `Doing` e `Done`, mesmo quando um grupo estiver vazio. O agrupamento SHALL poder ser obtido a partir da listagem (agrupamento feito pelo cliente), mantendo a API de listagem simples.

#### Scenario: Agrupamento com coluna vazia
- **WHEN** não há atividades `Done`
- **THEN** o grupo `Done` é exibido vazio, sem erro

### Requirement: Editar título e descrição
O sistema SHALL permitir alterar `title` e `description` de uma atividade existente, com as mesmas regras de validação da criação. A operação SHALL substituir ambos os campos (descrição ausente limpa a descrição). `id`, `status` e `createdAt` MUST NOT ser alterados por esta operação. `updatedAt` SHALL ser atualizado somente quando houver alteração efetivamente persistida.

#### Scenario: Edição bem-sucedida
- **WHEN** o cliente altera o título de uma atividade existente para um valor diferente
- **THEN** o título é persistido, `updatedAt` é atualizado e `createdAt` permanece igual

#### Scenario: Edição sem mudança de valores
- **WHEN** o cliente envia os mesmos título e descrição já persistidos
- **THEN** a operação responde sucesso e `updatedAt` não muda

#### Scenario: Edição inválida não altera dados
- **WHEN** o cliente envia título vazio na edição
- **THEN** a operação é rejeitada e a atividade permanece inalterada

#### Scenario: Edição de atividade inexistente
- **WHEN** o cliente edita um `id` válido que não existe
- **THEN** o sistema responde "não encontrado" e nada é criado

### Requirement: Alterar status
O sistema SHALL permitir alterar o status de uma atividade existente para qualquer valor válido (`ToDo`, `Doing`, `Done`), sem restrição de transição. `updatedAt` SHALL ser atualizado somente quando o status efetivamente mudar.

#### Scenario: Mudança de status
- **WHEN** o cliente altera o status de `ToDo` para `Done`
- **THEN** o novo status é persistido e `updatedAt` é atualizado

#### Scenario: Mesmo status
- **WHEN** o cliente define o status igual ao atual
- **THEN** a operação responde sucesso e `updatedAt` não muda

#### Scenario: Status inválido
- **WHEN** o cliente envia status "Blocked"
- **THEN** a operação é rejeitada com erro de validação e a atividade permanece inalterada

#### Scenario: Atividade inexistente
- **WHEN** o cliente altera o status de um `id` válido inexistente
- **THEN** o sistema responde "não encontrado"

### Requirement: Excluir atividade
O sistema SHALL permitir excluir uma atividade existente de forma definitiva. Excluir uma atividade inexistente SHALL resultar em "não encontrado".

#### Scenario: Exclusão bem-sucedida
- **WHEN** o cliente exclui uma atividade existente
- **THEN** a atividade deixa de existir e consultas posteriores respondem "não encontrado"

#### Scenario: Exclusão de inexistente
- **WHEN** o cliente exclui um `id` válido inexistente
- **THEN** o sistema responde "não encontrado"

### Requirement: Validação independente do cliente
O sistema SHALL validar todas as entradas no servidor, independentemente de validações do front-end. Operações inválidas MUST NOT modificar dados.

#### Scenario: Requisição fora do front-end
- **WHEN** um cliente HTTP qualquer envia dados inválidos diretamente à API
- **THEN** a API rejeita a requisição com erro de validação e nenhum dado é alterado
