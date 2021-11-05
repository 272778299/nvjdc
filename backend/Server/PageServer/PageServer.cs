using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IServer;
using IServer.IPageServer;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using PuppeteerSharp.Mobile;
using Systems;

namespace Server.PageServer
{
    public class Jpage
    {
        public DateTime regdate { get; set; }

        public Page Page { get; set; }
    }
    public class PageServer : IPageServer
    {
        private MainConfig _mainConfig;
        private OpenCVServer.OpenCVServer cv;
        public PageServer(MainConfig mainConfig, OpenCVServer.OpenCVServer openCV)
        {
            _mainConfig = mainConfig;
            cv = openCV;
        }
        public static Dictionary<string, Page> pagelist = new Dictionary<string, Page>();
        static readonly object _locker = new object();
        public Page AddPage(string phone, Page page)
        {
            lock (_locker)
            {
                string MaxTab = _mainConfig.MaxTab;
                if (string.IsNullOrEmpty(MaxTab)) MaxTab = "4";
                if (!pagelist.ContainsKey(phone))
                {
                    if (pagelist.Count < int.Parse(MaxTab))
                    {

                        pagelist.Add(phone, page);
                        return page;
                    }

                }
                else
                    return pagelist[phone];


            }
            return null;
        }
        public void Delpage(string phone, Page page)
        {
            lock (_locker)
            {
                if (pagelist.ContainsKey(phone))
                {
                    pagelist.Remove(phone);
                }
            }
        }
        public Page GetPage(string Phone)
        {
            lock (_locker)
            {
                if (!pagelist.ContainsKey(Phone))
                {

                    return null;
                }
                else
                    return pagelist[Phone];
            }
        }
        public Page GetPage()
        {
            lock (_locker)
            {
                System.Threading.Thread.Sleep(500);
                if (pagelist.Count > 0)
                    return pagelist.First().Value;
                else
                    return null;
            }
        }
        public int GetPageCount()
        {
            lock (_locker)
            {
                System.Threading.Thread.Sleep(500);

                return pagelist.Count;

            }
        }
        public async Task<bool> BrowserInit()
        {
            var browserFetcher = new BrowserFetcher();
            var aa = await browserFetcher.DownloadAsync();
            try
            {
                var path = aa.ExecutablePath;
                Bash($"chmod 777 {path}");
            }
            catch (Exception e)
            {
                Console.WriteLine("执行 CHOMD 777 浏览器地址错位 可以忽略");
                // Console.WriteLine(e.ToString()) ;
            }
            return aa.Downloaded;
        }
        public void Bash(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        public async Task<ResultModel<object>> OpenJDTab(int qlkey, string Phone,bool UploadQL=true)
        {
            DateTime expdate = DateTime.Now;

            ResultModel<object> result = ResultModel<object>.Create(false, "");
            if (UploadQL)
            {
                var qlconfig = _mainConfig.GetConfig(qlkey);
                if (qlconfig == null)
                {
                    result.message = "未找到相应的服务器配置。请刷新页面后再试";
                    result.data = new { Status = 404 };
                    return result;
                }
            }
           
            Page page = GetPage();
            Browser browser = null;
            if (page == null)
            {

                var options = new LaunchOptions
                {
                    Args = new string[] { "--no-sandbox", "--disable-setuid-sandbox" },
                    Headless = true,
                    DefaultViewport = new ViewPortOptions
                    {
                        Width = 375,
                        Height = 667,
                        IsMobile = true
                    }

                };
                browser = await Puppeteer.LaunchAsync(options);
            }
            else
                browser = page.Browser;

            string MaxTab = _mainConfig.MaxTab;
            if (string.IsNullOrEmpty(MaxTab)) MaxTab = "4";
            var Tablist = await browser.PagesAsync();
            if (Tablist.Length > int.Parse(MaxTab) + 1)
            {
                result.message = MaxTab + "个网页资源已经用完。请稍候再试!";
                result.success = false;
                return result;
            }

            string Url = "https://plogin.m.jd.com/login/login?appid=300&returnurl=https%3A%2F%2Fwq.jd.com%2Fpassport%2FLoginRedirect%3Fstate%3D2087738584%26returnurl%3Dhttps%253A%252F%252Fhome.m.jd.com%252FmyJd%252Fnewhome.action%253Fsceneval%253D2%2526ufc%253D%2526&source=wq_passport";
            ///、 string Url = "https://bean.m.jd.com/bean/signIndex.action";
            var context = await browser.CreateIncognitoBrowserContextAsync();
            page = await context.NewPageAsync();
            ///屏蔽 WebDriver 检测
             //await page.EvaluateFunctionOnNewDocumentAsync("function(){Object.defineProperty(navigator, 'webdriver', {get: () => undefined})}");
            DeviceDescriptor deviceOptions = Puppeteer.Devices.GetValueOrDefault(DeviceDescriptorName.IPhone7);
            await page.EmulateAsync(deviceOptions);
            await page.GoToAsync(Url);
            await page.WaitForTimeoutAsync(200);
            var aa = await GetPhoneCode(Phone, page);
            //Console.WriteLine(aa);
            await page.WaitForTimeoutAsync(210);
            //await page.ClickAsync("button[report-eventid='MLoginRegister_SMSReceiveCode']");
            //await page.ClickAsync("button[report-eventid='MLoginRegister_SMSReceiveCode']");
            await page.ClickAsync("button[report-eventid='MLoginRegister_SMSReceiveCode']", new PuppeteerSharp.Input.ClickOptions { ClickCount = 3 });
            await page.ClickAsync("input[type=checkbox]");
            ///傻逼等待代码
            await WaitSendSms(page);
            string js = "document.body.outerText";
            var pageouterText = await page.EvaluateExpressionAsync(js);
            var pagetext = pageouterText.ToString();
            //Console.WriteLine(pagetext);
            var ckcount = 0;
            var tabcount = GetTableCount();
            if (_mainConfig.UPTYPE == UpTypeEum.ql)
            {
                var data = await getCount(qlkey);
                ckcount = data.ckcount;
            }
           
            System.Timers.Timer timer = new System.Timers.Timer(60000 * 3);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(async (s, e) =>
            {
                Console.WriteLine("    手机：" + Phone + " tabe 自动回收 时间:" + DateTime.Now.ToString());
                Delpage(Phone, page);
                await page.CloseAsync();
                var oldpage = GetPage();
                if (oldpage == null)
                {
                    await browser.CloseAsync();
                }
                timer.Dispose();
            });
            timer.AutoReset = false;

            if (pagetext.Contains("安全验证") && !pagetext.Contains("验证成功"))
            {
                Console.WriteLine("    手机：" + Phone + " tabe 创建 时间:" + DateTime.Now.ToString());
                timer.Start();
                Console.WriteLine(Phone + "安全验证");
                // await PageClose(Phone);
                result.data = new { Status = 666, ckcount = ckcount, tabcount = tabcount };
                result.message = "出现安全验证,";
                return result;
            }
            if (pagetext.Contains("短信已经发送，请勿重复提交"))
            {
                await PageClose(Phone);
                result.data = new { Status = 505, pagetext = pagetext };
                result.message = "请刷新页面重新登陆。";
                return result;
            }
            if (pagetext.Contains("短信验证码发送次数已达上限"))
            {
                await PageClose(Phone);
                result.data = new { Status = 505, pagetext = pagetext };
                result.message = "对不起，短信验证码发送次数已达上限，请24小时后再试。";
                return result;
            }
            if (pagetext.Contains("该手机号未注册，将为您直接注册。"))
            {
                await PageClose(Phone);

                result.data = new { Status = 501 };
                result.message = "该手机号未注册";
                return result;
            }
            Console.WriteLine("    手机：" + Phone + " tabe 创建 时间:" + DateTime.Now.ToString());

            timer.Start();
            if (pagetext.Contains("重新获取"))
            {
                result.success = true;
                Console.WriteLine(Phone + "获取验证码成功");
            }
            result.data = new { ckcount = ckcount, tabcount = tabcount };

            return result;
        }
        /// <summary>
        /// 网络问题所以要写这种傻逼等待带代码
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<bool> WaitSendSms(Page page)
        {
            await page.WaitForTimeoutAsync(1000);
            string js = "document.body.outerText";
            var pageouterText = await page.EvaluateExpressionAsync(js);
            var pagetext = pageouterText.ToString();
            if (pagetext.Contains("安全验证") || pagetext.Contains("短信已经发送，请勿重复提交") || pagetext.Contains("短信验证码发送次数已达上限") || pagetext.Contains("该手机号未注册，将为您直接注册。") || pagetext.Contains("重新获取"))
            {
                return true;
            }
            else
            {
                return await WaitSendSms(page);
            }
        }
        public async Task PageClose(string Phone)
        {
            var page = GetPage(Phone);
            if (page != null)
            {
                Delpage(Phone, page);
                var browser = page.Browser;
                await page.CloseAsync();
                var oldpage = GetPage();
                if (oldpage == null)
                {
                    await browser.CloseAsync();
                }
            }
        }

        public async Task ReSendSmSCode(string Phone)
        {
            var page = GetPage(Phone);
            if (page == null) throw new Exception("页面未找到,可能超时回收了.请刷新页面重新登录");
            await GetPhoneCode(Phone, page);
        }
        /// <summary>
        /// 因为网络出现的傻逼等待代码
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<bool> AwitVerifyCode(Page page)
        {
            try
            {
                await page.WaitForTimeoutAsync(1000);
                // 打开京东App，购物更轻松
                string js = "document.body.outerText";
                var pageouterText = await page.EvaluateExpressionAsync(js);
                var pagetext = pageouterText.ToString();
              
                if (pagetext.Contains("验证码输入错误") || pagetext.Contains("验证码错误多次，请重新获取") || pagetext.Contains("该手机号未注册，将为您直接注册。") || pagetext.Contains("打开京东App，购物更轻松"))
                {
                    return true;
                }
                else
                {
                    return await AwitVerifyCode(page);
                }
            }
            catch (Exception e)
            {
                return await AwitVerifyCode(page);
            }

        }

        public async Task<ResultModel<object>> VerifyCode(int qlkey,string qq, string Phone, string Code)
        {

            ResultModel<object> result = ResultModel<object>.Create(false, "");
            if (_mainConfig.UPTYPE == UpTypeEum.ql)
            {
                var qlconfig = _mainConfig.GetConfig(qlkey);
                if (qlconfig == null)
                {
                    result.message = "未找到相应的服务器配置。请刷新页面后再试";
                    result.data = new { Status = 404 };
                    return result;
                }
            }
           

            Page page = GetPage(Phone);
            if (page == null)
            {
                result.message = "未找到当前号码的网页请稍候再试,或者网页超过3分钟已被回收";
                result.data = new { Status = 404 };
                return result;
            }
            await SetCode(Code, page);
            //await page.WaitForTimeoutAsync(400);
            Console.WriteLine("输入验证码" + Code);
          
            await page.ClickAsync("a[report-eventid='MLoginRegister_SMSLogin']");
            await AwitVerifyCode(page);

            string js = "document.body.outerText";
            var pageouterText = await page.EvaluateExpressionAsync(js);
            var pagetext = pageouterText.ToString();
            // Console.WriteLine(pagetext.ToString());
            if (pagetext.Contains("验证码输入错误"))
            {
                result.message = "验证码输入错误";
                return result;
            }
            if (pagetext.Contains("验证码错误多次，请重新获取"))
            {
                await PageClose(Phone);
                result.data = new { Status = 501 };
                result.message = "验证码错误多次,请过三分钟之后再来。";
                return result;
            }
            if (pagetext.Contains("该手机号未注册，将为您直接注册。"))
            {
                await PageClose(Phone);

                result.data = new { Status = 501 };
                result.message = "该手机号未注册";
                return result;
            }
            if (pagetext.Contains("打开京东App，购物更轻松"))
            {
                var cookies = await page.GetCookiesAsync();
                var CKkey = cookies.FirstOrDefault(x => x.Name == "pt_key");
                var CKpin = cookies.FirstOrDefault(x => x.Name == "pt_pin");
                if (CKkey == null || CKpin == null)
                {
                    await PageClose(Phone);
                    result.message = "获取Cookie失败，请重试";
                    result.data = new { Status = 404 };
                    return result;
                }
                
                var CCookie = CKkey.Name + "=" + CKkey.Value + ";" + CKpin.Name + "=" + CKpin.Value + ";";
                await PageClose(Phone);
                Console.WriteLine(Phone + "获取到ck");
                if (_mainConfig.UPTYPE == UpTypeEum.ql)
                {
                    result = await UploadQL(Phone, CCookie, CKpin.Value, qlkey);
                    return result;
                }
                if (_mainConfig.UPTYPE == UpTypeEum.xdd)
                {
                    result = await Uploadxdd(qq, CCookie);
                    return result;
                }
                int tabcount = GetTableCount();
                result.data = new { tabcount = tabcount };
                result.success = true;
                result.data = new { tabcount = tabcount,ck=CCookie };
            }
            result.message = "登陆失败,请刷新页面";
            return result;


        }

        public async Task<ResultModel<object>> Uploadxdd(string qq, string ck)
        {
            //"code":200,"data":"null","message":"添加成功"
            ResultModel<object> result = ResultModel<object>.Create(false, "");
         
            using (HttpClient client = new HttpClient())
            {
                Dictionary<string, string> dict = new Dictionary<string, string>
                     {
                         {"qq",qq},
                        {"token", _mainConfig.XDDToken},
                         {"ck", ck}
                    };

                var resultd =await client.PostAsync(_mainConfig.XDDurl, new FormUrlEncodedContent(dict));
                string resultContent = resultd.Content.ReadAsStringAsync().Result;
              

                JObject j = JObject.Parse(resultContent);
                int tabcount = GetTableCount();
                if (j["code"].ToString() == "200")
                {
                    result.success = true;
                    result.message = "添加xdd成功!";
                }
                else
                {
                    result.message = j["message"].ToString();
                }
                result.data = new { tabcount = tabcount};
                return result;
            }

        }
        public async Task<ResultModel<object>> UploadQL(string Phone,string ck,string ckpin , int qlkey)
        {
            ResultModel<object> result = ResultModel<object>.Create(false, "");
            var qlconfig = _mainConfig.GetConfig(qlkey);
            var Nickname = "";
            int MAXCount = qlconfig.QL_CAPACITY;
            Nickname = await GetNickname(ck);
            JArray data = await qlconfig.GetEnv();
            JToken env = null;
            var QLCount = 0;
            if (data != null)
            {
                env = data.FirstOrDefault(x => x["value"].ToString().Contains("pt_pin=" + ckpin + ";"));
                QLCount = data.Count;
            }

            string QLId = "";
            string timestamp = "";
            if (env == null)
            {
                if (QLCount >= MAXCount)
                {
                    result.message = "你来晚了，没有多余的位置了";
                    result.data = new { Status = 501 };
                    return result;
                }

                var addresult = await qlconfig.AddEnv(ck, Nickname);
                JObject addUser = (JObject)addresult.data[0];
                QLId = addUser["_id"].ToString();
                timestamp = addUser["timestamp"].ToString();
            }
            else
            {
                QLId = env["_id"].ToString();
                if (env["remarks"] != null)
                    Nickname = env["remarks"].ToString();


                var upresult = await qlconfig.UpdateEnv(ck, QLId, Nickname);
                timestamp = upresult.data["timestamp"].ToString();
            }
            await qlconfig.Enable(QLId);
            var qin = await getCount(qlkey);
            await PageClose(Phone);
            result.success = true;
            result.data = new { qlid = QLId, nickname = Nickname, timestamp = timestamp, remarks = Nickname, qlkey = qlconfig.QLkey, ckcount = qin.ckcount, tabcount = qin.tabcount };
            return result;

        }
        public int GetTableCount()
        {
            string MaxTab = _mainConfig.MaxTab;
            var intabcount = GetPageCount();
            int tabcount = int.Parse(MaxTab) - intabcount;
            return tabcount;
        }
        public async Task<(int ckcount, int tabcount)> getCount(int qlkey)
        {
            var config = _mainConfig.GetConfig(qlkey);
            var qlcount = await config.GetEnvsCount();
            var ckcount = config.QL_CAPACITY - qlcount;
            string MaxTab = _mainConfig.MaxTab;
            var intabcount = GetPageCount();
            int tabcount = int.Parse(MaxTab) - intabcount;
            return (ckcount, tabcount);
        }
        private async Task Setphone(string phone, Page page)
        {
            await page.ClickAsync("input[report-eventid='MLoginRegister_SMSPhoneInput']", new PuppeteerSharp.Input.ClickOptions { ClickCount = 3 });

            await page.TypeAsync("input[report-eventid='MLoginRegister_SMSPhoneInput']", phone);
            await page.WaitForTimeoutAsync(200);
        }
        private async Task<bool> GetPhoneCode(string Phone, Page page)
        {
            page = AddPage(Phone, page);
            await Setphone(Phone, page);
            var CodeBtn = await page.XPathAsync("//button[@report-eventid='MLoginRegister_SMSReceiveCode']");
            var CodeProperties = await CodeBtn[0].GetPropertiesAsync();

            ///var url = await CodeBtn[0].EvaluateFunctionAsync<string>("e => e.setAttribute('class','active')");
            //await page.evaluate((el, value) => el.setAttribute('style', value),el.setAttribute('style', value
            //            divHandle,
            //            'background: #0FF'
            //    )
            // CodeBtn[0].EvaluateFunctionAsync
            //page.ev
            // CodeBtn.Append
            var CodeBtnClasses = CodeProperties["_prevClass"].ToString().Split(" ");
            Console.WriteLine(CodeProperties["_prevClass"].ToString());
            bool canSendCode = CodeBtnClasses.Contains("active");
            if (canSendCode)
            {
                return true;
            }
            else
            {
                await page.ReloadAsync();
                await page.WaitForTimeoutAsync(500);
                return await GetPhoneCode(Phone, page);
            }
        }
        private long GetTime()
        {
            TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);//ToUniversalTime()转换为标准时区的时间,去掉的话直接就用北京时间
            return (long)ts.TotalSeconds;
        }
        private async Task<string> GetNickname(string cookie)
        {
            try
            {
                var url = @"https://me-api.jd.com/user_new/info/GetJDUserInfoUnion?orgFlag=JD_PinGou_New&callSource=mainorder&channel=4&isHomewhite=0&sceneval=2&_=" + GetTime() + "&sceneval=2&g_login_type=1&g_ty=ls";
                using (HttpClient client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Cookie", cookie);
                    client.DefaultRequestHeaders.Add("Referer", "https://home.m.jd.com/myJd/newhome.action");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Host", "me-api.jd.com");
                    var result = await client.GetAsync(url);
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    Console.WriteLine("获取nickname");
                    JObject j = JObject.Parse(resultContent);
                    // data?.userInfo.baseInfo.nickname
                    return j["data"]["userInfo"]["baseInfo"]["nickname"].ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("获取nickname出错:"+e.Message);
                return "未知";
            }


        }

        private static async Task SetCode(string Code, Page page)
        {
            await page.ClickAsync("#authcode", new PuppeteerSharp.Input.ClickOptions { ClickCount = 3 });
            await page.TypeAsync("#authcode", Code);

        }
        /// <summary>
        /// 因为网络出现的傻逼等待代码
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<bool> AwitAutoCaptcha(Page page)
        {
            try
            {
                await page.WaitForTimeoutAsync(1000);
                // 打开京东App，购物更轻松
                string js = "document.body.outerText";
                var pageouterText = await page.EvaluateExpressionAsync(js);
                var pagetext = pageouterText.ToString();
                if (pagetext.Contains("重新获取") || pagetext.Contains("短信验证码发送次数已达上限") || pagetext.Contains("该手机号未注册，将为您直接注册。") )
                {
                    return true;
                }
                else
                {
                    return await AwitAutoCaptcha(page);
                }
            }
            catch (Exception e)
            {
                return await AwitAutoCaptcha(page);
            }

        }
        public async Task<ResultModel<object>> AutoCaptcha(string Phone)
        {
            Console.WriteLine(Phone + "安全验证破解");
            var page = GetPage(Phone);
            var cpc_img = await page.QuerySelectorAsync("#cpc_img");
            var cpc_imgheader = await cpc_img.GetPropertyAsync("src");
            var cpc_imgsrc = await cpc_imgheader.JsonValueAsync();
            var small_img = await page.QuerySelectorAsync("#small_img");
            var small_imgheader = await small_img.GetPropertyAsync("src");
            var small_imgsrc = await small_imgheader.JsonValueAsync();
            string pattern = @"data:image.*base64,(.*)";
            Match m = Regex.Match(cpc_imgsrc.ToString(), pattern);
            var cpc_imgbase64 = m.Groups[1].ToString();
            Match m2 = Regex.Match(small_imgsrc.ToString(), pattern);
            var small_imgbase64 = m2.Groups[1].ToString();
            byte[] cpc_imgBytes = Convert.FromBase64String(cpc_imgbase64);
            byte[] small_imgbaseBytes = Convert.FromBase64String(small_imgbase64);
            Stream cpcstream = new MemoryStream(cpc_imgBytes);
            Stream smallstream = new MemoryStream(small_imgbaseBytes);
            var cpcmap = new Bitmap(new Bitmap(cpcstream));
            var smallmap = new Bitmap(new Bitmap(smallstream));
            var Rsct = cv.getOffsetX(cpcmap, smallmap);
            cpcmap.Dispose();
            cpcstream.Close();
            smallstream.Close();
            smallmap.Dispose();
            var list = cv.GetPoints2(Rsct);
            var slider = await page.WaitForXPathAsync("//div[@class='sp_msg']/img");
            var box = await slider.BoundingBoxAsync();
            await page.Mouse.MoveAsync(box.X, box.Y);
            await page.Mouse.DownAsync();
            Random r = new Random(Guid.NewGuid().GetHashCode());
            var Steps = r.Next(10, 15);
            foreach (var item in list)
            {
                await page.Mouse.MoveAsync(item.X, box.Y, new PuppeteerSharp.Input.MoveOptions { Steps = Steps });

            }
            await page.Mouse.UpAsync();
            await AwitAutoCaptcha(page);
            string js = "document.body.outerText";
            var pageouterText = await page.EvaluateExpressionAsync(js);
            var html = pageouterText.ToString();
            ResultModel<object> result = ResultModel<object>.Create(false, "");
            if (html.Contains("重新获取"))
            {
                Console.WriteLine("验证成功");
                result.success = true;
            }
            else
            {
                if (html.Contains("短信验证码发送次数已达上限"))
                {
                    await PageClose(Phone);
                    result.data = new { Status = 505 };
                    result.message = "对不起，短信验证码发送次数已达上限，请24小时后再试。";
                    return result;
                }
                if (html.Contains("该手机号未注册，将为您直接注册。"))
                {
                    await PageClose(Phone);

                    result.data = new { Status = 505 };
                    result.message = "该手机号未注册";
                    return result;
                }
                Console.WriteLine("验证失败");
                result.data = new { Status = 666 };
            }
            return result;
        }
    }
}
