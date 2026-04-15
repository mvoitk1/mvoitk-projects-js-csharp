import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
    },
    {
      path: '/products',
      name: 'products',
      component: () => import('@/views/ProductsView.vue'),
    },
    {
      path: '/products/:id',
      name: 'product-detail',
      component: () => import('@/views/ProductDetailView.vue'),
    },
    {
      path: '/cart',
      name: 'cart',
      component: () => import('@/views/CartView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/checkout',
      name: 'checkout',
      component: () => import('@/views/CheckoutView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/orders',
      name: 'orders',
      component: () => import('@/views/OrdersView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/orders/:id',
      name: 'order-detail',
      component: () => import('@/views/OrderDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/RegisterView.vue'),
    },
    {
      path: '/admin',
      component: () => import('@/views/admin/AdminLayout.vue'),
      meta: { requiresAuth: true, requiresAdmin: true },
      children: [
        {
          path: '',
          redirect: { name: 'admin-products' },
        },
        {
          path: 'categories',
          name: 'admin-categories',
          component: () => import('@/views/admin/AdminCategoriesView.vue'),
        },
        {
          path: 'collections',
          name: 'admin-collections',
          component: () => import('@/views/admin/AdminCollectionsView.vue'),
        },
        {
          path: 'products',
          name: 'admin-products',
          component: () => import('@/views/admin/AdminProductsView.vue'),
        },
        {
          path: 'products/:id',
          name: 'admin-product-detail',
          component: () => import('@/views/admin/AdminProductDetailView.vue'),
        },
        {
          path: 'orders',
          name: 'admin-orders',
          component: () => import('@/views/admin/AdminOrdersView.vue'),
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth && !auth.isLoggedIn) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return { name: 'home' }
  }
})

export default router
