using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TradingWindowsService
{
    public partial class TradeService : ServiceBase
    {
        #region PrivateVariables
        private bool stopService;
        private readonly int GrabberServiceInterval;
        private readonly int SignalServiceInterval;
        private readonly string FileSourcePath;

        #endregion
        public TradeService()
        {
            GrabberServiceInterval = int.Parse(ConfigurationManager.AppSettings["GrabberServiceInterval"]);
            SignalServiceInterval = int.Parse(ConfigurationManager.AppSettings["SignalServiceInterval"]);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                base.OnStart(args);
                StartService();
            }
            catch (Exception)
            {
                //CommonOperationService.WriteToLogFile(Path.Combine(FileSourcePath,"ErrorLog.txt"),$"{DateTime.Now} ----- {ex}");
            }
        }

        protected override void OnStop()
        {
            stopService = true;
            base.OnStop();
        }
        private void StartService()
        {
            stopService = false;
            var saveDataThread = new Thread(th => this.SaveHistoricalData(TimeSpan.FromMinutes(GrabberServiceInterval)));
            saveDataThread.Start();
            var generateSignalThread = new Thread(th => this.GenerateSignal(TimeSpan.FromMinutes(SignalServiceInterval)));
            generateSignalThread.Start();
        }
        private void SaveHistoricalData(TimeSpan period)
        {
            while (!stopService)
            {
                try
                {
                    CandleHelper.AutoRefresh().GetAwaiter().GetResult();
                    Thread.Sleep(period);
                }
                catch (Exception)
                {
                    //CommonOperationService.WriteToLogFile(Path.Combine(FileSourcePath, "ErrorLog.txt"), $"{DateTime.Now} ----- {ex}");
                }
            }
        }
        private void GenerateSignal(TimeSpan period)
        {
            while (!stopService)
            {
                try
                {
                    SignalHelper.AutoRefresh().GetAwaiter().GetResult();
                    Thread.Sleep(period);
                }
                catch (Exception)
                {
                    //CommonOperationService.WriteToLogFile(Path.Combine(FileSourcePath, "ErrorLog.txt"), $"{DateTime.Now} ----- {ex}");
                }
            }
        }
    }
}
