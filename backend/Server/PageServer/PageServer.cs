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
        private  MainConfig _mainConfig;
        private OpenCVServer.OpenCVServer cv;
        public PageServer(MainConfig mainConfig, OpenCVServer.OpenCVServer openCV)
        {
            _mainConfig = mainConfig;
            cv = openCV;
        }
        public static Dictionary<string, Page> pagelist = new Dictionary<string, Page>();
        static readonly object _locker = new object();
        public  Page AddPage(string phone, Page page)
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
        public  void Delpage(string phone, Page page)
        {
            lock (_locker)
            {
                if (pagelist.ContainsKey(phone))
                {
                    pagelist.Remove(phone);
                }
            }
        }
        public  Page GetPage(string Phone)
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
        public  Page GetPage()
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
        public  int GetPageCount()
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
            var aa = await browserFetcher.DownloadAsync("884014");
            try
            {
                var path = aa.ExecutablePath;
                Bash($"chmod 777 {path}");
            }
            catch(Exception e)
            {
                Console.WriteLine("执行 CHOMD 777 浏览器地址错位 可以忽略");
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
        public async Task<ResultModel<object>> OpenJDTab(int qlkey, string Phone)
        {
            DateTime expdate = DateTime.Now;

            ResultModel<object> result = ResultModel<object>.Create(false, "");
            var qlconfig = _mainConfig.GetConfig(qlkey);
            if (qlconfig == null)
            {
                result.message = "未找到相应的服务器配置。请刷新页面后再试";
                result.data = new { Status = 404 };
                return result;
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
           //string Url = "https://plogin.m.jd.com/login/login?appid=300&returnurl=https%3A%2F%2Fwq.jd.com%2Fpassport%2FLoginRedirect%3Fstate%3D2087738584%26returnurl%3Dhttps%253A%252F%252Fhome.m.jd.com%252FmyJd%252Fnewhome.action%253Fsceneval%253D2%2526ufc%253D%2526&source=wq_passport";
            string Url = "https://bean.m.jd.com/bean/signIndex.action";
            var context = await browser.CreateIncognitoBrowserContextAsync();
            page = await context.NewPageAsync();
            ///屏蔽 WebDriver 检测
            await page.EvaluateFunctionOnNewDocumentAsync("function(){Object.defineProperty(navigator, 'webdriver', {get: () => undefined})}");
            DeviceDescriptor deviceOptions = Puppeteer.Devices.GetValueOrDefault(DeviceDescriptorName.IPhone7);
            await page.EmulateAsync(deviceOptions);
            await page.GoToAsync(Url);
            await page.WaitForTimeoutAsync(500);
            await GetPhoneCode(Phone, page);
            var seven = await page.EvaluateExpressionAsync("document.querySelectorAll('button[report-eventid=MLoginRegister_SMSReceiveCode]')[0].innerText");
            await page.ClickAsync("button[report-eventid='MLoginRegister_SMSReceiveCode']");
            await page.ClickAsync("input[type=checkbox]");
            await page.WaitForTimeoutAsync(500);
            string js = "document.body.outerText";
            var pageouterText = await page.EvaluateExpressionAsync(js);
            var pagetext = pageouterText.ToString();
            var data = await getCount(qlkey);
            if (pagetext.Contains("安全验证") && !pagetext.Contains("验证成功"))
            {
                Console.WriteLine(Phone + "安全验证");
               // await PageClose(Phone);
                result.data = new { Status = 666,ckcount = data.ckcount, tabcount = data.tabcount };
                result.message = "出现安全验证,";
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
            timer.Start();
           
            result.data = new { ckcount = data.ckcount, tabcount = data.tabcount };
            result.success = true;
            return result;
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

        public async Task<ResultModel<object>> VerifyCode(int qlkey, string Phone, string Code)
        {
            try
            {
                ResultModel<object> result = ResultModel<object>.Create(false, "");
                var qlconfig = _mainConfig.GetConfig(qlkey);
                if (qlconfig == null)
                {
                    result.message = "未找到相应的服务器配置。请刷新页面后再试";
                    result.data = new { Status = 404 };
                    return result;
                }

                Page page = GetPage(Phone);
                if (page == null)
                {
                    result.message = "未找到当前号码的网页请稍候再试,或者网页超过3分钟已被回收";
                    result.data = new { Status = 404 };
                    return result;
                }
                await SetCode(Code, page);
                Console.WriteLine("输入验证码" + Code);
                Console.WriteLine(page.Url);
                await page.ClickAsync("a[report-eventid='MLoginRegister_SMSLogin']");
                //  
                await page.WaitForTimeoutAsync(2500);
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

                await page.WaitForTimeoutAsync(2500);
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
                int MAXCount = qlconfig.QL_CAPACITY;
                var CCookie = CKkey.Name + "=" + CKkey.Value + ";" + CKpin.Name + "=" + CKpin.Value + ";";
                var Nickname = "";
                Nickname = await GetNickname(CCookie);
                JArray data = await qlconfig.GetEnv();
                var env = data.FirstOrDefault(x => x["value"].ToString().Contains("pt_pin=" + CKpin.Value + ";"));
                var QLCount = data.Count;
               
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
                    
                    var addresult = await qlconfig.AddEnv(CCookie, Nickname);
                    JObject addUser = (JObject)addresult.data[0];
                    QLId = addUser["_id"].ToString();
                    timestamp = addUser["timestamp"].ToString();
                }
                else
                {
                    QLId = env["_id"].ToString();
                    if(env["remarks"] != null)
                        Nickname = env["remarks"].ToString();
                    

                    var upresult = await qlconfig.UpdateEnv(CCookie, QLId, Nickname);
                    timestamp = upresult.data["timestamp"].ToString();
                }
                var qin = await getCount(qlkey);
                await PageClose(Phone);
                result.success = true;
                result.data = new { qlid = QLId, nickname = Nickname, timestamp = timestamp, remarks = Nickname, qlkey = qlconfig.QLkey, ckcount = qin.ckcount, tabcount = qin.tabcount };
                return result;
            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
           
        }
        public  async Task<(int ckcount, int tabcount)> getCount(int qlkey)
        {
            var config = _mainConfig.GetConfig(qlkey);
            var qlcount = await config.GetEnvsCount();
            var ckcount = config.QL_CAPACITY - qlcount;
            string MaxTab = _mainConfig.MaxTab;
            var intabcount = GetPageCount();
            int tabcount = int.Parse(MaxTab) - intabcount;
            return (ckcount, tabcount);
        }
        private  async Task Setphone(string phone, Page page)
        {
            string jq = @"document.querySelectorAll('input[report-eventid=MLoginRegister_SMSPhoneInput]')[0].value='';";
            await page.EvaluateExpressionAsync(jq);
            System.Threading.Thread.Sleep(500);
            await page.TypeAsync("input[report-eventid=MLoginRegister_SMSPhoneInput]", phone);
            System.Threading.Thread.Sleep(500);
        }
        private  async Task<bool> GetPhoneCode(string Phone, Page page)
        {
            page = AddPage(Phone, page);
            await Setphone(Phone, page);
            var CodeBtn = await page.XPathAsync("//button[@report-eventid='MLoginRegister_SMSReceiveCode']");
            var CodeProperties = await CodeBtn[0].GetPropertiesAsync();
            var CodeBtnClasses = CodeProperties["_prevClass"].ToString().Split(" ");
            bool canSendCode = CodeBtnClasses.Contains("active");
            if (canSendCode)
            {
                return true;
            }
            else
            {
                await page.ReloadAsync();
                System.Threading.Thread.Sleep(500);
                return await GetPhoneCode(Phone, page);
            }
        }
        private  long GetTime()
        {
            TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);//ToUniversalTime()转换为标准时区的时间,去掉的话直接就用北京时间
            return (long)ts.TotalSeconds;
        }
        private  async Task<string> GetNickname(string cookie)
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
                    //  Console.WriteLine(resultContent);
                    JObject j = JObject.Parse(resultContent);
                    // data?.userInfo.baseInfo.nickname
                    return j["data"]["userInfo"]["baseInfo"]["nickname"].ToString();
                }
            }
            catch(Exception e)
            {
                return "未知";
            }
            

        }

        private static async Task SetCode(string Code, Page page)
        {
            await page.ClickAsync("#authcode", new PuppeteerSharp.Input.ClickOptions { ClickCount = 3 });
            System.Threading.Thread.Sleep(500);
            await page.TypeAsync("#authcode", Code);
            System.Threading.Thread.Sleep(500);

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
            var Steps = r.Next(5, 15);
            foreach (var item in list)
            {
                await page.Mouse.MoveAsync(item.X, box.Y, new PuppeteerSharp.Input.MoveOptions { Steps = Steps });

            }
            await page.Mouse.UpAsync();
            await page.WaitForTimeoutAsync(1000);
            var html = await page.GetContentAsync();
            
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
