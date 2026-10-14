import { Badge, Group, Paper, Stack, Text, Title } from '@mantine/core'
import type { Todo, TodoStatus } from '../../api'
import { messages } from '../../messages'
import { TodoCard } from './TodoCard'

interface TodoColumnProps {
  status: TodoStatus
  todos: Todo[]
  busyId?: string | null
  onEdit: (todo: Todo) => void
  onDelete: (todo: Todo) => void
  onChangeStatus: (todo: Todo, status: TodoStatus) => void
}

export function TodoColumn({ status, todos, busyId, onEdit, onDelete, onChangeStatus }: TodoColumnProps) {
  const headingId = `column-${status}`

  return (
    <Paper component="section" withBorder p="sm" bg="var(--mantine-color-default-hover)" aria-labelledby={headingId}>
      <Stack gap="sm">
        <Group justify="space-between">
          <Title order={2} size="h4" id={headingId}>
            {messages.columns[status]}
          </Title>
          <Badge variant="light" aria-label={`${todos.length} atividades`}>
            {todos.length}
          </Badge>
        </Group>

        {todos.length === 0 ? (
          <Text size="sm" c="dimmed">
            {messages.states.emptyColumn}
          </Text>
        ) : (
          todos.map((todo) => (
            <TodoCard
              key={todo.id}
              todo={todo}
              busy={busyId === todo.id}
              onEdit={onEdit}
              onDelete={onDelete}
              onChangeStatus={onChangeStatus}
            />
          ))
        )}
      </Stack>
    </Paper>
  )
}
