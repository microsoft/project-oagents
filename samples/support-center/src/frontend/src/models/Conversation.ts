import { Message } from './Message'

export interface Conversation {
  id: string
  metadata: { [key: string]: string }
  messages: Message[]
}
