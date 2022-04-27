using AutoAttendance;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProxyWindow
{
    public partial class Proxy : Form
    {
        public Proxy()
        {
            InitializeComponent();
            /// 初始化环境设置
            GlobalEnvironmentSetting();

            var browser = BrowserSimulation.GetInstance();
            if (Directory.Exists(browser.ImageFolder) == false)
            {
                Directory.CreateDirectory(browser.ImageFolder);
            }
            var runner = new TaskRunner();
            runner.Run();            
        }

        private static void GlobalEnvironmentSetting()
        {
            /// 设置对其他编码的支持
            EncodingProvider encodingProvider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(encodingProvider);
            XmlConfigurator.Configure(new FileInfo("Log4Net.config"));
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        /// <summary>  
        /// 获取窗口句柄  
        /// </summary>  
        /// <param name="lpClassName"></param>  
        /// <param name="lpWindowName"></param>  
        /// <returns></returns>  
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>  
        /// 设置窗体的显示与隐藏  
        /// </summary>  
        /// <param name="hWnd"></param>  
        /// <param name="nCmdShow"></param>  
        /// <returns></returns>  
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

    }
}
