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
        void AutoRefreshSignals(SignalProviders provider = SignalProviders.MaStrategyRevision);
        void SeedSignals(Symbol symbol, Timeframe timeframe, bool clearPrevious = false, SignalProviders provider = SignalProviders.MaStrategyRevision);
        string GetSignalsWithResult(Symbol symbol, Timeframe timeframe, SignalProviders provider = SignalProviders.MaStrategyRevision);
        void GetSignalReport(Symbol symbol, Timeframe timeframe, SignalProviders provider);
        string GetSignalEstimates(Symbol symbol, Timeframe timeframe, int CandleCounts = 10000);
        void DeleteSignals(Symbol symbol, Timeframe timeframe, SignalProviders provider = SignalProviders.MaStrategyRevision);
        //void ProcessHighsAndLows(Symbol symbol, Timeframe timeframe);
    }
}
