import { MantineProvider } from '@mantine/core'
import { Notifications } from '@mantine/notifications'
import { QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { createQueryClient } from './queryClient'
import { theme } from './theme'

import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'

interface AppProvidersProps {
  children: ReactNode
  /** `test` desliga transições e portais do Mantine (usado nos testes). */
  env?: 'default' | 'test'
}

export function AppProviders({ children, env = 'default' }: AppProvidersProps) {
  const [queryClient] = useState(createQueryClient)

  return (
    <MantineProvider theme={theme} env={env}>
      <Notifications position="top-right" />
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    </MantineProvider>
  )
}
