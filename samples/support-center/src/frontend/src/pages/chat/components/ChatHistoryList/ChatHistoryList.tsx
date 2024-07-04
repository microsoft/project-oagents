import { useContext, useEffect, useRef } from 'react'
import { SenderType } from '../../../../models/Message'
import { ChatFeatureContext } from '../../../../states/ChatContext'
import { ChatMessage } from '../ChatMessage/ChatMessage'
import './ChatHistoryList.css'

export function ChatHistoryList() {
  const context = useContext(ChatFeatureContext)

  // Scrolling to the bottom of the list when a new message is added
  const messagesEnd = useRef<HTMLDivElement>(null)
  useEffect(() => messagesEnd?.current?.scrollIntoView({ behavior: 'smooth' }), [context.conversation.messages])

  return (
    <div className='messages-container'>
      {context.conversation.messages.map((message) => (
        <div key={message.id} className={message.sender === SenderType.User ? 'user-message-container' : 'copilot-message-container'}>
          <ChatMessage message={message} />
        </div>
      ))}
      <ChatMessage isLoading={context.isLoading} isTakingTooLong={context.isTakingTooLong} waitingMessage={context.waitingMessage} />
      <div ref={messagesEnd}></div>
    </div>
  )
}
