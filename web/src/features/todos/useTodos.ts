import { notifications } from '@mantine/notifications'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, todosApi, type CreateTodoInput, type ListTodosParams, type TodoStatus, type UpdateTodoInput } from '../../api'
import { messages } from '../../messages'

const todosKey = ['todos'] as const

/** Lista as atividades; a API é a única fonte de dados (filtros enviados como parâmetros). */
export function useTodosQuery(filters: ListTodosParams) {
  return useQuery({
    queryKey: [...todosKey, filters],
    queryFn: ({ signal }) => todosApi.list(filters, signal),
    // Ao mudar busca/filtro, mantém o quadro anterior até a nova resposta (sem piscar o carregamento).
    placeholderData: keepPreviousData,
  })
}

function notifySuccess(message: string) {
  notifications.show({ message, color: 'green' })
}

export function notifyError(error: unknown) {
  notifications.show({
    message: error instanceof ApiError ? error.message : messages.notifications.error,
    color: 'red',
  })
}

/**
 * Mutações sem otimismo: o quadro só muda depois da resposta da API, e a lista é recarregada em seguida.
 * Erros de validação (400) do formulário ficam com o chamador; os demais viram notificação.
 */
function useTodoMutation<TVariables>(
  mutationFn: (variables: TVariables) => Promise<unknown>,
  successMessage: string,
  { notifyOnError = true }: { notifyOnError?: boolean } = {},
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: async () => {
      notifySuccess(successMessage)
      await queryClient.invalidateQueries({ queryKey: todosKey })
    },
    onError: (error) => {
      if (notifyOnError) notifyError(error)
    },
  })
}

export const useCreateTodo = () =>
  useTodoMutation((input: CreateTodoInput) => todosApi.create(input), messages.notifications.created, {
    notifyOnError: false,
  })

export const useUpdateTodo = () =>
  useTodoMutation(
    ({ id, input }: { id: string; input: UpdateTodoInput }) => todosApi.update(id, input),
    messages.notifications.updated,
    { notifyOnError: false },
  )

export const useChangeTodoStatus = () =>
  useTodoMutation(
    ({ id, status }: { id: string; status: TodoStatus }) => todosApi.changeStatus(id, status),
    messages.notifications.statusChanged,
  )

export const useDeleteTodo = () =>
  useTodoMutation((id: string) => todosApi.remove(id), messages.notifications.deleted)
