import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/todos' },
    { path: '/login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
    { path: '/register', component: () => import('../views/RegisterView.vue'), meta: { public: true } },
    { path: '/todos', component: () => import('../views/TodoListView.vue') },
    { path: '/todos/:id', component: () => import('../views/TodoDetailView.vue') },
    { path: '/categories', component: () => import('../views/CategoryListView.vue') },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isAuthenticated) {
    return '/login'
  }
  if (to.meta.public && auth.isAuthenticated) {
    return '/todos'
  }
})

export default router
