import { Citation } from './Citation'

export interface Message {
  id: string
  conversationId: string
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
  // Generic agent type
  Agent = 'Agent',
  Dispatcher = 'Dispatcher',
  CustomerInfo = 'CustomerInfo',
  Notification = 'Notification',
  Discount = 'Discount',
  Invoice = 'Invoice',
  QnA = 'QnA',
  ErrorNotification = 'ErrorNotification',
  // Agent-specific notification types
  DispatcherNotification = 'DispatcherNotification',
  CustomerInfoNotification = 'CustomerInfoNotification',
  AgentInfoNotification = 'AgentInfoNotification'
}