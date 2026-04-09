import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/dashboard' },
    { path: '/login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
    { path: '/register', component: () => import('../views/RegisterView.vue'), meta: { public: true } },
    { path: '/dashboard', component: () => import('../views/DashboardView.vue') },
    { path: '/todos', redirect: '/dashboard' },
    { path: '/tasks/new', component: () => import('../views/CreateTaskView.vue') },
    { path: '/todos/:id', component: () => import('../views/TodoDetailView.vue') },
    { path: '/categories', component: () => import('../views/CategoryListView.vue') },
    { path: '/priorities', component: () => import('../views/PriorityListView.vue') },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isAuthenticated) {
    return '/login'
  }
  if (to.meta.public && auth.isAuthenticated) {
    return '/dashboard'
  }
})

export default router
