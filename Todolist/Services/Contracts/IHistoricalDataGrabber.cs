using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static Todolist.Models.TradeModels;

namespace Todolist.Services.Contracts
{
    public interface IHistoricalDataGrabber
    {
        void AutoRefreshCandles();
        void RefreshCandles(Symbol sym, Timeframe tf);
        void UpdateData(Symbol symbol, Timeframe tf);
        void SeedData(Symbol symbol, Timeframe tf);
        Task<bool> GetLastHourData(Symbol symbol, Timeframe timeframe);
    }
}