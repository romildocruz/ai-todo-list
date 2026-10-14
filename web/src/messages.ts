/** Textos de interface em pt-BR, centralizados. Identificadores de código ficam em inglês. */
export const messages = {
  appTitle: 'AI Todo List',
  appSubtitle: 'Organize suas atividades no quadro Kanban.',

  columns: {
    ToDo: 'A fazer',
    Doing: 'Em andamento',
    Done: 'Concluído',
  },

  actions: {
    create: 'Nova atividade',
    edit: 'Editar',
    delete: 'Excluir',
    save: 'Salvar',
    cancel: 'Cancelar',
    confirm: 'Confirmar',
    retry: 'Tentar novamente',
    moveTo: 'Mover para',
  },

  fields: {
    title: 'Título',
    description: 'Descrição',
    search: 'Buscar por título',
    statusFilter: 'Filtrar por status',
    allStatuses: 'Todos os status',
  },

  validation: {
    titleRequired: 'O título é obrigatório.',
    titleTooLong: 'O título deve ter no máximo 100 caracteres.',
    descriptionTooLong: 'A descrição deve ter no máximo 500 caracteres.',
  },

  states: {
    loading: 'Carregando atividades...',
    loadError: 'Não foi possível carregar as atividades.',
    emptyBoard: 'Nenhuma atividade ainda. Crie a primeira atividade para começar.',
    emptyColumn: 'Nenhuma atividade nesta coluna.',
    noResults: 'Nenhuma atividade encontrada.',
  },

  notifications: {
    created: 'Atividade criada com sucesso.',
    updated: 'Atividade atualizada com sucesso.',
    statusChanged: 'Status alterado com sucesso.',
    deleted: 'Atividade excluída com sucesso.',
    error: 'Não foi possível concluir a operação.',
  },

  confirmDelete: {
    title: 'Excluir atividade',
    body: 'Esta exclusão é definitiva. Deseja continuar?',
  },

  /** Erros do cliente HTTP (a API já devolve as demais mensagens em pt-BR). */
  errors: {
    network: 'Não foi possível conectar à API. Verifique sua conexão e tente novamente.',
    notFound: 'Atividade não encontrada.',
    unavailable: 'Serviço temporariamente indisponível. Tente novamente em instantes.',
    unexpected: 'Ocorreu um erro inesperado. Tente novamente mais tarde.',
    invalid: 'Requisição inválida.',
  },
} as const
