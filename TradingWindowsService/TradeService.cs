using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
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
        private readonly bool DebugMode;

        #endregion
        public TradeService()
        {
            GrabberServiceInterval = int.Parse(ConfigurationManager.AppSettings["GrabberServiceInterval"]);
            SignalServiceInterval = int.Parse(ConfigurationManager.AppSettings["SignalServiceInterval"]);
            FileSourcePath = ConfigurationManager.AppSettings["FileSourcePath"];
            DebugMode = bool.Parse(ConfigurationManager.AppSettings["DebugMode"]);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                base.OnStart(args);
                StartService();
            }
            catch (Exception ex)
            {
                if (DebugMode)
                    CommonHelper.WriteLog(Path.Combine(FileSourcePath, "Log.txt"), $"{DateTime.Now} | error | {ex}" + Environment.NewLine);
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
                    var result = CandleHelper.AutoRefresh().GetAwaiter().GetResult();
                    if (DebugMode)
                    {
                        if (!result.Item1)
                            CommonHelper.WriteLog(Path.Combine(FileSourcePath, "Log.txt"), $"{DateTime.Now} | info | candle refresh success" + Environment.NewLine);
                        else
                            CommonHelper.WriteLog(Path.Combine(FileSourcePath, "Log.txt"), $"{DateTime.Now} | info | candle refresh error | {result.Item2}" + Environment.NewLine);
                    }
                    Thread.Sleep(period);
                }
                catch (Exception ex)
                {
                    if(DebugMode)
                        CommonHelper.WriteLog(Path.Combine(FileSourcePath, "Log.txt"), $"{DateTime.Now} | error | {ex}" + Environment.NewLine);
                }
            }
        }
        private void GenerateSignal(TimeSpan period)
        {
            while (!stopService)
            {
                try
                {
                    var result = SignalHelper.AutoRefresh().GetAwaiter().GetResult();
                    if (DebugMode)
                    {
                        if(result)
                            CommonHelper.WriteLog(Path.Combine(FileSourcePath, "Log.txt"), $"{DateTime.Now} | info | signal refresh success" + Environment.NewLine);
                        else
                            CommonHelper.WriteLog(Path.Combine(FileSourcePath, "Log.txt"), $"{DateTime.Now} | info | signal refresh error" + Environment.NewLine);
                    }

                    Thread.Sleep(period);
                }
                catch (Exception ex)
                {
                    if (DebugMode)
                        CommonHelper.WriteLog(Path.Combine(FileSourcePath, "Log.txt"), $"{DateTime.Now} | error | {ex}" + Environment.NewLine);
                }
            }
        }
    }
}
