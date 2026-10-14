import { Button, Card, Group, Menu, Stack, Text } from '@mantine/core'
import { TODO_STATUSES, type Todo, type TodoStatus } from '../../api'
import { messages } from '../../messages'
import { formatDate } from './formatDate'

interface TodoCardProps {
  todo: Todo
  busy?: boolean
  onEdit: (todo: Todo) => void
  onDelete: (todo: Todo) => void
  onChangeStatus: (todo: Todo, status: TodoStatus) => void
}

export function TodoCard({ todo, busy = false, onEdit, onDelete, onChangeStatus }: TodoCardProps) {
  return (
    <Card withBorder padding="sm" component="article" aria-label={todo.title}>
      <Stack gap="xs">
        <Text fw={600} style={{ overflowWrap: 'anywhere' }}>
          {todo.title}
        </Text>
        {todo.description && (
          <Text size="sm" c="dimmed" style={{ overflowWrap: 'anywhere', whiteSpace: 'pre-wrap' }}>
            {todo.description}
          </Text>
        )}
        <Text size="xs" c="dimmed">
          <time dateTime={todo.createdAt}>{formatDate(todo.createdAt)}</time>
        </Text>

        <Group gap="xs" justify="flex-end">
          <Menu position="bottom-end" withinPortal>
            <Menu.Target>
              <Button size="compact-xs" variant="light" disabled={busy}>
                {messages.actions.moveTo}
              </Button>
            </Menu.Target>
            <Menu.Dropdown>
              {TODO_STATUSES.filter((status) => status !== todo.status).map((status) => (
                <Menu.Item key={status} onClick={() => onChangeStatus(todo, status)}>
                  {messages.columns[status]}
                </Menu.Item>
              ))}
            </Menu.Dropdown>
          </Menu>
          <Button size="compact-xs" variant="subtle" disabled={busy} onClick={() => onEdit(todo)}>
            {messages.actions.edit}
          </Button>
          <Button size="compact-xs" variant="subtle" color="red" disabled={busy} onClick={() => onDelete(todo)}>
            {messages.actions.delete}
          </Button>
        </Group>
      </Stack>
    </Card>
  )
}
