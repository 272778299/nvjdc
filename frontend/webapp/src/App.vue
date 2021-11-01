<template>

     <el-container>
      <el-header class="el-header" style="--el-header-padding:0px;z-index: 1999; " ><Header  :title="title" /></el-header>
      <el-main class="el-main"><router-view /></el-main>
    </el-container>
   

 
</template>
<script>
import {  onMounted,reactive, toRefs } from 'vue'
import Header from '@/components/Header.vue'
import { getQLTitle} from '@/api/index'
export default {
  name: 'App',
  components: {
    Header
  },setup() {

     let data = reactive({
       title:"NolanJDCloud1",
     })
     const getmainConfigtitle = async () => {
    
       const configdata = await getQLTitle();
       console.log(configdata)
       data.title=configdata.data.title
    }
     onMounted(getmainConfigtitle)
  return {
      ...toRefs(data)
    }
  }
}
</script>

<style>
body{
  margin: 0px;
}
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
}
.el-header {
       
        position: relative;
        width: 100%;
        height: 60px;  
}
.el-main {
  position: absolute;
  left: 0;;
  right: 0;
  top: 60px;
  bottom: 0;
  overflow-y: scroll;
  }
</style>
