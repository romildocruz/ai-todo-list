import { notifications } from '@mantine/notifications'
import '@testing-library/jest-dom/vitest'

// O store de notificações é global: limpa entre os testes.
afterEach(() => notifications.clean())

// Mantine usa matchMedia e ResizeObserver, ausentes no jsdom.
window.matchMedia ??= ((query: string) => ({
  matches: false,
  media: query,
  onchange: null,
  addListener: () => {},
  removeListener: () => {},
  addEventListener: () => {},
  removeEventListener: () => {},
  dispatchEvent: () => false,
})) as typeof window.matchMedia

globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
}

// Textarea com autosize escuta document.fonts, ausente no jsdom.
if (!('fonts' in document)) {
  Object.defineProperty(document, 'fonts', {
    value: { addEventListener: () => {}, removeEventListener: () => {} },
  })
}
