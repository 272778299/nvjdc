import ElementPlus from 'element-plus'
import 'element-plus/lib/theme-chalk/index.css'
import locale from 'element-plus/lib/locale/lang/zh-cn'
import * as ElIconModules from '@element-plus/icons'

export default (app) => {
  app.use(ElementPlus, { locale })
  for(let iconName in ElIconModules){
    let newName="e-"+iconName;
    app.component(newName,ElIconModules[iconName])
}
}
