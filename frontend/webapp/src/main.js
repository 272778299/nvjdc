import { createApp } from 'vue'
import App from './App.vue'
import installElementPlus from './plugins/element'
import zhCn from 'element-plus/es/locale/lang/zh-cn'
import router from './router'
import "./styles/index.scss"

const app = createApp(App).use(router).use(router)
installElementPlus(app,{
    locale: zhCn,
  })
app.mount('#app')
router.beforeEach((to, from, next)=>{
  if(to.meta.title){
    document.title = to.meta.title
  }
  next()
})