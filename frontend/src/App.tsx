import type React from 'react'
import { useHello } from './api/hello'
import './index.css'

function App(): React.JSX.Element {
  const { data, isLoading, isError } = useHello()

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-2">
      <h1 className="text-2xl font-semibold text-gray-800">aora-care</h1>

      {isLoading && <p className="text-sm text-gray-500">Connecting to API…</p>}
      {isError && <p className="text-sm text-red-500">API unreachable</p>}
      {data && <p className="text-sm text-green-600">{data.message}</p>}
    </div>
  )
}

export default App
