using Newtonsoft.Json;
using System;

namespace Systems
{
    [Serializable]
    public class ResultModel<T> where T : new()
    {
        /// <summary>
        /// 创建数据包
        /// </summary>
        /// <param name="success"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static ResultModel<T> Create(bool success)
        {
            ResultModel<T> result = new ResultModel<T>();
            result.success = success;
            return result;
        }
        /// <summary>
        /// 创建数据包
        /// </summary>
        /// <param name="success"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static ResultModel<T> Create(bool success, string message)
        {
            ResultModel<T> result = new ResultModel<T>();
            result.success = success;
            result.message = message;
            return result;
        }
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool success { get; set; } = false;
        /// <summary>
        /// 返回信息
        /// </summary>
        public string message { get; set; } = "";

        private T _data;
        /// <summary>
        /// 返回数据集合
        /// </summary>
        public T data { get { if (_data == null) _data = new T(); return _data; } set { _data = value; } }

        /// <summary>
        /// 
        /// </summary>
        public ResultModel() { }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="suc"></param>
        public ResultModel(T t, bool suc = true)
        {
            data = t;
            this.success = suc;
        }
        public string ToJson()
        {
            string result = JsonConvert.SerializeObject(this);

            return result;
        }
    }
}
