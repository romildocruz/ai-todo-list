import { Button, Group, Modal, Text } from '@mantine/core'
import type { Todo } from '../../api'
import { messages } from '../../messages'

interface ConfirmDeleteModalProps {
  /** Atividade a excluir; `null` mantém o modal fechado. */
  todo: Todo | null
  loading?: boolean
  onConfirm: (todo: Todo) => void
  onCancel: () => void
}

export function ConfirmDeleteModal({ todo, loading = false, onConfirm, onCancel }: ConfirmDeleteModalProps) {
  return (
    <Modal opened={todo !== null} onClose={onCancel} title={messages.confirmDelete.title}>
      <Text mb="xs" fw={600} style={{ overflowWrap: 'anywhere' }}>
        {todo?.title}
      </Text>
      <Text mb="md">{messages.confirmDelete.body}</Text>
      <Group justify="flex-end">
        <Button variant="default" onClick={onCancel} disabled={loading}>
          {messages.actions.cancel}
        </Button>
        <Button color="red" loading={loading} onClick={() => todo && onConfirm(todo)}>
          {messages.actions.delete}
        </Button>
      </Group>
    </Modal>
  )
}
