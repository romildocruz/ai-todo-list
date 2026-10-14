import { Group, Select, TextInput } from '@mantine/core'
import { TODO_STATUSES, type TodoStatus } from '../../api'
import { messages } from '../../messages'

interface TodoFiltersProps {
  search: string
  status: TodoStatus | null
  onSearchChange: (search: string) => void
  onStatusChange: (status: TodoStatus | null) => void
}

const statusOptions = TODO_STATUSES.map((status) => ({ value: status, label: messages.columns[status] }))

export function TodoFilters({ search, status, onSearchChange, onStatusChange }: TodoFiltersProps) {
  return (
    <Group grow align="flex-end" preventGrowOverflow={false} wrap="wrap">
      <TextInput
        label={messages.fields.search}
        value={search}
        onChange={(event) => onSearchChange(event.currentTarget.value)}
        style={{ flex: '2 1 14rem' }}
      />
      <Select
        label={messages.fields.statusFilter}
        placeholder={messages.fields.allStatuses}
        data={statusOptions}
        value={status}
        onChange={(value) => onStatusChange(value as TodoStatus | null)}
        clearable
        style={{ flex: '1 1 12rem' }}
      />
    </Group>
  )
}
