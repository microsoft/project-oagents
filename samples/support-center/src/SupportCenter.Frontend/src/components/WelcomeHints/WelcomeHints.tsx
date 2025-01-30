import { Body1, Button, Card, CardFooter, CardHeader, Subtitle1, Title1, makeStyles, shorthands, tokens } from '@fluentui/react-components'
import { ArrowReply16Regular, ChatBubblesQuestion24Regular } from '@fluentui/react-icons'
import { useContext } from 'react'
import { AppFeatureContext } from '../../states/AppContext'
import { ChatFeatureContextHandler } from '../../states/ChatContext'
import './WelcomeHints.css'

const useStyles = makeStyles({
  title: {
    ...shorthands.margin('15px', '0px', '30px', '0px'),
  },
  card: {
    ...shorthands.margin('0px', '30px'),
    minWidth: '300px',
    maxWidth: '300px',
  },
  button: {
    backgroundColor: tokens.colorBrandBackground
  },
  warning: {
    ...shorthands.margin('0px', '30px'),
    minWidth: '0px',
    maxWidth: '500px',
    maxHeight: '200px',
    backgroundColor: '#f2f2f2',
    fontStyle: 'italic'
  },
})

export function WelcomeHints() {
  const styles = useStyles()
  const appContext = useContext(AppFeatureContext)
  const contextHandler = useContext(ChatFeatureContextHandler)

  return (
    <div className='welcome-container'>
      <Title1 className={styles.title} align='center'>
        {appContext.configuration?.chatConfiguration?.welcomeTitle}
      </Title1>
      <Subtitle1 className={styles.title} align='center'>
        {appContext.configuration?.chatConfiguration?.welcomeSubtitle}
      </Subtitle1>
      <div className='hints-container'>
        {appContext.configuration?.chatConfiguration?.welcomeHints?.map((hint) => (
          <Card key={hint} className={styles.card}>
            <CardHeader description={<ChatBubblesQuestion24Regular />} />
            <Body1>{hint}</Body1>
            <CardFooter>
              <Button icon={<ArrowReply16Regular />} className={styles.button} appearance='primary' onClick={() => contextHandler.onSendMessage(hint)}>
                Ask
              </Button>
            </CardFooter>
          </Card>
        ))}
      </div>
      {appContext.configuration?.chatConfiguration?.welcomeWarning ? (
          <div className='warning-container'>
            <Card className={styles.warning}>
              <Body1>{appContext.configuration?.chatConfiguration?.welcomeWarning}</Body1>
            </Card>
          </div>
        ) : <></>
      }
    </div>
  )
}
