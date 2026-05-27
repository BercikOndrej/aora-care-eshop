import { useQuery } from '@tanstack/react-query'
import apiClient from './client'

type HelloResponse = {
  readonly message: string
}

async function fetchHello(): Promise<HelloResponse> {
  const { data } = await apiClient.get<HelloResponse>('/hello')
  return data
}

export function useHello() {
  return useQuery({
    queryKey: ['hello'],
    queryFn: fetchHello,
  })
}
