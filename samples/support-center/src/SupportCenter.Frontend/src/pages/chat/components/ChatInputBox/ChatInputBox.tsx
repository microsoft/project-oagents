import { Button, InputOnChangeData, Textarea, makeStyles } from '@fluentui/react-components'
import { AddSquareRegular, SendRegular } from '@fluentui/react-icons'
import React, { useContext, useState } from 'react'
import { ChatFeatureContextHandler } from '../../../../states/ChatContext'
import './ChatInputBox.css'

const useStyles = makeStyles({
  button: {
    marginRight: '5px',
  },
})

export function ChatInputBox() {
  const styles = useStyles()
  const contextHandler = useContext(ChatFeatureContextHandler)
  const [message, setMessage] = useState('')

  function onMessageChanged(_ev: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>, data?: InputOnChangeData) {
    setMessage(data?.value || '')
  }

  function onEnterPress(event: React.KeyboardEvent<Element>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      onSendMessageClicked()
      event.preventDefault()
    }
  }

  function onSendMessageClicked() {
    if (message) {
      contextHandler.onSendMessage(message)
      setMessage('')
    }
  }

  return (
    <>
      <Textarea value={message} size='large' className='input-message' onChange={onMessageChanged} onKeyDown={onEnterPress} />
      <div className='input-toolbar'>
        <div>
          <Button
            icon={<AddSquareRegular />}
            size='large'
            className={styles.button}
            onClick={contextHandler.onRestartConversation}
          >
            New topic
          </Button>
          {/* <Button icon={<AttachRegular />} size='large'>
                        Upload document
                    </Button> */}
        </div>
        <Button
          icon={<SendRegular />}
          appearance='primary'
          size='large'
          onClick={onSendMessageClicked}
          disabled={!message}
        ></Button>
      </div>
    </>
  )
}
