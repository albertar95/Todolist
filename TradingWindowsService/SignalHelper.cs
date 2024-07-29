using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingWindowsService
{
    public static class SignalHelper
    {
        private static string BaseAddress = $"{ConfigurationManager.AppSettings["BaseAddress"]}/Trades";
        public static async Task<bool> AutoRefresh()
        {
            var result = await ApiHelper.Call(ApiHelper.HttpMethods.Get, $"{BaseAddress}/AutoRefreshSignals");
            return result.IsSuccessfulResult();
        }
    }
}
