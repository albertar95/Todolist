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
        public static async Task<Tuple<bool,string>> AutoRefresh()
        {
            var result = await ApiHelper.Call(ApiHelper.HttpMethods.Post, $"{BaseAddress}/AutoRefreshCandles");
            if (result.IsSuccessfulResult())
            {
                var result2 = ApiHelper.Deserialize<JsonResults>(result.Content);
                return new Tuple<bool, string>(result2.hasError,result2.message);
            }
            else
                return new Tuple<bool, string>(true,"http error");
        }
    }
}
