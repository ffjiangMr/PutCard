namespace AutoAttendance
{
    #region using directive 

    using log4net;

    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Management;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    #endregion
    public sealed class TaskRunner
    {
        /// <summary>
        /// Log 输出实例
        /// </summary>
        private ILog logger;

        private Boolean isStop;

        private Int32 beginTime = 7;

        private Int32 endTime = 17;

        private Int32 delaySpan = 50;

        private List<ManagementObject> networkInterface;

        public TaskRunner()
        {
            this.logger = LogManager.GetLogger(this.GetType());
            networkInterface = new List<ManagementObject>();
            Int32.TryParse(ConfigurationManager.AppSettings["BeginTime"], out this.beginTime);
            Int32.TryParse(ConfigurationManager.AppSettings["EndTime"], out this.endTime);
            Int32.TryParse(ConfigurationManager.AppSettings["DelaySpan"], out this.delaySpan);
            this.isStop = false;
        }

        ~TaskRunner()
        {
            this.isStop = true;
        }

        /// <summary>
        /// 计算延时时间(30 ~ 81分钟).
        /// </summary>
        /// <returns></returns>
        private Int32 GetDelay()
        {
            Int32 result = 0;
            var random = new Random(DateTime.Now.Millisecond);
            result = random.Next(this.delaySpan, this.delaySpan + 10) * 60 + random.Next(59);
            this.logger.Info($"Delay {result} seconds.");
            return result;
        }

        /// <summary>
        /// 计算当天是否进行任务
        /// </summary>
        /// <returns></returns>
        private Boolean IsActive()
        {
            return String.Compare(ConfigurationManager.AppSettings[DateTime.Now.DayOfWeek.ToString()], "true", true) == 0;
        }

        public void Run()
        {
            Task.Run(async () =>
            {
                try
                {
                    this.logger.Info("Task start.");



                    Boolean isAttend = false;
                    Boolean isLeave = false;
                    while (this.isStop == false)
                    {
                        ConfigurationManager.RefreshSection("appSettings");
                        Int32.TryParse(ConfigurationManager.AppSettings["BeginTime"], out this.beginTime);
                        Int32.TryParse(ConfigurationManager.AppSettings["EndTime"], out this.endTime);
                        Int32.TryParse(ConfigurationManager.AppSettings["DelaySpan"], out this.delaySpan);
                        try
                        {
                            if (this.IsActive() == true)
                            {
                                this.logger.Info("Need attendance.");
                                if ((DateTime.Now.Hour == this.beginTime) &&
                                    (isAttend == false))
                                {
                                    this.logger.Info("Attendance delay.");
                                    Thread.Sleep(this.GetDelay() * 1000);
                                    while (isAttend == false)
                                    {
                                        isAttend = await this.SubTask();
                                        this.logger.Info($"First attendance, result: {isAttend}.");
                                    }
                                    isLeave = false;
                                    this.logger.Info("Attendance.");
                                }
                                else if ((DateTime.Now.Hour == this.endTime) &&
                                         (isLeave == false))
                                {
                                    this.logger.Info("Leave delay.");
                                    Thread.Sleep(this.GetDelay() * 1000);
                                    while (isLeave == false)
                                    {
                                        isLeave = await this.SubTask();
                                        this.logger.Info($"Second attendance, result:{isLeave}.");
                                    }
                                    isAttend = false;
                                    this.logger.Info("Leave.");
                                }
                                else
                                {
                                    this.logger.Info("Working time, delay 1000 * 60 * 10 secondes.");
                                    Thread.Sleep(1000 * 60 * 10);
                                }
                            }
                            else
                            {
                                this.logger.Info("Relax, delay 1000 * 60 * 60 secondes.");
                                Thread.Sleep(1000 * 60 * 60);
                                isAttend = false;
                                isLeave = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            this.logger.Info($"Task error:{ex.Message}.");
                            this.logger.Info($"Task error:{ex}.");
                        }
                    }
                    this.logger.Info("Task stop.");
                }
                catch (Exception ex)
                {
                    this.logger.Info($"Task error:{ex.Message}");
                    this.logger.Info($"Task error:{ex}");
                }
            });
        }

        private async Task<Boolean> SubTask()
        {
            var loginTask = await BrowserSimulation.GetInstance().Login();
            var attendTask = await BrowserSimulation.GetInstance().Attendance(loginTask);
            return attendTask;
        }

        private void GetNetworkInterface()
        {
            List<String> netWorkList = new List<String>();
            String manage = "SELECT * From Win32_NetworkAdapter";
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var item in nics)
            {
                if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    netWorkList.Add(item.Description);
                }
            }
            if (netWorkList.Count > 0)
            {
                manage += " where ";
                foreach (var item in netWorkList)
                {
                    manage += (" Name = \"" + item + "\" OR");
                }
                manage = manage.Substring(0, manage.Length - 2);
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(manage);
            ManagementObjectCollection collection = searcher.Get();
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    this.networkInterface.Add(item);
                }
            }
        }

    }
}
