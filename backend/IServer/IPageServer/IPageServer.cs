
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Systems;

namespace IServer.IPageServer
{
    public interface IPageServer
    {
        /// <summary>
        /// 检查浏览器是否下载
        /// </summary>
        /// <returns></returns>
        public Task<bool> BrowserInit();
        /// <summary>
        /// 关闭窗体
        /// </summary>
        /// <returns></returns>
        public Task PageClose(string Phone);

        /// <summary>
        /// 打开京东页面 并且发送验证码
        /// </summary>
        /// <param name="qlkey"></param>
        /// <param name="Phone"></param>
        /// <returns></returns>
        public Task<ResultModel<object>> OpenJDTab(int qlkey, string Phone);

        /// <summary>
        /// 验证验证码
        /// </summary>
        /// <param name="qlkey"></param>
        /// <param name="Phone"></param>
        /// <param name="Code"></param>
        /// <returns></returns>
        public Task<ResultModel<object>> VerifyCode(int qlkey, string Phone, string Code);
        /// <summary>
        /// 重新发送验证码
        /// </summary>
        /// <param name="Phone"></param>
        /// <returns></returns>
        public Task ReSendSmSCode(string Phone);

        public Task<ResultModel<object>> AutoCaptcha(string Phone);
        public int GetPageCount();
    }
}
