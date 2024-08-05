using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingWindowsService
{
    public static class CandleHelper
    {
        private static string BaseAddress = $"{ConfigurationManager.AppSettings["BaseAddress"]}/Trades";
        public static async Task<bool> AutoRefresh()
        {
            var result = await ApiHelper.Call(ApiHelper.HttpMethods.Post, $"{BaseAddress}/AutoRefreshCandles");
            return result.IsSuccessfulResult();
        }
    }
}
