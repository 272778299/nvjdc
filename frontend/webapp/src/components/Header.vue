<template>
  <div class="header " style="box-shadow: var(--el-box-shadow-base); margin-bottom: 20px;">
    <div class="header-wrapper">
      <div class="flex items-center "  style="text-align: left;font-size:20px; line-height: 3;">
        <Logo class="log" />
        <span  style="margin-left: 10px;">{{title}}</span>
      </div>
    </div>
  </div>
</template>

<script>
import Logo from '@/components/Logo.vue'
export default {
 name: 'header',
  props: {
    title: String
  },
  components: {
    Logo,
  },
}
</script>

<style scoped>
.log{
   
    margin-left: 50px;
    width: 25px;
    height: 25px;
}
</style>
