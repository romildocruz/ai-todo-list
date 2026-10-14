## Purpose

Definir a estratégia de testes automatizados e as instruções de execução local no Windows, sem Docker, para MongoDB, API e front-end.

## ADDED Requirements

### Requirement: Testes automatizados do back-end
O projeto SHALL conter testes xUnit cobrindo: handlers de Commands e Queries, validações, regras de negócio, endpoints (HTTP) e principais cenários de erro, além de testes de integração com MongoDB. Cada requisito/cenário das specs SHALL ser coberto por ao menos um teste rastreável.

#### Scenario: Suíte do back-end
- **WHEN** `dotnet test` é executado na solução
- **THEN** testes unitários e de endpoint rodam sem MongoDB; testes de integração rodam apenas se a instância de testes estiver configurada

### Requirement: Testes automatizados do front-end
O projeto SHALL conter testes Vitest + React Testing Library cobrindo componentes, formulários, alteração de status, estados de carregamento/erro e a camada de comunicação com a API (com a API simulada).

#### Scenario: Suíte do front-end
- **WHEN** `npm test` é executado no front-end
- **THEN** os testes rodam sem depender da API real nem do MongoDB

### Requirement: Execução local sem Docker
A documentação SHALL ensinar, para Windows e terminal do VS Code, como: verificar a instalação e iniciar o serviço do MongoDB, confirmar porta e conexão (`mongosh`/Compass opcional) e instalar o MongoDB se ausente; restaurar pacotes, configurar a connection string, executar a API e acessar o Swagger; instalar dependências, configurar a URL da API e executar o Vite; e executar todos os testes. Nenhuma etapa MUST exigir Docker, Compose, contêineres ou Kubernetes.

#### Scenario: Primeira execução
- **WHEN** um desenvolvedor segue o guia em máquina Windows com os pré-requisitos
- **THEN** consegue subir MongoDB, API (com Swagger) e front-end e usar o quadro Kanban

#### Scenario: MongoDB ausente
- **WHEN** o MongoDB não está instalado
- **THEN** o guia indica como instalar o MongoDB Community Server como serviço do Windows e como validar a instalação

### Requirement: Configuração sem segredos versionados
O repositório SHALL fornecer arquivos de exemplo de configuração (API e front-end) e MUST NOT versionar segredos ou credenciais reais.

#### Scenario: Arquivos de exemplo
- **WHEN** o repositório é clonado
- **THEN** existem arquivos `*.example` com chaves e valores de desenvolvimento, e arquivos locais com segredos estão no `.gitignore`

### Requirement: Rastreabilidade e critérios de aceite
A documentação SHALL manter rastreabilidade requisito → decisão de design → tarefa → teste/critério de aceite, e as tarefas de implementação SHALL ser pequenas e verificáveis.

#### Scenario: Revisão de cobertura
- **WHEN** um revisor percorre as specs
- **THEN** cada requisito possui ao menos uma tarefa e um teste ou critério de aceite associado
