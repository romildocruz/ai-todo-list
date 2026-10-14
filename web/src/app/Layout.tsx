import { Container, Stack, Text, Title } from '@mantine/core'
import type { ReactNode } from 'react'
import { messages } from '../messages'

/** Casca responsiva da página: conteúdo centralizado com margens que se adaptam à largura da tela. */
export function Layout({ children }: { children: ReactNode }) {
  return (
    <Container size="xl" px={{ base: 'sm', sm: 'md' }} py={{ base: 'md', sm: 'xl' }}>
      <Stack gap="lg">
        <header>
          <Title order={1}>{messages.appTitle}</Title>
          <Text c="dimmed">{messages.appSubtitle}</Text>
        </header>
        <main>{children}</main>
      </Stack>
    </Container>
  )
}
