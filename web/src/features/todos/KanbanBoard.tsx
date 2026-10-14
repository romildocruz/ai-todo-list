import { Alert, Button, Center, Group, Loader, SimpleGrid, Stack, Text } from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { useState } from 'react'
import { TODO_STATUSES, type Todo, type TodoStatus } from '../../api'
import { messages } from '../../messages'
import { ConfirmDeleteModal } from './ConfirmDeleteModal'
import { TodoColumn } from './TodoColumn'
import { TodoFilters } from './TodoFilters'
import { TodoFormModal } from './TodoFormModal'
import { useChangeTodoStatus, useDeleteTodo, useTodosQuery } from './useTodos'

const SEARCH_DEBOUNCE_MS = 300

type FormState = { mode: 'closed' } | { mode: 'create' } | { mode: 'edit'; todo: Todo }

export function KanbanBoard() {
  const [search, setSearch] = useState('')
  const [debouncedSearch] = useDebouncedValue(search, SEARCH_DEBOUNCE_MS)
  const [statusFilter, setStatusFilter] = useState<TodoStatus | null>(null)
  const [form, setForm] = useState<FormState>({ mode: 'closed' })
  const [todoToDelete, setTodoToDelete] = useState<Todo | null>(null)

  const filters = { status: statusFilter ?? undefined, search: debouncedSearch.trim() || undefined }
  const { data: todos, isPending, isError, refetch, isRefetching } = useTodosQuery(filters)

  const changeStatus = useChangeTodoStatus()
  const deleteTodo = useDeleteTodo()

  const hasFilters = filters.status !== undefined || filters.search !== undefined
  const busyId = changeStatus.isPending ? (changeStatus.variables?.id ?? null) : null

  const confirmDelete = (todo: Todo) =>
    deleteTodo.mutate(todo.id, { onSettled: () => setTodoToDelete(null) })

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-end" wrap="wrap">
        <Button onClick={() => setForm({ mode: 'create' })}>{messages.actions.create}</Button>
      </Group>

      <TodoFilters
        search={search}
        status={statusFilter}
        onSearchChange={setSearch}
        onStatusChange={setStatusFilter}
      />

      {isPending ? (
        <Center py="xl" role="status">
          <Loader size="sm" mr="sm" />
          <Text>{messages.states.loading}</Text>
        </Center>
      ) : isError ? (
        <Alert color="red" title={messages.states.loadError} role="alert">
          <Button size="xs" variant="light" color="red" loading={isRefetching} onClick={() => void refetch()}>
            {messages.actions.retry}
          </Button>
        </Alert>
      ) : todos.length === 0 ? (
        <Center py="xl">
          <Text c="dimmed">{hasFilters ? messages.states.noResults : messages.states.emptyBoard}</Text>
        </Center>
      ) : (
        <SimpleGrid cols={{ base: 1, md: 3 }} spacing="md">
          {TODO_STATUSES.map((status) => (
            <TodoColumn
              key={status}
              status={status}
              todos={todos.filter((todo) => todo.status === status)}
              busyId={busyId}
              onEdit={(todo) => setForm({ mode: 'edit', todo })}
              onDelete={setTodoToDelete}
              onChangeStatus={(todo, next) => changeStatus.mutate({ id: todo.id, status: next })}
            />
          ))}
        </SimpleGrid>
      )}

      <TodoFormModal
        opened={form.mode !== 'closed'}
        todo={form.mode === 'edit' ? form.todo : null}
        onClose={() => setForm({ mode: 'closed' })}
      />
      <ConfirmDeleteModal
        todo={todoToDelete}
        loading={deleteTodo.isPending}
        onConfirm={confirmDelete}
        onCancel={() => setTodoToDelete(null)}
      />
    </Stack>
  )
}
