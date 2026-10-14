import { Button, Group, Modal, Stack, Textarea, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { useEffect } from 'react'
import { ApiError, type Todo } from '../../api'
import { messages } from '../../messages'
import { notifyError, useCreateTodo, useUpdateTodo } from './useTodos'
import { validateTodoForm, type TodoFormValues } from './validateTodoForm'

interface TodoFormModalProps {
  opened: boolean
  /** Atividade em edição; `null` cria uma nova. */
  todo: Todo | null
  onClose: () => void
}

export function TodoFormModal({ opened, todo, onClose }: TodoFormModalProps) {
  const createTodo = useCreateTodo()
  const updateTodo = useUpdateTodo()
  const pending = createTodo.isPending || updateTodo.isPending

  const form = useForm<TodoFormValues>({
    mode: 'controlled',
    initialValues: { title: '', description: '' },
    validate: validateTodoForm,
  })

  // Cada abertura começa com os dados da atividade (edição) ou com o formulário limpo (criação).
  const { setValues, resetDirty, clearErrors } = form
  useEffect(() => {
    if (!opened) return
    const values = { title: todo?.title ?? '', description: todo?.description ?? '' }
    setValues(values)
    resetDirty(values)
    clearErrors()
  }, [opened, todo, setValues, resetDirty, clearErrors])

  const submit = form.onSubmit(async (values) => {
    const input = { title: values.title.trim(), description: values.description.trim() || null }

    try {
      if (todo) await updateTodo.mutateAsync({ id: todo.id, input })
      else await createTodo.mutateAsync(input)
      onClose()
    } catch (error) {
      // Erros por campo da API ficam junto aos campos e o formulário permanece aberto.
      if (error instanceof ApiError && Object.keys(error.fieldErrorMap).length > 0) {
        form.setErrors(error.fieldErrorMap)
      } else {
        notifyError(error)
      }
    }
  })

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={todo ? messages.actions.edit : messages.actions.create}
      closeOnClickOutside={!pending}
    >
      <form onSubmit={submit} noValidate>
        <Stack>
          <TextInput
            label={messages.fields.title}
            data-autofocus
            withAsterisk
            {...form.getInputProps('title')}
          />
          <Textarea
            label={messages.fields.description}
            autosize
            minRows={3}
            maxRows={8}
            {...form.getInputProps('description')}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose} disabled={pending}>
              {messages.actions.cancel}
            </Button>
            <Button type="submit" loading={pending}>
              {messages.actions.save}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}
