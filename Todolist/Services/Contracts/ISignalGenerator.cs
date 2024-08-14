using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Todolist.Models.TradeModels;

namespace Todolist.Services.Contracts
{
    public interface ISignalGenerator
    {
        void AutoRefreshSignals();
        void SeedSignals(Symbol symbol, Timeframe timeframe, bool clearPrevious = false);
        void GetSignalsWithResult(Symbol symbol, Timeframe timeframe, SignalProviders provider);
        void GetSignalReport(Symbol symbol, Timeframe timeframe, SignalProviders provider);
        string GetSignalEstimates(Symbol symbol, Timeframe timeframe, int CandleCounts = 10000);
    }
}
