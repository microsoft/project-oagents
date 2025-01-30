import { createContext } from 'react'
import { Citation } from '../models/Citation'
import { Conversation } from '../models/Conversation'
import { Message } from '../models/Message'

export interface ChatContext {
  conversation: Conversation
  citationPreview: Citation | undefined
  isLoading: boolean
  isTakingTooLong: boolean
  waitingMessage: string
}

export const initialContext: ChatContext = {
  conversation: {
    id: '',
    metadata: {},
    messages: [],
  },
  citationPreview: undefined,
  isLoading: false,
  isTakingTooLong: false,
  waitingMessage: ''
}

export const ChatFeatureContext = createContext<ChatContext>(initialContext)

export interface ChatContextHandler {
  onRestartConversation: () => void
  onSendMessage: (messageText: string) => void
  onCitationPreview: (citation: Citation) => void
  onSendFeedback: (message: Message, feedback: -1 | 1) => void
}

export const initialContextHandler: ChatContextHandler = {
  onRestartConversation: () => new Error('onRestartConversation is not initialized'),
  onSendMessage: () => new Error('onSendMessage is not initialized'),
  onCitationPreview: () => new Error('onCitationPreview is not initialized'),
  onSendFeedback: () => new Error('onSendFeedback is not initialized'),
}

export const ChatFeatureContextHandler = createContext<ChatContextHandler>(initialContextHandler)
