import { createApp } from 'vue'
import './style.css'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

// Attempt silent refresh on init if refreshToken exists but jwt is missing
const auth = useAuthStore()
if (auth.refreshToken && !auth.jwt) {
  auth.refreshTokens()
}

app.mount('#app')
