import { createRouter, createWebHistory } from 'vue-router'
import Index from '@/views/Index.vue'
import Login from '@/views/Logn.vue'
const routes = [
  { path: '/', component: Index , meta: { hidden: true,title:"NolanJDCloud"}},
  { path: '/login', component: Login,meta: { hidden: true,title:"NolanJDCloud"} },
]


const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes
})

export default router
