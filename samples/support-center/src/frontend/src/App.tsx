import { Tab, TabList } from '@fluentui/react-components'
import { Settings } from 'luxon'
import { Route, Routes, createBrowserRouter, useNavigate } from 'react-router-dom'
import './App.css'
import { NavBar } from './components/NavBar/NavBar'
import { ChatPage } from './pages/chat/ChatPage'

export default function App() {
  Settings.defaultLocale = 'en-UK'

  createBrowserRouter([
    {
      path: '/',
      element: <ChatPage />,
    },
    {
      path: '/chat',
      element: <ChatPage />,
    }
  ])

  const navigate = useNavigate()

  return (
    <div className='body'>
        <NavBar />
        <div className='app-header'>
          <TabList>
            <Tab value='tab1' onClick={() => navigate('chat')}>
              🗨️ Chat
            </Tab>
          </TabList>
        </div>

        <Routes>
          <Route path='/' element={<ChatPage />} />
          <Route path='/chat' element={<ChatPage />} />
        </Routes>
    </div>
  )
}
