## Purpose

Definir como as atividades são armazenadas no MongoDB local: banco, coleção, mapeamento de campos, datas em UTC, índices, configuração externa e comportamento diante de falhas de conexão.

## ADDED Requirements

### Requirement: Banco, coleção e documento
O sistema SHALL armazenar as atividades no banco `AiTodoDb`, coleção `Todos`, em instância MongoDB executada diretamente na máquina local (sem contêineres). Cada documento SHALL conter `_id` (ObjectId), `title`, `description` (opcional), `status` (string), `createdAt` e `updatedAt` (UTC). Nomes de banco e coleção SHALL ser configuráveis.

#### Scenario: Documento persistido
- **WHEN** uma atividade é criada
- **THEN** a coleção `Todos` do banco `AiTodoDb` contém um documento com `_id` ObjectId, `status` = `"ToDo"` como string e datas em UTC

#### Scenario: Id exposto como string
- **WHEN** a API retorna uma atividade
- **THEN** o `id` é a representação em string de 24 caracteres hexadecimais do `_id`

### Requirement: Datas em UTC
O sistema SHALL gravar e ler `createdAt` e `updatedAt` como UTC, e a API SHALL retornar essas datas em ISO 8601 com designador UTC (`Z`).

#### Scenario: Data retornada em UTC
- **WHEN** uma atividade é lida
- **THEN** `createdAt` e `updatedAt` possuem sufixo `Z` e representam o mesmo instante gravado

### Requirement: Índices
O sistema SHALL garantir na inicialização índices que suportem a listagem: um em `createdAt` decrescente e um composto em `status` + `createdAt`. A criação de índices MUST ser idempotente.

#### Scenario: Inicializações repetidas
- **WHEN** a API é iniciada várias vezes
- **THEN** os índices existem uma única vez e nenhum erro é gerado

### Requirement: Configuração externa
A connection string, o nome do banco e o nome da coleção SHALL ser lidos de configuração externa ao código (arquivos de configuração, variáveis de ambiente ou user-secrets). O repositório MUST NOT conter credenciais reais; SHALL haver arquivo de exemplo. O padrão de desenvolvimento é `mongodb://localhost:27017`, sem autenticação, com documentação de como informar credenciais caso o servidor local as exija.

#### Scenario: Valor sobrescrito por ambiente
- **WHEN** uma variável de ambiente define a connection string
- **THEN** ela prevalece sobre o valor do arquivo de configuração

#### Scenario: Configuração ausente
- **WHEN** a connection string não está configurada
- **THEN** a API falha ao iniciar com mensagem clara indicando a chave ausente, sem expor valores sensíveis

### Requirement: Falhas de conexão
O sistema SHALL usar tempos limite de seleção de servidor/conexão configuráveis e curtos em desenvolvimento. Falhas de conexão SHALL ser registradas em log sem expor credenciais e traduzidas em resposta 503 pela API. A API MUST iniciar e atender `GET` de saúde/Swagger mesmo que o MongoDB esteja indisponível no momento da subida, recuperando-se quando o banco voltar.

#### Scenario: MongoDB parado
- **WHEN** o serviço MongoDB está parado e um endpoint de dados é chamado
- **THEN** a resposta é 503 em tempo limitado e o log registra a falha sem a connection string completa

#### Scenario: Recuperação
- **WHEN** o MongoDB é iniciado depois da falha
- **THEN** as chamadas seguintes voltam a funcionar sem reiniciar a API

### Requirement: Isolamento dos dados de teste
Os testes de integração SHALL usar um banco dedicado a testes (nome distinto, p.ex. `AiTodoDb_Tests_<id>`), configurado separadamente, e MUST NOT ler nem alterar o banco de desenvolvimento. Os testes SHALL limpar os dados que criarem.

#### Scenario: Execução de testes de integração
- **WHEN** os testes de integração são executados
- **THEN** somente o banco de testes é usado e `AiTodoDb` permanece intacto

#### Scenario: Instância de testes indisponível
- **WHEN** a instância de MongoDB de testes não está acessível
- **THEN** os testes de integração falham ou são ignorados com mensagem explícita, nunca recorrendo ao banco de desenvolvimento
