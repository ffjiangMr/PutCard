namespace AutoAttendance
{
    #region using directive

    using log4net;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    #endregion 

    public sealed class BrowserSimulation : IDisposable
    {
        private static Object locker = new Object();

        #region Field

        /// <summary>
        /// 实际网络访问者对象
        /// 以http协议访问服务器
        /// </summary>
        private HttpHelper httpVistor;

        /// <summary>
        /// 实现单例模式，静态私有实例
        /// </summary>
        private static BrowserSimulation instance;

        /// <summary>
        /// 实例是否被释放
        /// </summary>
        private Boolean disposed;

        /// <summary>
        /// 图片存在文件夹位置
        /// </summary>
        private readonly String imageFolder;

        /// <summary>
        /// 图片存在全路径，包含文件名
        /// </summary>
        private readonly String imageFullPath;

        /// <summary>
        /// Log 管理实例
        /// </summary>
        private ILog logger;

        /// <summary>
        /// 登录状态
        /// </summary>
        private LogState state;

        #endregion

        #region Property

        /// <summary>
        /// 图片存放文件夹路径
        /// </summary>
        public String ImageFolder { get { return this.imageFolder; } }

        /// <summary>
        /// 图片全路径，包含文件名
        /// </summary>
        public String ImageFullPath { get { return this.imageFullPath; } }

        #endregion

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private BrowserSimulation()
        {
            this.imageFolder = "Image";
            this.imageFullPath = "Image\\bigImage.png";
            this.httpVistor = new HttpHelper();
            this.httpVistor.InitClient();
            this.state = LogState.DEFAULT;
            this.logger = LogManager.GetLogger(this.GetType());
        }

        public static BrowserSimulation GetInstance()
        {
            if (instance == null)
            {
                instance = new BrowserSimulation();
            }
            return instance;
        }


        /// <summary>
        /// 模拟浏览器登陆系统
        /// </summary>
        public async Task<String> Login()
        {
            String result = String.Empty;
            try
            {
                Boolean isLoging = false;
                lock (locker)
                {
                    if ((this.state != LogState.LOGING))
                    {
                        this.state = LogState.LOGING;
                        isLoging = true;
                    }
                }
                if (isLoging == true)
                {
                    while (String.IsNullOrEmpty(result) == true)
                    {
                        /// 访问登陆页面
                        var logPage = await this.httpVistor.AccessToObtainBodyAsync("index.jsp");
                        this.logger.Info("Access Login page.");
                        this.logger.Debug($"Login page info:{logPage}.");

                        /// 获取验证码图片资源路径
                        var imagePage = await this.httpVistor.AccessToObtainBodyAsync("jigsaw");
                        this.logger.Info("Obtain verification code image path.");

                        /// 解析验证码资源真实路径
                        var paths = ToolHelper.GetValueByKeyFromJson(ref imagePage, new String[] { "smallImage", "bigImage" });

                        /// 下载验证码图片            
                        if (Directory.Exists(this.ImageFolder) == true)
                        {
                            foreach (KeyValuePair<String, String> item in paths)
                            {
                                this.logger.Info($"Download verification code image :{item.Key}.");
                                _ = await this.httpVistor.AccessToSaveBodyAsync("upload/jigsawImg/" + item.Value + ".png", Path.Combine(this.imageFolder, item.Key + ".png"));
                            }
                        }

                        /// 模拟浏览器页面加载完成脚本访问资源
                        /// ClearS()
                        Dictionary<String, String> valuesMap = new Dictionary<String, String>();
                        valuesMap["type"] = "0";
                        valuesMap["img"] = "S";
                        _ = await this.ScriptSimulation("jigsawVerify", valuesMap);
                        this.logger.Info($"Simulation script access source:ClearS().");

                        /// ClearB
                        valuesMap["type"] = "0";
                        valuesMap["img"] = "B";
                        _ = await this.ScriptSimulation("jigsawVerify", valuesMap);
                        this.logger.Info($"Simulation script access source:ClearB().");

                        /// 模拟验证码请求
                        String path = "Image\\bigImage.png";
                        valuesMap.Clear();
                        valuesMap["xWidth"] = ToolHelper.CalculateCodeIndex(ref path).ToString();
                        valuesMap["type"] = "1";
                        var verifyCode = await this.ScriptSimulation("jigsawVerify", valuesMap);
                        this.logger.Info($"Simulation slide verify.");
                        if (verifyCode == "1")
                        {
                            this.logger.Info($"Verify success.");
                        }
                        else
                        {
                            continue;
                        }

                        /// 模拟登陆            
                        result = await this.ScriptSimulation("login.jsp", ToolHelper.GetFormDataFromHtml(ref logPage, valuesMap["xWidth"]));
                        this.logger.Info("Login finished.");
                        this.logger.Debug($"Attendance page info:{result}.");
                    }
                    this.state = LogState.LOGED;
                }
            }
            catch (Exception ex)
            {
                this.logger.Error($"Operator exception.");
                this.logger.Error($"Exception message:{ex.Message}.");
                this.logger.Error($"Exception stack:{ex}.");
                this.state = LogState.DEFAULT;
            }
            return result;
        }
        /// <summary>
        /// 模拟打卡点击
        /// </summary>
        /// <param name="attendance">attendance 页面</param>        
        public async Task<Boolean> Attendance(String attendance)
        {
            Boolean isAttance = false;
            Int32 finishedCount = 0;
            Int32 attendanceCount = 0;
            lock (locker)
            {
                if (state == LogState.LOGED)
                {
                    isAttance = true;
                }
            }
            if (isAttance == true)
            {
                attendanceCount = attendance.Split(ConfigurationManager.AppSettings["EmployeeNo"]).Length;
                using (FormUrlEncodedContent postContent = new FormUrlEncodedContent(ToolHelper.GetFormDataFromHtml(ref attendance)))
                {
                    var response = await this.httpVistor.PostContentAsync("record.jsp", postContent);
                    finishedCount = response.Split(ConfigurationManager.AppSettings["EmployeeNo"]).Length;
                    this.logger.Info("Simulation attend action.");
                    if (attendanceCount < finishedCount)
                    {
                        state = LogState.DEFAULT;
                    }
                }
            }
            return attendanceCount < finishedCount;
        }


        /// <summary>
        /// 事件模拟
        /// </summary>
        /// <param name="url">服务器接口</param>
        /// <param name="valuesMap">待提交的表单数据</param>
        /// <returns>服务器响应</returns>
        private async Task<String> ScriptSimulation(String url, Dictionary<String, String> valuesMap)
        {
            String result = String.Empty;
            using (FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(valuesMap))
            {
                result = await this.httpVistor.PostContentAsync(url, encodedContent);
                encodedContent.Dispose();
            }
            return result;
        }

        /// <summary>
        /// 实现Dispose接口
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
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
                    this.httpVistor.Dispose();
                }
                this.disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~BrowserSimulation()
        {
            this.Dispose(false);
        }

    }
}
