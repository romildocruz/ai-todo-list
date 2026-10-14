# Matriz de aceite: requisito → teste / verificação

Cada cenário das specs (`openspec/changes/bootstrap-ai-todo-list/specs/`) aponta para o teste automatizado ou para a verificação que o cobre.
Os nomes de testes foram abreviados; o prefixo indica a classe.

| Sigla | Arquivo |
|---|---|
| **DT** | `tests/AiTodo.Domain.UnitTests` (`TodoTests`, `TodoIdTests`, `TodoRulesTests`) |
| **CH / QH** | `tests/AiTodo.Application.UnitTests` (`CommandHandlerTests`, `QueryHandlerTests`) |
| **VT** | `Api.IntegrationTests/Validators/ValidatorTests` |
| **TE** | `Api.IntegrationTests/Endpoints/TodoEndpointsTests` |
| **EP** | `Api.IntegrationTests/Endpoints/ErrorHandlingAndPlatformTests` |
| **LOG** | `Api.IntegrationTests/Endpoints/RequestLoggingTests` |
| **AR** | `Api.IntegrationTests/Architecture/DependencyRulesTests` |
| **RI** | `Api.IntegrationTests/Integration/TodoRepositoriesIntegrationTests` (MongoDB real) |
| **MI / MA / MS** | `MongoInfrastructureTests` / `MongoApiIntegrationTests` / `MongoTestSettingsTests` |
| **WEB** | `web/src/**/*.test.ts(x)` (Vitest + RTL, API simulada) |

Legenda: ✅ teste automatizado · 🔎 verificação manual/inspeção · ⚠️ lacuna conhecida.

## todo-management

| Requisito / cenário | Cobertura |
|---|---|
| Estrutura da atividade — campos retornados | ✅ TE `Post_serializes_status_as_a_string_and_dates_in_utc_with_Z`, `Get_by_id_returns_200_with_the_todo` |
| Criar — título e descrição | ✅ TE `Post_creates_a_todo_returning_201...`; DT `Create_keeps_and_trims_the_description`; CH `Create_persists_a_ToDo...` |
| Criar — só título | ✅ DT `Create_treats_a_missing_or_blank_description_as_absent`; VT `Create_accepts_a_missing_or_blank_description` |
| Criar — título ausente/só espaços | ✅ TE `Post_without_a_usable_title_returns_400_problem_and_stores_nothing`; VT `Create_rejects_a_missing_or_blank_title...`; CH `Create_with_an_invalid_title_throws...` |
| Criar — campos do servidor ignorados | ✅ TE `Post_ignores_id_status_and_dates_sent_by_the_client` |
| Limites — título > 100 / descrição > 500 | ✅ DT `Create_accepts_a_title_of_exactly_100...`, `..._description_of_exactly_500...`; VT `Create_rejects_a_title_with_101_characters`, `..._description_with_501_characters`; TE `Post_with_a_title_over_100_characters_returns_400`, `Post_with_a_description_over_500_characters_returns_400` |
| Limites — trim nas bordas | ✅ DT `Create_trims_the_title`, `Title_limit_is_applied_after_trimming`; VT `Create_measures_the_title_after_trimming` |
| Consultar — existente / id inválido / inexistente | ✅ TE `Get_by_id_returns_200...`, `Get_by_id_with_a_malformed_id_returns_400...`, `Get_by_id_of_an_unknown_id_returns_404_problem`; QH `GetById_*` |
| Listar — sem filtros / status / busca / combinados | ✅ TE `List_returns_everything_newest_first`, `List_filters_by_status_name_ignoring_case`, `List_searches_by_title_and_combines_with_status`; RI `List_orders_newest_first`, `List_filters_by_status`, `List_combines_status_and_search_with_AND` |
| Listar — caracteres especiais na busca | ✅ RI `List_treats_regex_metacharacters_in_the_search_as_literal_text`, `List_matches_a_search_term_that_literally_contains_metacharacters` |
| Listar — status inválido / nenhum resultado | ✅ TE `List_with_an_invalid_status_returns_400_on_field_status`, `List_returns_200_with_an_empty_array_when_there_is_nothing`; QH `List_with_no_match_returns_an_empty_list_not_an_error` |
| Agrupamento por status (coluna vazia) | ✅ WEB `KanbanBoard › distribui as atividades nas colunas…`, `TodoColumn › mostra mensagem amigável quando a coluna está vazia` |
| Editar — sucesso / sem mudança / inválida / inexistente | ✅ TE `Put_updates_title_and_description_returning_200`, `Put_with_unchanged_values_returns_200_and_keeps_UpdatedAt`, `Put_with_an_invalid_title_returns_400_and_leaves_the_todo_unchanged`, `Put_of_an_unknown_id_returns_404_and_creates_nothing`; DT `Update_*`; CH `Update_*` |
| Alterar status — mudança / mesmo status / inválido / inexistente | ✅ TE `Patch_*`; DT `ChangeStatus_*`; CH `ChangeStatus_*`; VT `ChangeStatus_rejects_a_missing_status...` |
| Excluir — sucesso / inexistente | ✅ TE `Delete_returns_204_without_a_body_and_removes_only_that_todo`, `Delete_twice_returns_404_the_second_time`; CH `Delete_*` |
| Validação independente do cliente | ✅ TE `Post_*` (requisições HTTP diretas, sem o front-end) e VT |

## todo-api

| Requisito / cenário | Cobertura |
|---|---|
| Rotas e semântica HTTP (201, listagem, 204, status) | ✅ TE `Post_creates_a_todo_returning_201...`, `List_*`, `Delete_returns_204...`, `Patch_changes_the_status_returning_200` |
| Erros — validação 400 | ✅ TE `Post_reports_every_invalid_field_at_once`; VT |
| Erros — recurso inexistente 404 | ✅ TE `Get_by_id_of_an_unknown_id_returns_404_problem`, `Put_of_...`, `Patch_of_...` |
| Erros — id malformado | ✅ TE `*_with_a_malformed_id_returns_400`; VT `Every_endpoint_with_an_id_rejects_a_malformed_id` |
| Erros — JSON inválido | ✅ TE `Post_with_malformed_json_returns_400_problem_without_leaking_internals`, `Post_with_an_empty_json_body_returns_400_validation_error` |
| Erros — falha inesperada 500 | ✅ EP `An_unexpected_failure_returns_a_generic_500_problem_without_internal_details`, `A_domain_rule_violation_reaching_the_handlers_returns_400_problem_not_500` |
| Erros — banco indisponível 503 | ✅ EP `A_database_outage_returns_503_problem_and_the_api_stays_up`, `Every_data_endpoint_maps_a_database_outage_to_503`, `Validation_errors_do_not_reach_the_database_even_when_it_is_down` |
| OpenAPI — Swagger disponível | ✅ EP `Swagger_document_lists_the_six_todo_operations_and_their_responses`, `Swagger_documents_status_as_a_string_enum...` |
| CORS — origem permitida / não listada | ✅ EP `Cors_allows_the_configured_front_end_origin`, `Cors_preflight_for_the_front_end_allows_the_needed_methods_and_headers`, `Cors_does_not_allow_other_origins`; 🔎 preflight e `Location` exposto verificados com a API real (etapa 8.6) |
| CQRS — escrita via command / leitura via query | ✅ CH e QH (handlers por `ICommandHandler`); AR `Application_uses_only_the_command_bus_abstractions_of_FastEndpoints` |
| Cross-cutting (log) via Pre/Post Processors | ✅ LOG `Request_logs_received_and_completed_with_status_code` |

## todo-persistence

| Requisito / cenário | Cobertura |
|---|---|
| Documento persistido (ObjectId, status string, UTC, camelCase) | ✅ RI `Documents_are_stored_with_ObjectId_string_status_utc_dates_and_camelCase_names`, `Status_is_stored_by_name_for_every_value`, `A_missing_description_is_not_stored` |
| Id exposto como string | ✅ RI `The_read_model_returns_the_same_data_as_the_aggregate...`; TE `Post_creates_a_todo...` |
| Datas em UTC | ✅ DT `Dates_must_be_utc`; TE `Post_serializes_status_as_a_string_and_dates_in_utc_with_Z` |
| Índices — inicializações repetidas | ✅ MI `Starting_the_api_creates_the_indexes_and_restarting_is_idempotent`, `The_index_keys_match_the_listing_query_shape` |
| Configuração externa — sobrescrita / ausente | ✅ MI `Configured_values_override_the_defaults`, `Defaults_point_to_AiTodoDb_Todos_with_short_timeouts`, `Missing_connection_string_fails_with_a_message_naming_the_key_and_no_value`, `Invalid_settings_are_rejected_on_validation` |
| Falhas de conexão — MongoDB parado | ✅ MI `Every_repository_operation_translates_an_unreachable_server_into_DataStoreUnavailableException`, `The_index_initializer_does_not_crash_or_block_startup_when_mongodb_is_unreachable`; EP (503) |
| Falhas de conexão — recuperação | ⚠️ O inicializador tenta de novo a cada 10 s (código em `MongoIndexInitializer`), mas não há teste automatizado de "parar e subir o MongoDB". Verificação manual: parar o serviço, subir a API, iniciar o serviço e conferir o log de índices (ver guia do MongoDB, seção 4) |

## kanban-ui

| Requisito / cenário | Cobertura |
|---|---|
| Quadro com três colunas — distribuição | ✅ WEB `KanbanBoard › distribui as atividades nas colunas conforme o status` |
| Cartão sem descrição | ✅ WEB `TodoCard › exibe apenas título e data quando não há descrição` |
| Cartão com data pt-BR | ✅ WEB `TodoCard › exibe título, descrição e data de criação no padrão pt-BR` |
| Tela estreita (colunas empilhadas) | 🔎 `SimpleGrid cols={{ base: 1, md: 3 }}` em `KanbanBoard.tsx`; o jsdom não mede layout |
| Criação bem-sucedida | ✅ WEB `KanbanBoard › cria uma atividade e a mostra na coluna "A fazer"`; `TodoFormModal › cria a atividade…` |
| Título vazio (sem requisição) | ✅ WEB `TodoFormModal › não envia a requisição e mostra o erro quando o título está vazio`; `validateTodoForm` |
| Erro de validação da API junto aos campos | ✅ WEB `TodoFormModal › exibe os erros da API junto aos campos e mantém o formulário aberto` |
| Mover para Concluído / falha ao mover | ✅ WEB `KanbanBoard › alteração de status` (sucesso e falha) |
| Exclusão confirmada / cancelada | ✅ WEB `KanbanBoard › exclusão` (confirmada e cancelada) |
| Pesquisa por título (debounce) / filtro por status | ✅ WEB `KanbanBoard › busca e filtro` |
| Carregamento / falha com "Tentar novamente" / sem atividades / busca sem resultados | ✅ WEB `KanbanBoard › estados de carregamento, erro e vazio` |
| API como única fonte (recarregar a página) | ✅ WEB `KanbanBoard › usa a resposta da API como única fonte…`; lista é buscada ao montar (`useTodosQuery`) |
| Fluxo completo no navegador (API + MongoDB + CORS) | 🔎 Tarefa 8.6: percorrido manualmente em 14/10/2026 com a API e o MongoDB locais e o front-end em `http://localhost:5173`, com sucesso (relato do usuário); preflight e `Location` também verificados via HTTP |

## quality-and-local-setup

| Requisito / cenário | Cobertura |
|---|---|
| Testes do back-end (sem MongoDB; integração só se configurada) | ✅ `dotnet test` sem a variável: integração ignorada com mensagem (`MongoFact`/`MongoTheory`); MS `A_missing_or_blank_value_counts_as_not_configured` |
| Isolamento dos dados de teste | ✅ MS `The_guard_refuses_any_database_that_is_not_a_throwaway_test_one`, `Generated_database_names_are_unique_prefixed_and_accepted_by_the_guard`; MA `The_api_works_against_the_test_database_and_never_the_development_one` |
| Regras de dependência entre camadas | ✅ AR (11 testes) |
| Testes do front-end (API simulada) | ✅ `npm test` (32 testes, sem rede) |
| Execução local sem Docker — primeira execução / MongoDB ausente | 🔎 `README.md` + `docs/mongodb-local-windows.md` (seção 5 trata a instalação do MSI) |
| Arquivos de exemplo / sem segredos versionados | 🔎 `appsettings.Development.json.example` e `web/.env.example` existem; `appsettings.Development.json`, `.env` e `*.local.json` estão no `.gitignore`; busca por credenciais e por Docker/Compose/Testcontainers (10.3) não encontrou nada além de menções "sem Docker" na documentação |
| Rastreabilidade | ✅ este documento |

## Lacunas conhecidas

1. **Recuperação do MongoDB** (acima): sem teste automatizado, só verificação manual.
2. **Layout em tela estreita**: verificado por inspeção do código, sem teste visual.

O fluxo completo no navegador (tarefa 8.6) deixou de ser lacuna: foi validado manualmente.
