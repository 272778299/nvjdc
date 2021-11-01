<template>
 <el-row :span="24" v-show="isSHowAnnouncement" >
      <el-col :span="24" >
        <el-card  >
           <el-tag  type="warning" style="font-size: var(--el-font-size-large)" ><i class="el-icon-chat-round"></i> 公告</el-tag>
           <p><span style="font-size: var(--el-font-size-small)" v-html="announcement"></span></p>
        </el-card>
       </el-col>
 </el-row>
<el-row :span="24">
      <el-col :span="24">
        <el-card >
        <template #header>
          <div class="card-header">
            <span>手机登陆</span>
            <el-tag type="success">服务器余量：{{ ckcount }}</el-tag>
          </div>
        </template>
          <div class="card-body">
              <el-row class="demo-autocomplete">
                <el-col >
                     <el-tag type="success">:资源余量：{{ tabcount }}</el-tag>
                     <el-tag>当前占用:{{ marginCount }}</el-tag>
                     <el-tag type="danger">{{ times }}秒后释放资源</el-tag>
                </el-col>
                </el-row>
               <el-row class="demo-autocomplete">
                <el-col >
                    <span class="elabe">你的手机号码</span>
                </el-col>
                </el-row>
               <el-row class="demo-autocomplete">
                <el-col  >
                <el-input style="max-width:260px"
                    v-model="phone"
                    placeholder="Phone"
                    prefix-icon="el-icon-mobile-phone"
                />
                </el-col>
                </el-row>
                <el-row class="demo-autocomplete">
                <el-col >
                      <span class="elabe">验证码</span>
                </el-col>
                </el-row>
                 <el-row class="demo-autocomplete">
                <el-col > 
                <el-input
                        v-model="code" style="max-width:150px"
                        placeholder="Code"
                        prefix-icon="el-icon-lock"
                    />
                      <el-button type="success" v-show="isShow" @click="GetSMSCode" plain style="width:110px">获取验证码</el-button>
                      <el-button type="success" v-show="!isShow"   plain style="width:110px" disabled>{{Codetime}}秒后重发</el-button>
                </el-col>
            </el-row>
             <el-row class="demo-autocomplete">
                <el-col >
                      <span class="elabel">服务器</span>
                </el-col>
                </el-row>
                 <el-row class="demo-autocomplete">
                <el-col > 
                <el-select v-model="optionsvlue" placeholder="Select" style="width:100%;max-width:260px"  @change="valuechange">
                  <el-option
                    v-for="item in options"
                    :key="item.value"
                    :label="item.label"
                    :value="item.value"
                  >
                  <span style="float: left">{{ item.label }}</span>
                  <span
                    style="
                      float: right;
                      color: var(--el-text-color-secondary);
                      font-size: 13px;
                    "
                    >{{ item.count }}</span >
                  </el-option>
                </el-select>
                </el-col>
            </el-row>
            <el-button type="primary"    @click="Login">登录 </el-button>
          </div>
      </el-card>
     </el-col>
  </el-row>
  
</template>
<style>
   .elabe{
     font-size: var(--el-font-size-base)
   }
    .demo-autocomplete{
         margin-bottom: 10px;
   }
   .esuccess{
    color:  var(--el-color-success)  !important
   }
</style>
<script>
import {  onMounted,reactive, toRefs } from 'vue'
import { useRouter } from 'vue-router'
import { ElLoading } from 'element-plus'
import { ElMessage,ElNotification } from 'element-plus'
import { getConfigMain ,getQLConfig,SendSMS,VerifyCode,AutoCaptcha} from '@/api/index'
export default {

setup() {
   
    const router = useRouter()
    let data = reactive({
      marginCount: 0,
      ckcount:0,
      tabcount:0,
      allowAdd: true,
      First:true,
      isShow:true,
      code: '',
      times:0,
      Codetime:60,
      isSHowAnnouncement:false,
      announcement:"",
      centerDialogVisible:true,
      getCode:false,
      options: [],
      optionsvlue:"",
      phone:'',
      oldphone:'',
      form:{
          code: '',
         phone:'',
      }
    })
    const getmainConfig = async () => {
      const cphone = localStorage.getItem('phone')
      if(cphone){
        data.phone=cphone;
        localStorage.removeItem('phone')
      }
       const configdata = await getConfigMain();
       console.log(configdata)
       data.announcement=configdata.data.announcement
      if(configdata.data.announcement!=""){
      data.isSHowAnnouncement=true;
    }
     ElNotification.success({
        title: '提示',
        message: "获取验证码后3分钟后会释放资源,请在三分钟内完成登录",
        iconClass:"esuccess"
      })
        if(!configdata.success){
            ElMessage.error(configdata.message);
            return;
        }

        data.ckcount=configdata.data.ckcount;
        data.tabcount=configdata.data.tabcount;
        configdata.data.list.forEach(element => {
        
         if(data.optionsvlue=="")data.optionsvlue=element.qLkey;
          data.options.push({value:element.qLkey,label: element.qlName,count:"容量:"+element.qL_CAPACITY});
        });
    }
     const valuechange = async(value) => {
        const configdata =await  getQLConfig(value);
        data.ckcount=configdata.data.ckcount;
        data.tabcount=configdata.data.tabcount;
          if(!configdata.success){
            ElMessage.error(configdata.message);
            return;
        }
        ElMessage.success("切换成功")
    }
    onMounted(getmainConfig)
    
    const Login =async () => {
      console.log(data.getCode)
       if(!data.code)  ElMessage.error("请先输入验证码");
         if(!data.phone)  ElMessage.error("请先输入手机");
      if(!data.getCode)  ElMessage.error("请先获取验证码");
      const loading = ElLoading.service({
        lock: true,
        text: '正在登陆',
      })
      const body = await VerifyCode({ Phone: data.phone, qlkey: data.optionsvlue ,Code:data.code})
       loading.close()
      if(!body.success)
      {
           ElMessage.error(body.message);
      }else{
         ElNotification.success({
        title: '提示',
        message: "登录成功",
        iconClass:"esuccess"
      })
        console.log(body);
         localStorage.setItem("qlid",body.data.qlid);
         localStorage.setItem("qlkey",body.data.qlkey)
         router.push('/');
      }
     
    
    }
    const  sleep=(ms)=> {
      return new Promise(resolve => setTimeout(resolve, ms))
    }
    const GetSMSCode = async() => {

     // console.log(data.optionsvlue)  
     
      if(data.phone==""){
        ElMessage.error('请输入手机号码')
        return false;
      }
      var  re = /^1\d{10}$/ ;
    //   var reg = new RegExp(/^(13[0-9]|14[01456879]|15[0-35-9]|16[2567]|17[0-8]|18[0-9]|19[0-35-9])\d{8}$/)
        var res = re.test(data.phone);
        if (!res) {
              ElMessage.error("手机号错误");
            return false;
      }
      if(data.oldphone!=""&&data.phone!=data.oldphone){
        
        data.First=true;
      }
      if(data.oldphone!=""&&data.phone==data.oldphone){
        data.First=false;
      }
     
       if(data.First){
         const loading = ElLoading.service({
        lock: true,
        text: '正在获取验证码',
       })
       
        data.marginCount=1;
         data.times=180;
         data.Codetime=60
         data.oldphone=data.phone;
         data.First=false;
        
         const body = await SendSMS({ Phone: data.phone, qlkey: data.optionsvlue })
         console.log(body)
         if(body.success==false&&body.data.status==666){
           loading.setText("正在破解安全验证");
           console.log("安全验证")
           var AutoCaptchadata=null;
           for (let index = 0; index <5; index++) {
             let count=index+1;
              loading.setText("第"+count+"次破解安全验证");
             AutoCaptchadata=  await AutoCaptcha({ Phone: data.phone })

             if(AutoCaptchadata.success) break;
              if(!AutoCaptchadata.success&&AutoCaptchadata.data.status!=666){
                    break;
              } 
           }
           if(AutoCaptchadata.success)
              body.success=true;
           else{
             loading.close()
             if(AutoCaptchadata.data.status==666){
               ElMessage.error("安全认证破解失败，请联系Nolan");
             }else{
               ElMessage.error(AutoCaptchadata.message);
             }
             return false;
           }
         }
         loading.close()
         if(body.success==false&&body.data.status!=666){
            ElMessage.error(body.message);
            return false;
         }
          data.tabcount=body.data.tabcount;
          data.ckcount=body.data.ckcount;
            let timer=setInterval(function () {
                  data.times--;
                  if(data.times<=0){
                       clearInterval(timer);
                      
                  }
                 
               },1000)
       }else{
          localStorage.setItem("phone",data.phone);
         router.go(0);
       }
        data.isShow=false;
       
          let timer2=setInterval(function () {
                  data.Codetime--;
                  if(data.Codetime<=0){
                       clearInterval(timer2);
                       data.isShow=true
                  }
                 
               },1000)
      data.getCode=true;
      
     
    }
    return {
      ...toRefs(data),
      Login,
      GetSMSCode,
      valuechange,
    }
  }
}
</script>