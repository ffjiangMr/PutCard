namespace AutoAttendance
{

    #region using directive 

    using log4net;
    using System;
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    #endregion 

    public enum LogState
    {
        DEFAULT = 0,
        LOGING = 1,
        LOGED = 2,
    }

    /// <summary>
    /// 以Http协议访问服务的客户端抽象
    /// </summary>
    internal sealed class HttpHelper : IDisposable
    {
        #region Field

        /// <summary>
        /// 登陆网址
        /// </summary>
        private String url;

        /// <summary>
        /// Http 请求客户端
        /// </summary>
        private HttpClient client;

        /// <summary>
        /// 是否进行初始化操作
        /// </summary>
        private Boolean isInit;

        /// <summary>
        /// 资源是否释放
        /// </summary>
        private Boolean disposed;

        /// <summary>
        /// Log 管理实例
        /// </summary>
        private ILog logger;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public HttpHelper()
        {
            this.url = ConfigurationManager.AppSettings["url"];
            this.client = new HttpClient();
            this.isInit = false;
            this.disposed = false;
            this.logger = LogManager.GetLogger(this.GetType());
        }

        /// <summary>
        /// 以Get方法访问指定Http资源,并返回资源
        /// </summary>
        /// <param name="url">资源URL路径</param>        
        /// <returns>访问的资源Body</returns>访问方法
        public async Task<String> AccessToObtainBodyAsync(String url)
        {
            String result = String.Empty;
            if (this.isInit == true)
            {
                this.logger.Info($"Access source path:{url}.");
                try
                {
                    using (var response = await this.AccessAsync(url, HttpMethod.Get))
                    {
                        this.logger.Info($"Source path:{url},server response code:{response.StatusCode}.");
                        if (response.IsSuccessStatusCode == true)
                        {
                            result = await response.Content.ReadAsStringAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error($"Source path:{url},Exception message:{ex.Message}.");
                    this.logger.Error($"Source path:{url},Exception stack:{ex}.");
                }
            }
            return result;
        }

        /// <summary>
        /// 以Get访问访问指定资源，并将资源存储在指定路径
        /// </summary>
        /// <param name="url">资源URL路径</param>
        /// <param name="savePath">资源保存的全路径(包含文件名)</param>
        /// <returns>是否成功保存资源</returns>
        public async Task<Boolean> AccessToSaveBodyAsync(String url, String savePath)
        {
            Boolean result = false;
            if (this.isInit == true)
            {
                this.logger.Info($"Access source path:{url}.");
                try
                {
                    using (var response = await this.AccessAsync(url, HttpMethod.Get))
                    {
                        this.logger.Info($"Source path:{url},Serve response code:{response.StatusCode}.");
                        if (response.IsSuccessStatusCode == true)
                        {
                            using (var writer = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                using (var reader = await response.Content.ReadAsStreamAsync())
                                {
                                    Byte[] buffer = new Byte[1024 * 4];
                                    var count = -1;
                                    while (count != 0)
                                    {
                                        count = reader.Read(buffer, 0, buffer.Length);
                                        writer.Write(buffer, 0, count);
                                    }
                                }
                            }
                            result = File.Exists(savePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error($"Source path:{url},Exception message:{ex.Message}.");
                    this.logger.Error($"Source path:{url},Exception stack:{ex}.");
                }
            }
            return result;
        }

        /// <summary>
        /// 以Post方式访问资源
        /// </summary>
        /// <param name="url">资源URL路径</param>
        /// <param name="content">待提交的数据</param>
        /// <returns>资源服务器的返回值</returns>
        public async Task<String> PostContentAsync(String url, HttpContent content)
        {
            String result = String.Empty;
            if (this.isInit == true)
            {
                try
                {
                    this.logger.Info($"Access source path:{url}.");
                    using (var response = await this.AccessAsync(url, HttpMethod.Post, content))
                    {
                        this.logger.Info($"Source path:{url},response code:{response.StatusCode}.");
                        result = await response.Content.ReadAsStringAsync();
                    }
                    this.logger.Info($"Post result:{result}.");
                }
                catch (Exception ex)
                {
                    this.logger.Error($"Source path:{url},exception message:{ex.Message}.");
                    this.logger.Error($"Source path:{url},exception stack:{ex}.");
                }
            }
            return result;
        }

        /// <summary>
        /// 根据提供的资源路径，访问方法，以及待提交的数据，对服务器发起访问
        /// </summary>
        /// <param name="url">资源路径</param>
        /// <param name="mehtod">访问方法</param>
        /// <param name="content">待提交的实体(可为null)</param>
        /// <returns>服务器对请求的响应</returns>
        private async Task<HttpResponseMessage> AccessAsync(String url, HttpMethod mehtod, HttpContent content = null)
        {
            HttpResponseMessage result;
            using (HttpRequestMessage request = new HttpRequestMessage(mehtod, new Uri(this.client.BaseAddress, url)))
            {
                request.Content = content;
                result = await this.client.SendAsync(request);
                if (result.IsSuccessStatusCode == true)
                {
                    if (result.Headers.Contains("Set-Cookie") == true)
                    {
                        foreach (var item in result.Headers.GetValues("Set-Cookie"))
                        {
                            this.client.DefaultRequestHeaders.Remove("Cookie");
                            this.client.DefaultRequestHeaders.Add("Cookie", item.Split(';')[0]);
                            this.logger.Info($"Setting cookie:{item.Split(';')[0]}.");
                            break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 对Http请求进行初始化操作
        /// 发送请求前，务必进行初始化
        /// </summary>
        public void InitClient()
        {
            this.SetProxy();
            this.SetDefaultHeaders();
            this.client.Timeout = new TimeSpan(0, 2, 0);
            this.isInit = true;
        }

        /// <summary>
        /// 如果配置了代理，则将代理设置加入到请求中
        /// </summary>
        private void SetProxy()
        {
            if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["proxy-url"]) == false)
            {
                WebProxy proxy = new WebProxy(ConfigurationManager.AppSettings["proxy-url"]);
                if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["proxy-username"]) == false)
                {
                    NetworkCredential credential = new NetworkCredential(ConfigurationManager.AppSettings["proxy-username"], ConfigurationManager.AppSettings["proxy-password"]);
                    proxy.Credentials = credential;
                    HttpClient.DefaultProxy = proxy;
                    this.logger.Debug($"Setting proxy.");
                    this.logger.Debug($"Host:{ConfigurationManager.AppSettings["proxy-url"]}.");
                    this.logger.Debug($"Username:{ConfigurationManager.AppSettings["proxy-username"]}.");
                    this.logger.Debug($"Password:{ConfigurationManager.AppSettings["proxy-password"]}.");
                }
            }
        }

        /// <summary>
        /// 设置默认请求头，模拟浏览器
        /// </summary>
        private void SetDefaultHeaders()
        {
            this.client.DefaultRequestHeaders.Connection.Add("keep-alive");
            this.client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            this.client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36");
            this.client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,application/json,text/javascript,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            this.client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
            this.client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            this.client.BaseAddress = new Uri(this.url);
            this.logger.Debug($"Setting request header[keep-alive].");
            this.logger.Debug($"Setting request header[Cache-Control:max-age=0].");
            this.logger.Debug($"Setting request header[UserAgent:Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36].");
            this.logger.Debug($"Setting request header[Accept:text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,application/json,text/javascript,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9].");
            this.logger.Debug($"Setting request header[AcceptEncoding:gzip, deflate].");
            this.logger.Debug($"Setting request header[AcceptLanguage:en-US,en;q=0.9].");
            this.logger.Debug($"Setting base address:{this.url}.");
        }

        /// <summary>
        /// 实现Dispose接口
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            this.isInit = false;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 实现Dispose模式
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        private void Dispose(bool disposing)
        {
            if (this.disposed == false)
            {
                if (disposing)
                {
                    this.client.Dispose();
                }
                this.disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~HttpHelper()
        {
            this.Dispose(false);
        }
    }
}
