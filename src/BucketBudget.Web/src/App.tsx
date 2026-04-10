import { useState } from 'react'
import { isLoggedIn } from '@/lib/auth'
import { LoginPage } from '@/pages/LoginPage'
import { AccountRegisterPage } from '@/pages/AccountRegisterPage'

function App() {
  const [loggedIn, setLoggedIn] = useState(isLoggedIn)

  if (!loggedIn) {
    return <LoginPage onLogin={() => setLoggedIn(true)} />
  }

  return <AccountRegisterPage onLogout={() => setLoggedIn(false)} />
}

export default App
