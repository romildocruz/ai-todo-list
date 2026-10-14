## Purpose

Definir o comportamento da interface web em formato Kanban para gerenciar atividades em português do Brasil, consumindo exclusivamente a API REST.

## ADDED Requirements

### Requirement: Quadro Kanban com três colunas
A interface SHALL exibir um quadro com as colunas "A fazer" (`ToDo`), "Em andamento" (`Doing`) e "Concluído" (`Done`). Cada cartão SHALL exibir título, descrição (quando houver) e data de criação formatada no padrão pt-BR. O layout SHALL ser responsivo, empilhando as colunas em telas estreitas.

#### Scenario: Atividades distribuídas por status
- **WHEN** a API retorna atividades com diferentes status
- **THEN** cada atividade aparece na coluna correspondente ao seu status

#### Scenario: Cartão sem descrição
- **WHEN** uma atividade não possui descrição
- **THEN** o cartão exibe apenas título e data de criação

#### Scenario: Tela estreita
- **WHEN** a largura da viewport é de dispositivo móvel
- **THEN** as colunas são exibidas empilhadas, sem rolagem horizontal da página

### Requirement: Criar e editar atividades por formulário
A interface SHALL oferecer formulário (modal ou painel) para criar e editar atividades, validando no cliente título obrigatório (sem apenas espaços), limite de 100 caracteres e descrição de até 500, e exibindo também os erros de validação retornados pela API junto aos campos.

#### Scenario: Criação bem-sucedida
- **WHEN** o usuário preenche um título válido e confirma
- **THEN** a atividade aparece na coluna "A fazer" e uma mensagem de sucesso é exibida

#### Scenario: Título vazio
- **WHEN** o usuário tenta salvar com título vazio
- **THEN** o formulário exibe mensagem de erro no campo e não envia a requisição

#### Scenario: Erro de validação da API
- **WHEN** a API responde 400 com erros por campo
- **THEN** as mensagens são exibidas junto aos campos correspondentes e o formulário permanece aberto

### Requirement: Alterar status
A interface SHALL permitir mover uma atividade para qualquer outra coluna por controle explícito no cartão (menu ou botões), sem exigir drag and drop. O estado visual SHALL refletir o resultado confirmado pela API.

#### Scenario: Mover para Concluído
- **WHEN** o usuário escolhe "Concluído" no cartão de uma atividade
- **THEN** a API é chamada, o cartão passa para a coluna "Concluído" e uma mensagem de sucesso é exibida

#### Scenario: Falha ao mover
- **WHEN** a API falha ao alterar o status
- **THEN** o cartão permanece na coluna original e uma mensagem de erro é exibida

### Requirement: Excluir com confirmação
A interface SHALL solicitar confirmação antes de excluir uma atividade.

#### Scenario: Exclusão confirmada
- **WHEN** o usuário confirma a exclusão
- **THEN** a atividade é removida do quadro e uma mensagem de sucesso é exibida

#### Scenario: Exclusão cancelada
- **WHEN** o usuário cancela a confirmação
- **THEN** nenhuma requisição de exclusão é enviada e a atividade permanece

### Requirement: Pesquisa e filtro
A interface SHALL permitir pesquisar pelo título (com debounce) e filtrar por status, aplicando os critérios por meio dos parâmetros da API.

#### Scenario: Pesquisa por título
- **WHEN** o usuário digita um termo no campo de busca
- **THEN** o quadro exibe somente atividades cujo título corresponde ao termo

#### Scenario: Filtro por status
- **WHEN** o usuário seleciona o status "Em andamento"
- **THEN** somente atividades `Doing` são exibidas

### Requirement: Estados de carregamento, erro e vazio
A interface SHALL exibir indicador de carregamento durante requisições, mensagem de erro com opção de tentar novamente quando a listagem falhar, e mensagem amigável quando uma coluna ou o quadro não tiver atividades. Todas as mensagens SHALL estar em português do Brasil.

#### Scenario: Carregamento inicial
- **WHEN** a listagem está em andamento
- **THEN** um indicador de carregamento é exibido

#### Scenario: Falha na listagem
- **WHEN** a API está indisponível
- **THEN** uma mensagem de erro com botão "Tentar novamente" é exibida

#### Scenario: Sem atividades
- **WHEN** não há atividades
- **THEN** uma mensagem orientando a criar a primeira atividade é exibida

#### Scenario: Busca sem resultados
- **WHEN** a pesquisa não encontra atividades
- **THEN** uma mensagem de "nenhuma atividade encontrada" é exibida

### Requirement: API como única fonte de dados
A interface MUST obter e alterar atividades exclusivamente via API REST. O armazenamento local do navegador MUST NOT ser usado como fonte das atividades. A URL base da API SHALL ser configurável por variável de ambiente.

#### Scenario: Recarregar a página
- **WHEN** o usuário recarrega a página
- **THEN** as atividades são obtidas novamente da API
