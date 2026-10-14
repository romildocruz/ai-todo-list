import { messages } from '../../messages'

export const MAX_TITLE = 100
export const MAX_DESCRIPTION = 500

export interface TodoFormValues {
  title: string
  description: string
}

/** Validação do formulário no cliente; a API valida de novo e seus erros são exibidos junto aos campos. */
export function validateTodoForm({ title, description }: TodoFormValues) {
  const errors: Partial<Record<keyof TodoFormValues, string>> = {}

  if (title.trim().length === 0) errors.title = messages.validation.titleRequired
  else if (title.trim().length > MAX_TITLE) errors.title = messages.validation.titleTooLong

  if (description.trim().length > MAX_DESCRIPTION) errors.description = messages.validation.descriptionTooLong

  return errors
}
