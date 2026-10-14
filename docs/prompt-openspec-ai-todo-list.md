# Projeto: AI Todo List — Especificação e Arquitetura com OpenSpec

## 1. Objetivo

Quero desenvolver um projeto de estudo chamado AI Todo List para aprofundar meus conhecimentos em desenvolvimento de software moderno, arquitetura de aplicações, boas práticas de programação e utilização de Inteligência Artificial durante o ciclo de desenvolvimento.

O sistema será uma aplicação web para gerenciamento de tarefas, permitindo cadastrar, consultar, editar, alterar o status e excluir atividades.

Utilize o OpenSpec para analisar os requisitos e produzir toda a documentação necessária para o projeto antes de qualquer implementação.

**Nesta primeira etapa, gere somente a documentação e as especificações. Não gere código-fonte da aplicação.**

## 2. Tecnologias obrigatórias

### Back-end
- .NET 10 com C#.
- ASP.NET Core Web API.
- FastEndpoints para definição dos endpoints HTTP.
- CQRS (Command Query Responsibility Segregation).
- MongoDB como banco de dados.
- MongoDB.Driver para acesso ao banco.
- Injeção de dependência nativa do .NET.
- Validação das requisições.
- Swagger/OpenAPI.
- xUnit para testes automatizados.

### Front-end
- React.
- TypeScript.
- Vite.
- React Router, caso necessário.
- Biblioteca de componentes visuais adequada para uma interface moderna e responsiva.
- Comunicação com a API REST.
- Vitest e React Testing Library para testes.

### Ambiente de desenvolvimento
- Windows com ferramentas de desenvolvimento instaladas localmente.
- Visual Studio Code.
- .NET 10 SDK.
- Node.js e npm.
- MongoDB instalado e executado diretamente na máquina local.
- MongoDB Compass como ferramenta opcional de visualização dos dados.

**Não utilizar Docker, Docker Compose, contêineres ou Kubernetes nesta versão.** Não exigir instalação dessas ferramentas em nenhuma etapa do projeto.

## 3. Requisitos funcionais

O sistema deverá permitir o gerenciamento completo de atividades.

Cada atividade deverá conter:
- Id: identificador único compatível com MongoDB.
- Title: título obrigatório.
- Description: descrição opcional.
- Status: situação da atividade.
- CreatedAt: data e hora de criação.
- UpdatedAt: data e hora da última atualização.

Os status disponíveis serão exclusivamente:
- ToDo: atividade não iniciada.
- Doing: atividade em andamento.
- Done: atividade concluída.

As operações disponíveis serão:
1. Criar atividade.
2. Listar atividades.
3. Consultar atividade por identificador.
4. Editar título e descrição.
5. Alterar status.
6. Excluir atividade.
7. Filtrar por status.
8. Pesquisar pelo título.
9. Visualizar atividades agrupadas por status.

Documente as regras de validação, os cenários de erro e os critérios de aceite de cada operação.

## 4. Requisitos do front-end

A interface principal deverá ser um quadro Kanban responsivo com três colunas: To Do, Doing e Done.

Cada cartão deverá apresentar o título, a descrição quando disponível e a data de criação.

O usuário deverá conseguir:
- Criar atividades por formulário.
- Editar atividades.
- Excluir atividades com confirmação.
- Alterar o status.
- Pesquisar pelo título.
- Filtrar por status.
- Visualizar estados de carregamento.
- Visualizar mensagens de erro e sucesso.
- Visualizar mensagens quando não houver atividades.

Avalie a utilização de drag and drop para mover os cartões entre colunas. Se isso aumentar a complexidade inicial, deixe essa funcionalidade para uma evolução futura.

O front-end deverá consumir os dados exclusivamente por meio da API REST. Não utilizar armazenamento local do navegador como fonte principal das atividades.

As mensagens da interface devem estar em português do Brasil. O código-fonte e os identificadores técnicos devem utilizar inglês.

## 5. Arquitetura do back-end

Organize a aplicação utilizando CQRS, separando operações de escrita e leitura.

Adote uma organização baseada em funcionalidades (feature folders ou vertical slices), priorizando clareza e baixo acoplamento.

A estrutura deverá contemplar:
- Configuração da aplicação.
- Endpoints FastEndpoints.
- Commands e seus handlers.
- Queries e seus handlers.
- Entidade ou modelo de domínio.
- DTOs de requisição e resposta.
- Acesso ao MongoDB.
- Validações.
- Configurações da aplicação.
- Tratamento de erros.
- Testes automatizados.

### 5.1. CQRS

As operações de escrita deverão utilizar Commands, como:
- CreateTodoCommand.
- UpdateTodoCommand.
- ChangeTodoStatusCommand.
- DeleteTodoCommand.

As operações de consulta deverão utilizar Queries, como:
- GetTodoByIdQuery.
- GetTodosQuery.

Documente o fluxo de cada operação, desde o recebimento da requisição HTTP até o handler e o acesso ao banco de dados.

Não adicionar MediatR ou outra biblioteca de mensageria interna automaticamente. Avalie uma implementação simples, com handlers e dispatching explícitos, compatível com FastEndpoints e .NET 10.

A escolha arquitetural deverá ser documentada e justificada.

Nesta primeira versão, leitura e escrita utilizarão o mesmo banco MongoDB. Não implementar bancos separados para CQRS.

### 5.2. FastEndpoints

Documente:
- Organização dos endpoints.
- Contratos de requisição e resposta.
- Validações.
- Injeção de dependência.
- Códigos HTTP.
- Tratamento de exceções.
- Integração com Swagger/OpenAPI.

Verifique a compatibilidade das versões das dependências com .NET 10 antes de recomendá-las.

### 5.3. MongoDB local

O MongoDB deverá estar instalado e executado diretamente no computador, sem Docker.

Documente:
- Como verificar se o MongoDB está instalado.
- Como iniciar e verificar o serviço no Windows.
- Como verificar a porta local utilizada.
- Como configurar a connection string.
- Como criar ou selecionar o banco de dados.
- Como organizar a coleção de atividades.
- Como configurar o MongoDB.Driver.
- Como serializar os identificadores.
- Como armazenar e consultar datas em UTC.
- Como criar índices apropriados.
- Como tratar falhas de conexão.

Considere uma connection string de desenvolvimento semelhante a:

`mongodb://localhost:27017`

Utilize um nome de banco específico para o projeto, como `AiTodoDb`, e uma coleção chamada `Todos`.

A aplicação deverá ler a connection string de configuração externa ao código-fonte.

Não exigir autenticação no MongoDB para o ambiente local inicial caso a instalação esteja configurada sem autenticação. Documente como ajustar a configuração se o servidor local exigir credenciais.

Não utilizar Entity Framework Core.

Não presumir que o MongoDB esteja instalado corretamente: documente como verificar a instalação e apresente os passos necessários caso ele ainda não esteja disponível.

## 6. Contrato da API REST

Defina os seguintes endpoints iniciais:
- `POST /api/todos` — criar atividade.
- `GET /api/todos` — listar atividades com filtros opcionais.
- `GET /api/todos/{id}` — consultar atividade.
- `PUT /api/todos/{id}` — atualizar título e descrição.
- `PATCH /api/todos/{id}/status` — alterar status.
- `DELETE /api/todos/{id}` — excluir atividade.

Para cada endpoint, documente:
- Método HTTP e rota.
- Finalidade.
- Parâmetros de rota e query string.
- Corpo da requisição.
- Exemplo de requisição e resposta.
- Códigos HTTP possíveis.
- Regras de validação.
- Cenários de erro.

Utilize códigos HTTP adequados, incluindo 200, 201, 204, 400 e 404, conforme a operação.

Defina um formato consistente para erros de validação e falhas inesperadas.

Avalie paginação para a listagem, mantendo a solução inicial proporcional ao objetivo didático.

## 7. Regras de negócio

Documente as seguintes regras:

1. O título é obrigatório e não pode conter apenas espaços.
2. A descrição é opcional.
3. O status aceita somente ToDo, Doing ou Done.
4. Toda atividade nova começa com status ToDo.
5. A data de criação não pode ser alterada pelo usuário.
6. A data de atualização deve mudar quando houver uma alteração persistida.
7. Não é possível consultar, editar ou excluir uma atividade inexistente.
8. O identificador deve ser validado antes de consultar o MongoDB.
9. Operações inválidas não devem modificar os dados.
10. A API deve validar os dados independentemente das validações do front-end.

## 8. Testes automatizados

### Back-end
- Testes unitários de Commands e Queries.
- Testes de validação.
- Testes das regras de negócio.
- Testes dos endpoints.
- Testes de integração com MongoDB.
- Testes dos principais cenários de erro.

### Front-end
- Testes de componentes.
- Testes dos formulários.
- Testes da alteração de status.
- Testes de carregamento e erro.
- Testes da comunicação com a API.

Os testes de integração não devem depender de um banco compartilhado com os dados de desenvolvimento.

Como não haverá Docker, documente uma estratégia para executar testes de integração utilizando uma instância de MongoDB dedicada aos testes, configurada localmente ou em outro ambiente explicitamente disponibilizado para esse fim.

Não presuma que uma instância isolada de MongoDB estará disponível automaticamente. Explique as dependências necessárias e como executar os testes.

## 9. Execução local sem Docker

A documentação deverá ensinar a executar todos os componentes diretamente no computador.

### MongoDB
- Verificar a instalação.
- Iniciar o serviço local.
- Confirmar a conexão.
- Inspecionar os dados com MongoDB Compass, caso instalado.

### Back-end
- Restaurar as dependências NuGet.
- Configurar a connection string.
- Executar a API com o .NET CLI.
- Acessar o Swagger.
- Executar os testes.

### Front-end
- Instalar as dependências com npm.
- Configurar a URL da API por variável de ambiente.
- Executar o Vite em modo de desenvolvimento.
- Executar os testes.

Documente os comandos de terminal necessários para cada operação, considerando o Windows e o terminal integrado do VS Code.

Utilize arquivos de exemplo para configuração, sem armazenar credenciais reais.

Configure o CORS para permitir a comunicação entre o front-end e a API durante o desenvolvimento.

Não exigir Docker, Docker Compose, contêineres ou Kubernetes para executar, testar ou estudar a aplicação.

## 10. Artefatos do OpenSpec

Utilize o fluxo e as convenções da versão do OpenSpec instalada no ambiente.

Produza a documentação necessária antes de qualquer implementação, contemplando:
1. Proposta do projeto.
2. Especificações funcionais.
3. Design técnico.
4. Contratos da API.
5. Modelo de dados MongoDB.
6. Especificação do front-end.
7. Estratégia de testes.
8. Plano de implementação.
9. Instruções de configuração e execução local.
10. Critérios de aceite.

Utilize os diretórios e nomes de arquivos esperados pela versão instalada. Não invente comandos ou formatos incompatíveis com o OpenSpec.

Inclua diagramas Mermaid para representar:
- Arquitetura geral.
- Fluxo de Commands.
- Fluxo de Queries.
- Modelo de dados.
- Comunicação entre front-end, API, handlers e MongoDB.

Mantenha os requisitos rastreáveis até as decisões arquiteturais, tarefas e critérios de aceite.

## 11. Boas práticas

Oriente a implementação futura para:
- Código limpo e legível.
- Separação de responsabilidades.
- Princípios SOLID quando aplicáveis.
- Tipagem forte em C# e TypeScript.
- Tratamento consistente de erros.
- Configuração externa ao código-fonte.
- Ausência de segredos versionados.
- Dependências compatíveis e estáveis.
- Evitar duplicação de regras de negócio.
- Evitar abstrações desnecessárias.
- Evitar complexidade arquitetural sem benefício didático.

Explique as principais decisões técnicas e seus motivos.

## 12. Fora do escopo inicial

Não implementar:
- Docker ou Docker Compose.
- Kubernetes.
- Autenticação e autorização.
- Cadastro de usuários.
- Multi-tenancy.
- Integração com serviços externos de IA.
- Notificações.
- Microsserviços.
- Mensageria externa.
- Banco de dados distribuído.
- Implantação em nuvem.

Essas funcionalidades poderão ser avaliadas futuramente.

## 13. Sequência de trabalho

### Fase 1 — Análise
Analise os requisitos, identifique ambiguidades e verifique a compatibilidade das tecnologias.

### Fase 2 — Documentação
Gere a proposta, as especificações, o design técnico e os demais artefatos suportados pelo OpenSpec.

Não implemente código-fonte.

### Fase 3 — Revisão
Verifique consistência entre requisitos, arquitetura, API, MongoDB, front-end, testes e execução local.

### Fase 4 — Preparação
Organize a implementação futura em etapas pequenas e verificáveis:

1. Estrutura da solução .NET 10.
2. Configuração e conexão com MongoDB local.
3. Modelo de domínio e persistência.
4. Commands, Queries e handlers.
5. Endpoints FastEndpoints.
6. Testes do back-end.
7. Estrutura do front-end React.
8. Integração com a API.
9. Testes do front-end.
10. Validação completa e documentação de uso.

Não execute automaticamente nenhuma dessas etapas de implementação.

## 14. Resultado esperado

Ao concluir o trabalho, apresente:
- Lista dos artefatos de documentação criados.
- Resumo da arquitetura.
- Justificativa das decisões técnicas.
- Dependências e versões recomendadas.
- Pré-requisitos para execução local.
- Comandos para iniciar cada componente.
- Plano de implementação.
- Critérios de aceite.
- Dúvidas ou decisões pendentes.

O resultado deve ser uma especificação técnica consistente, pronta para revisão humana e para orientar a implementação posterior por uma IA de programação.

**Restrição final: não utilizar Docker em nenhuma etapa deste projeto. O MongoDB deverá ser executado diretamente na máquina local.**
