import { IPublicClientApplication } from '@azure/msal-browser'
import { useMsal } from '@azure/msal-react'
import { CompoundButton } from '@fluentui/react-components'
import { GuestRegular } from '@fluentui/react-icons'
import { loginRequest } from '../../security/authConfig'
import './Login.css'

export function Login() {
  const { instance } = useMsal()

  const handleLogin = (instance: IPublicClientApplication) => {
    instance.loginRedirect(loginRequest).catch((e) => {
      console.error(e)
    })
  }

  return (
    <div className='login-container'>
      <CompoundButton size='large' appearance='primary' icon={<GuestRegular />} onClick={() => handleLogin(instance)}>
        Please click here to sign in
      </CompoundButton>
    </div>
  )
}
