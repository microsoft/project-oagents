import { Citation } from './Citation'

export interface Message {
  id: string
  userId: string
  sender: SenderType
  text: string
  timestamp: Date
  citations: undefined | Citation[]
  feedback: undefined | -1 | 1
  isError: boolean
}

export enum SenderType {
  User = 'User',
  Agent = 'Agent'
}