using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Systems
{
    public class MainConfig
    {
        /// <summary>
        /// 最大标签
        /// </summary>
        public string MaxTab { get; set; }
        /// <summary>
        /// 网站标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 网站公共
        /// </summary>
        public string Announcement { get; set; }

        /// <summary>
        /// 青龙配置
        /// </summary>
        public List< Qlconfig> Config = new List<Qlconfig>();

        public Qlconfig GetConfig(int Id)
        {
            var item = this.Config.FirstOrDefault(x => x.QLkey == Id);
            if (item == null) throw new Exception("未找相应到配置青龙");
            return item;

        }
        public MainConfig()
        {
             string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Config.json");
            if (System.IO.File.Exists(ConfigPath))
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(ConfigPath))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {

                        JObject o = (JObject)JToken.ReadFrom(reader);
                        Type t = this.GetType();
                        if ( o.Property("Config") != null)
                        {
                            var json = o["Config"].ToString();
                            this.Config= JsonConvert.DeserializeObject<List<Qlconfig>>(json);
                        }
                        foreach (PropertyInfo pi in t.GetProperties())
                        {
                            if(pi.Name!= "Config"&& o.Property(pi.Name) != null)
                            {
                                pi.SetValue(this, o[pi.Name].ToString(), null);
                            }
                           
                        }

                    }
                }
            }
        }
    }

    public class Qlconfig
    {
        public int QLkey { set; get; }
        public string QLName { set; get; }
        public string QLurl { set; get; }

        public string QL_CLIENTID { set; get; }

        public string QL_SECRET { set; get; }

        public int QL_CAPACITY { set; get; }

        public string QRurl { set; get; }
        public static long GetTime()
        {
            TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);//ToUniversalTime()转换为标准时区的时间,去掉的话直接就用北京时间
            return (long)ts.TotalSeconds;
        }
        public async Task<string> GetToken()
        {
            try
            {
                string value = "" ;
                   var url = QLurl + "/open/auth/token?client_id=" + QL_CLIENTID + "&client_secret=" + QL_SECRET;
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        var result = await client.GetAsync(url);
                        string resultContent = result.Content.ReadAsStringAsync().Result;
                        JObject j = JObject.Parse(resultContent);
                        value = j["data"]["token"].ToString();
                       
                    }

                return value.ToString();
            }
            catch (Exception E)
            {
                throw new Exception("配置有误获取token失败");
            }

        }


        public async Task<JArray> GetEnv(String searchValue = "")
        {
            var token = await GetToken();
            var Url = QLurl + "/open/envs?searchValue=" + searchValue + "&t=" + GetTime();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var result = await client.GetAsync(Url);
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    JObject j = JObject.Parse(resultContent);
                    if (j["code"].ToString() != "200")
                    {

                        return null;
                    }
                    JArray array = (JArray)j["data"];
                    return array;
                }
            }
            catch (Exception e)
            {
                return null;
            }


        }
        public async Task<int> GetEnvsCount(String searchValue = "")
        {
            JArray data = await GetEnv(searchValue);
            if (data != null)
                return data.Count;
            return 0;
        }

        public async Task<ResultModel<JArray>> AddEnv(string Ck, string remarks = "JDCOOKIE")
        {
            ResultModel<JArray> result = ResultModel<JArray>.Create(false, "");
            var token = await GetToken();
            var Url = QLurl + "/open/envs?t=" + GetTime();
            JArray jArray = new JArray();
            JObject jsonObject = new JObject();
            jsonObject.Add("value", Ck);
            jsonObject.Add("name", "JD_COOKIE");
            jsonObject.Add("remarks", remarks);
            jArray.Add(jsonObject);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(jArray.ToString());
                    httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var req = await client.PostAsync(Url, httpContent);
                    string resultContent = req.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(resultContent);
                    JObject j = JObject.Parse(resultContent);
                    if (j["code"].ToString() != "200")
                    {
                        result.message = "ck上传青龙失败";
                        return result;
                    }
                    result.success = true;
                    result.data = (JArray)j["data"];
                    result.message = "ck上传成功";
                }
            }
            catch (Exception e)
            {
                result.message = "ck上传青龙失败";
            }

            return result;
        }
        public async Task<JObject> GetEnvbyid(string Eid)
        {
            JArray data = await GetEnv();
            var env = data.FirstOrDefault(x => x["_id"].ToString() == Eid);
            return (JObject)env;
        }
        public async Task<ResultModel<JObject>> UpdateEnv(string Ck, string Eid, string remarks = "JDCOOKIE")
        {
            ResultModel<JObject> result = ResultModel<JObject>.Create(false, "");
            var token = await GetToken();
            var Url = QLurl + "/open/envs?t=" + GetTime();
            var pocoObject = new
            {
                value = Ck,
                name = "JD_COOKIE",
                remarks = remarks,
                _id = Eid
            };
            string json = JsonConvert.SerializeObject(pocoObject);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    StringContent data = new StringContent(json, Encoding.UTF8, "application/json");


                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var res = await client.PutAsync(Url, data);
                    string resultContent = res.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(resultContent);
                    JObject j = JObject.Parse(resultContent);
                    if (j["code"].ToString() != "200")
                    {
                        result.message = "更新账户错误，请重试";
                        return result;
                    }
                    result.success = true;
                    result.message = "ck更新/上传备注成功";
                    result.data = (JObject)j["data"];
                }
            }
            catch (Exception e)
            {
                result.message = e.Message;
            }

            return result;
        }
        public async Task<ResultModel<JObject>> DelEnv(string Eid)
        {
            ResultModel<JObject> result = ResultModel<JObject>.Create(false, "");
            var token = await GetToken();
            var Url = this.QLurl + "/open/envs?t=" + GetTime();


            var pocoObject = new string[]
            {
             Eid
            };
            string json = JsonConvert.SerializeObject(pocoObject);
            Console.WriteLine(json);
            try
            {
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Content = data,
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(Url)
                    };
                    //HttpContent httpContent = new StringContent(jArray.ToString());
                    //httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var req = await client.SendAsync(request);
                    string resultContent = req.Content.ReadAsStringAsync().Result;
                    JObject j = JObject.Parse(resultContent);
                    if (j["code"].ToString() != "200")
                    {
                        result.message = "删除账户错误，请重试";
                        return result;
                    }
                    result.success = true;
                    result.message = "账户已移除";
                    // result.data = (JObject)j["data"];
                }
            }
            catch (Exception e)
            {
                result.message = e.Message;
            }

            return result;
        }
    }
}
