import { screen } from '@testing-library/react'
import App from './App'
import { messages } from './messages'
import { renderWithProviders } from './test/renderWithProviders'

describe('App', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('[]', { status: 200 })))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renderiza o título em pt-BR e a orientação do quadro vazio', async () => {
    renderWithProviders(<App />)

    expect(screen.getByRole('heading', { level: 1, name: messages.appTitle })).toBeInTheDocument()
    expect(await screen.findByText(messages.states.emptyBoard)).toBeInTheDocument()
  })
})
