const formatter = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

/** Formata uma data ISO 8601 (UTC) no padrão pt-BR, no fuso do navegador. */
export function formatDate(iso: string): string {
  return formatter.format(new Date(iso))
}
