<template>
  <el-row :span="24">
      <el-col :span="24">
        <el-card >
        <template #header>
          <div class="card-header">
            <span>个人中心</span>
          </div>
        </template>
          <div class="card-body">
            <p>昵称：{{ nickname }}</p>
            <p>更新时间：{{ timestamp }}</p>
          </div>
          <div class="card-footer">
            <el-button size="small" auto @click="logout">退出登录</el-button>
            <el-button type="danger" size="small" auto @click="delAccount"
              >删除账号</el-button>
          </div>
      </el-card>
     </el-col>
  </el-row>
  <el-row :span="24">
      <el-col :span="24">
        <el-card >
        <template #header>
          <div class="card-header">
            <span>修改备注</span>
          </div>
        </template>
          <div class="card-body">
             <el-input style="max-width:260px"
                    v-model="remarks"
                    placeholder="备注"
                    prefix-icon="el-icon-edit"
                />
          </div>
          <div class="card-footer">
            <el-button type="success" size="small" auto @click="upAccount"
              >修改</el-button>
          </div>
      </el-card>
     </el-col>
  </el-row>
   <el-row v-show="qrshow" :span="24">
      <el-col :span="24">
        <el-card >
        <template #header>
          <div class="card-header">
            <span>消息推送二维码</span>
          </div>
        </template>
          <div class="card-body">
            <el-image style="width:200px" :src="qrurl"></el-image>
          </div>
      </el-card>
     </el-col>
  </el-row>
  <el-row :span="24">
      <el-col :span="24">
        <el-card >
        <template #header>
          <div class="card-header">
            <span>常见活动位置</span>
          </div>
        </template>
          <div class="card-body">
           <ul class="ulc">
              <li
                v-for="(item, index) in activity"
                :key="index"
                class="leading-normal"
              >
                <span>{{ item.name }}：</span>
                <span class="pr-2">{{ item.address }}</span>
                <a
                  v-if="item.href"
                  class="text-blue-400"
                  href="#"
                  @click="openUrlWithJD(item.href)"
                  >直达链接</a
                >
              </li>
            </ul>
          </div>
          
      </el-card>
     </el-col>
  </el-row>
  <el-row>
    <el-col :span="8"><div class="grid-content bg-purple"></div></el-col>
    <el-col :span="8"><div class="grid-content bg-purple-light"></div></el-col>
    <el-col :span="8"><div class="grid-content bg-purple"></div></el-col>
  </el-row>
  <el-row>
    <el-col :span="6"><div class="grid-content bg-purple"></div></el-col>
    <el-col :span="6"><div class="grid-content bg-purple-light"></div></el-col>
    <el-col :span="6"><div class="grid-content bg-purple"></div></el-col>
    <el-col :span="6"><div class="grid-content bg-purple-light"></div></el-col>
  </el-row>
  <el-row>
    <el-col :span="4"><div class="grid-content bg-purple"></div></el-col>
    <el-col :span="4"><div class="grid-content bg-purple-light"></div></el-col>
    <el-col :span="4"><div class="grid-content bg-purple"></div></el-col>
    <el-col :span="4"><div class="grid-content bg-purple-light"></div></el-col>
    <el-col :span="4"><div class="grid-content bg-purple"></div></el-col>
    <el-col :span="4"><div class="grid-content bg-purple-light"></div></el-col>
  </el-row>
</template>

<style>
.text-blue-400{
  color: blue;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.text {
  font-size: 14px;
}

.item {
  margin-bottom: 18px;
}

.box-card {
  width: 480px;
}
.el-row {
  margin-bottom: 20px;
  
}
.el-col {
  border-radius: 4px;
}
.bg-purple-dark {
  background: #99a9bf;
}
.bg-purple {
  background: #d3dce6;
}
.bg-purple-light {
  background: #e5e9f2;
}
.grid-content {
  border-radius: 4px;
  min-height: 36px;
}
.row-bg {
  padding: 10px 0;
  background-color: #f9fafc;
}
.leading-normal{
   list-style: none; 
   margin-top: 15px
}
.ulc{
      list-style: none;
    padding-inline-start: 0;
}
</style>
<script>
import { onMounted, reactive, toRefs } from 'vue'
import { useRouter } from 'vue-router'
import { getUserInfoAPI,Upremarksapi,delAccountAPI} from '@/api/index'
import { ElMessage } from 'element-plus'
export default {
 setup() {
   const router = useRouter()

    let data = reactive({
      nickname: undefined,
      timestamp: undefined,
      remarks:"",
      qrurl:"",
      qrshow:false,
    })
    const getInfo = async () => {
      const qlid = localStorage.getItem('qlid')
      const qlkey = localStorage.getItem('qlkey')
      if (!qlid) {
        logout()
        return
      }
     const userInfo = await getUserInfoAPI(qlid,qlkey);
       if (!userInfo||!userInfo.success) {
          ElMessage.error('获取用户信息失败，请重重新登录')
          logout()
         return
       }
       localStorage.setItem("qlid",userInfo.data.qlid);
        localStorage.setItem("qlky",userInfo.data.qlky);
       data.nickname = userInfo.data.nickname;
       data.qrurl=  userInfo.data.qrurl;
      if(data.qrurl!="") data.qrshow=true
       data.timestamp = new Date(userInfo.data.timestamp).toLocaleString();
       data.remarks=userInfo.data.remarks;
    }

    onMounted(getInfo)

    const logout = () => {
     console.log("我没找到");
      localStorage.removeItem('qlid')
      localStorage.removeItem('qlky')
      router.push('/login')
    }
  const upAccount = async () => {
      const qlid = localStorage.getItem('qlid')
      const qlkey = localStorage.getItem('qlkey')
      const body = await Upremarksapi({ qlid: qlid, qlkey:qlkey ,remarks:data.remarks})
      if(body.success){
        ElMessage.success(body.message)
      }else{
        ElMessage.error(body.message)
      }
    }
    const delAccount = async () => {
       const qlid = localStorage.getItem('qlid')
      const qlkey = localStorage.getItem('qlkey')
      const body = await delAccountAPI({ qlid: qlid, qlkey:qlkey })
      if (!body.success) {
        ElMessage.error(body.message)
      } else {
        ElMessage.success(body.message)
        setTimeout(() => {
          logout()
        }, 1000)
      }
    }

    const openUrlWithJD = (url) => {
      const params = encodeURIComponent(
        `{"category":"jump","des":"m","action":"to","url":"${url}"}`
      )
      window.location.href = `openapp.jdmobile://virtual?params=${params}`
      console.log(window.location.href)
    }

    const activity = [
      {
        name: '玩一玩（可找到大多数活动）',
        address: '京东 APP 首页-频道-边玩边赚',
        href: 'https://funearth.m.jd.com/babelDiy/Zeus/3BB1rymVZUo4XmicATEUSDUgHZND/index.html',
      },
      {
        name: '宠汪汪',
        address: '京东APP-首页/玩一玩/我的-宠汪汪',
      },
      {
        name: '东东萌宠',
        address: '京东APP-首页/玩一玩/我的-东东萌宠',
      },
      {
        name: '东东农场',
        address: '京东APP-首页/玩一玩/我的-东东农场',
      },
      {
        name: '东东工厂',
        address: '京东APP-首页/玩一玩/我的-东东工厂',
      },
      {
        name: '东东超市',
        address: '京东APP-首页/玩一玩/我的-东东超市',
      },
      {
        name: '领现金',
        address: '京东APP-首页/玩一玩/我的-领现金',
      },
      {
        name: '东东健康社区',
        address: '京东APP-首页/玩一玩/我的-东东健康社区',
      },
      {
        name: '京喜农场',
        address: '京喜APP-我的-京喜农场',
      },
      {
        name: '京喜牧场',
        address: '京喜APP-我的-京喜牧场',
      },
      {
        name: '京喜工厂',
        address: '京喜APP-我的-京喜工厂',
      },
      {
        name: '京喜财富岛',
        address: '京喜APP-我的-京喜财富岛',
      },
      {
        name: '京东极速版红包',
        address: '京东极速版APP-我的-红包',
      },
    ]

    return {
      ...toRefs(data),
      activity,
      getInfo,
      logout,
      delAccount,
      upAccount,
      openUrlWithJD
    }
  },
}
  
</script>

