import { Layout } from './app/Layout'
import { KanbanBoard } from './features/todos/KanbanBoard'

export default function App() {
  return (
    <Layout>
      <KanbanBoard />
    </Layout>
  )
}
