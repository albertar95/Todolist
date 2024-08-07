using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Todolist.Models;
using static Todolist.Models.TradeModels;

namespace Todolist.Services.Contracts
{
    public interface IHistoricalDataGrabber
    {
        Tuple<bool, List<Tuple<int, string>>> AutoRefreshCandles();
        DateTime RefreshCandles(Symbol sym, Timeframe tf);
        bool UpdateData(Symbol symbol, Timeframe tf, AugmentedCandle lastCandle);
        bool SeedData(Symbol symbol, Timeframe tf);
        Task<Tuple<bool,string>> GetLastHourData(Symbol symbol, Timeframe timeframe, AugmentedCandle lastCandle);
        DateTime GetLastCandle(Symbol sym, Timeframe tf);
    }
}