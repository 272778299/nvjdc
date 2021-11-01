using IServer.IPageServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NETJDC.Extensions;
using NETJDC.Request;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Systems;

namespace NETJDC.Controllers
{
    [Route("api/")]
    [ApiController, EnableCors("CorsPolicy")]
    public class CloudController : ControllerBase
    {

        private readonly IPageServer _PageServer;
        private readonly MainConfig _mainConfig;
        public CloudController(IPageServer helper, MainConfig mainConfig)
        {
            _PageServer = helper;
            _mainConfig = mainConfig;
        }
        [HttpGet, Route("Title")]
        public IActionResult Title()
        {
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            var Title = _mainConfig.Title;
            if (string.IsNullOrEmpty(_mainConfig.Title)) Title = "NolanJDCloud";
            result.data = new { title = Title };
            return Ok(result);
        }
        [HttpGet, Route("Config")]
        public async Task<IActionResult> Config()
        {
          
            if(_mainConfig.Config.Count==0) throw new Exception("没有配置青龙服务器,检查配置");
            var list = _mainConfig.Config.Select(x => new { x.QLkey, x.QLName, x.QL_CAPACITY }).ToList();
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            var config = _mainConfig.Config.First();
            var qlcount = await config.GetEnvsCount();
            var ckcount = config.QL_CAPACITY-qlcount;
            if (ckcount < 0) ckcount = 0;
            string MaxTab = _mainConfig.MaxTab;
            string Announcement = _mainConfig.Announcement;
            var intabcount= _PageServer.GetPageCount();
            int tabcount = int.Parse(MaxTab) - intabcount;
            result.data = new { list=list, ckcount= ckcount , tabcount = tabcount , announcement = Announcement };
            return Ok(result);
        }
        [HttpGet, Route("QLConfig")]
        public async Task<IActionResult> QLConfig(int qlkey)
        {
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            if (qlkey==0) throw new Exception("请选择配置");
            var config = _mainConfig.GetConfig(qlkey);
            var qlcount = await config.GetEnvsCount();
            var ckcount = config.QL_CAPACITY - qlcount;
            string MaxTab = _mainConfig.MaxTab;
            var intabcount = _PageServer.GetPageCount();
            int tabcount = int.Parse(MaxTab) - intabcount;
            if (tabcount < 0) tabcount = 0;
            if (ckcount < 0) ckcount = 0;
            result.data = new { ckcount = ckcount, tabcount =tabcount };
            return Ok(result);
        }
     
        [HttpPost, Route("AutoCaptcha")]
        public async Task<IActionResult> AutoCaptcha(RequestEntity obj)
        {
            string Phone = obj.Phone;
            if (string.IsNullOrEmpty(Phone)) throw new Exception("请输入手机号码");
            if (!CheckPhoneIsAble(Phone)) throw new Exception("请输入正确的手机号码");
            ResultModel<object> result = await _PageServer.AutoCaptcha(Phone);
            return Ok(result);
        }
        [HttpPost, Route("SendSMS")]
        public async Task<IActionResult> SendSMS(RequestEntity obj)
        {

            string Phone = obj.Phone;
            int qlkey =  obj.qlkey;
            ResultModel<object> result = ResultModel<object>.Create(true, "");
           
           
            try
            {
               
                if (string.IsNullOrEmpty(Phone)) throw new Exception("请输入手机号码");
                if (qlkey==0) throw new Exception("请选择服务器");
              
                if (!CheckPhoneIsAble(Phone)) throw new Exception("请输入正确的手机号码");
                await _PageServer.PageClose(Phone);
                result = await _PageServer.OpenJDTab(qlkey, Phone);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (!string.IsNullOrEmpty(Phone))
                {
                    await _PageServer.PageClose(Phone);
                }
                  
                result.data = new { Status = 404 };
                result.message = e.Message;
                result.success = false;
            }
            return Ok(result);
        }
        [HttpGet, Route("User")]
        public async Task<IActionResult> Userd(string qlid,int qlkey)
        {
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            try
            {
                if (string.IsNullOrEmpty(qlid)) throw new Exception("Id为空");
                if (qlkey == 0) throw new Exception("服务器为空");
                var config = _mainConfig.GetConfig(qlkey);
                var env = await config.GetEnvbyid(qlid);
                if (env == null) throw new Exception("未找到相应的账号请检查");
                var timestamp = env["timestamp"].ToString();
                var remarks = env["remarks"].ToString();
                var nickname = await GetNickname(env["value"].ToString());
                result.data = new { qlid = qlid, qlkey = qlkey, timestamp = timestamp, remarks = remarks , nickname=nickname,qrurl= config.QRurl};
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result.message = e.Message;
                result.success = false;
            }
            return Ok(result);
        }
        private async Task<string> GetNickname(string cookie)
        {
            try
            {
                TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);
                var url = @"https://me-api.jd.com/user_new/info/GetJDUserInfoUnion?orgFlag=JD_PinGou_New&callSource=mainorder&channel=4&isHomewhite=0&sceneval=2&_=" + (long)ts.TotalSeconds + "&sceneval=2&g_login_type=1&g_ty=ls";
                using (HttpClient client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Cookie", cookie);
                    client.DefaultRequestHeaders.Add("Referer", "https://home.m.jd.com/myJd/newhome.action");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Host", "me-api.jd.com");
                    var result = await client.GetAsync(url);
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(resultContent);
                    JObject j = JObject.Parse(resultContent);
                    // data?.userInfo.baseInfo.nickname
                    return j["data"]["userInfo"]["baseInfo"]["nickname"].ToString();
                }
            }
            catch
            {
                return "未知";
            }
            

        }
        [HttpPost, Route("Upremarks")]
        public async Task<IActionResult> Upremarks(Requestremarks obj)
        {
            string qlid = obj.qlid;
            int qlkey = obj.qlkey;
            string remarks = obj.remarks;
            ResultModel<JObject> result = ResultModel<JObject>.Create(true, "");

            try
            {
                if (string.IsNullOrEmpty(remarks)) throw new Exception("Id为空");
                if (string.IsNullOrEmpty(qlid)) throw new Exception("Id为空");
                if (qlkey == 0) throw new Exception("请选择服务器");
                var config = _mainConfig.GetConfig(qlkey);
                var env =await config.GetEnvbyid(qlid);
                if (env == null) throw new Exception("未找到相应的账号请检查");
               var upresult = await config.UpdateEnv(env["value"].ToString(),qlid,remarks);
                var  timestamp = upresult.data["timestamp"].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                result.message = e.Message;
                result.success = false;
            }
            return Ok(result);
        }
        [HttpPost, Route("del")]
        public async Task<IActionResult> del(RequestDEL obj)
        {
            string qlid = obj.qlid;
            int qlkey = obj.qlkey;
            ResultModel<JObject> result = ResultModel<JObject>.Create(true, "");

            try
            {

                if (string.IsNullOrEmpty(qlid)) throw new Exception("Id为空");
                if (qlkey == 0) throw new Exception("请选择服务器");
                var config = _mainConfig.GetConfig(qlkey);
                result = await config.DelEnv(qlid);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
              
                result.message = e.Message;
                result.success = false;
            }
            return Ok(result);
        }
        [HttpPost, Route("VerifyCode")]
        public async Task<IActionResult> VerifyCode(RequestEntity obj)
        {

            string Phone = obj.Phone;
            int qlkey = obj.qlkey;
            string Code = obj.Code;
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            if(string.IsNullOrEmpty(Phone)) throw new Exception("请输入手机号码");
            if (!CheckPhoneIsAble(Phone)) throw new Exception("请输入正确的手机号码");
            if (string.IsNullOrEmpty(Code)) throw new Exception("请输入验证码");
            if (qlkey==0) throw new Exception("请选择服务器");
            try
            {
                 result = await _PageServer.VerifyCode(qlkey,Phone, Code);
               
            }catch( Exception e)
            {
                if (!string.IsNullOrEmpty(Phone))
                {
                   await _PageServer.PageClose(Phone);;
                }
                result.data = new { Status = 404 };
                result.message = e.Message;
                result.success = false;
            }

            return Ok(result);
        }
        public  bool CheckPhoneIsAble(string input)
        {
            if (input.Length < 11)
            {
                return false;
            }
            Regex regex = new Regex("^1\\d{10}$");
            return regex.IsMatch(input);
        }
       
    }
}
