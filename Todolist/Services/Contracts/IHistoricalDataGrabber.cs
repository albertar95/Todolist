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
        void AutoRefreshCandles();
        DateTime RefreshCandles(Symbol sym, Timeframe tf);
        void UpdateData(Symbol symbol, Timeframe tf, AugmentedCandle lastCandle);
        void SeedData(Symbol symbol, Timeframe tf);
        Task<bool> GetLastHourData(Symbol symbol, Timeframe timeframe, AugmentedCandle lastCandle);
        DateTime GetLastCandle(Symbol sym, Timeframe tf);
    }
}