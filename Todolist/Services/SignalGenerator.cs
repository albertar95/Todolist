using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using Todolist.Helpers;
using Todolist.Models;
using Todolist.Services.Contracts;
using static Todolist.Models.TradeModels;

namespace Todolist.Services
{
    public class SignalGenerator : ISignalGenerator
    {
        #region Variables
        private readonly int keepDataInterval = int.Parse(ConfigurationManager.AppSettings["keepDataInterval"]);
        private readonly IDbRepository _dbRepository;
        private readonly List<Timeframe> Timeframes;
        private readonly List<Symbol> Symbol;

        public static Signal currentSignal;
        public double CurrentCeiling;
        public double CurrentFloor;
        public static float minSlTpPip;
        public static float maxSlTpPip;
        public static float FixedSlPip;
        public static float FixedTpPip;
        public SMAPositions CurrentSmaPosition;
        public bool IsSignalProcessInitialized;
        public DateTime ProcessedCandleCheckpoint;
        public SignalCreationStatus SignalStatus;

        #endregion
        public SignalGenerator(IDbRepository dbRepository)
        {
            _dbRepository = dbRepository;
            Timeframes = getTimeframes(ConfigurationManager.AppSettings["Timeframes"]);
            Symbol = getSymbol(ConfigurationManager.AppSettings["Symbol"]);
            currentSignal = new Signal();
            CurrentCeiling = 0F;
            CurrentFloor = 0F;
            minSlTpPip = float.Parse(ConfigurationManager.AppSettings["minSlTpPip"]) * 0.0001F;
            maxSlTpPip = float.Parse(ConfigurationManager.AppSettings["maxSlTpPip"]) * 0.0001F;
            FixedSlPip = float.Parse(ConfigurationManager.AppSettings["FixedSlPip"]) * 0.0001F;
            FixedTpPip = float.Parse(ConfigurationManager.AppSettings["FixedTpPip"]) * 0.0001F;
            CurrentSmaPosition = SMAPositions.Equal;
            IsSignalProcessInitialized = false;
            ProcessedCandleCheckpoint = DateTime.MinValue;
            SignalStatus = SignalCreationStatus.Observing;
        }
        #region CoreMethods
        public void Worker()
        {
            foreach (var sym in Symbol)
            {
                foreach (var tf in Timeframes)
                {
                    if (_dbRepository.Any<Signal>(p => p.Symbol == (int)sym && p.Timeframe == (int)tf && p.SignalProvider == (int)SignalProviders.MaStrategyRevision))
                        UpdateSignals(sym, tf);
                    else
                        SeedSignals(sym, tf);
                }
            }
        }
        public void SeedSignals(Symbol symbol, Timeframe timeframe, bool clearPrevious = false)
        {
            if (clearPrevious)
                DeleteSignals(symbol, timeframe, SignalProviders.MaStrategyRevision);
            var allCandles = _dbRepository.GetList<AugmentedCandle>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe, 10000).OrderBy(q => q.Time).ToList();
            var LastsignalEstimates = CommonTradeOperations.GenerateSignalEstimates(allCandles);
            GenerateSignalAndFollow(LastsignalEstimates);
        }
        public void UpdateSignals(Symbol symbol, Timeframe timeframe)
        {
            List<AugmentedCandle> allCandles = new List<AugmentedCandle>();
            var activeSignals = _dbRepository.GetList<Signal>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.SignalProvider == (int)SignalProviders.MaStrategyRevision && p.IsActive == true);
            if (activeSignals.Any())
            {
                currentSignal = activeSignals.OrderByDescending(p => p.StartDate).FirstOrDefault();
                SignalStatus = currentSignal.SignalType == (int)SignalTypes.Bearish ? SignalCreationStatus.BearSignalCreated : SignalCreationStatus.BullSignalCreated;
                ProcessedCandleCheckpoint = currentSignal.StartDate;
            }
            else
            {
                currentSignal = new Signal();
                SignalStatus = SignalCreationStatus.Observing;
                var lastResult = _dbRepository.GetMax<SignalResult, DateTime>(p => p.CloseDate,null);
                if (lastResult != null)
                    ProcessedCandleCheckpoint = lastResult.CloseDate;
                else
                    ProcessedCandleCheckpoint = DateTime.MinValue;
            }
            allCandles = _dbRepository.GetList<AugmentedCandle>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.Time > ProcessedCandleCheckpoint, 10000).OrderBy(q => q.Time).ToList();
            var LastsignalEstimates = CommonTradeOperations.GenerateSignalEstimates(allCandles);
            GenerateSignalAndFollow(LastsignalEstimates);
        }
        public void GetSignalsWithResult(Symbol symbol, Timeframe timeframe, SignalProviders provider)
        {
            CommonTradeOperations.DownloadSignals(_dbRepository.GetList<SignalResult>(p => p.Signal.Timeframe == (int)timeframe && p.Signal.Symbol == (int)symbol && p.Signal.SignalProvider == (int)provider));
        }
        public void GetSignalReport(Symbol symbol, Timeframe timeframe, SignalProviders provider)
        {
            CommonTradeOperations.SignalReport(_dbRepository.GetList<SignalResult>(p => p.Signal.Timeframe == (int)timeframe && p.Signal.Symbol == (int)symbol && p.Signal.SignalProvider == (int)provider));
        }
        #endregion
        #region PrivateMethods
        public static List<Timeframe> getTimeframes(string input)
        {
            List<Timeframe> result = new List<Timeframe>();
            input.ToLower().Split(',').ToList().ForEach(x => { result.Add((Timeframe)(int.Parse(x))); });
            return result;
        }
        public static List<Symbol> getSymbol(string input)
        {
            List<Symbol> result = new List<Symbol>();
            input.ToLower().Split(',').ToList().ForEach(x => { result.Add((Symbol)(int.Parse(x))); });
            return result;
        }
        public void GenerateSignalAndFollow(Dictionary<AugmentedCandle, SignalEstimate> estimates)
        {
            if (estimates.Count != 0)
            {
                foreach (var est in estimates.OrderBy(p => p.Key.Time))
                {
                    if (SignalStatus == SignalCreationStatus.Observing)
                        SignalGenerator2(est);
                    else
                        FollowActiveSignals(est);
                    if (est.Key.High > CurrentCeiling)
                        CurrentCeiling = est.Key.High;
                    if (est.Key.Low < CurrentFloor || CurrentFloor == 0F)
                        CurrentFloor = est.Key.Low;
                }
                ProcessedCandleCheckpoint = estimates.OrderByDescending(p => p.Key.Time).FirstOrDefault().Key.Time;
            }
        }
        public void SignalGenerator2(KeyValuePair<AugmentedCandle, SignalEstimate> estimates)
        {
            if (estimates.Value.LinesPosition == LinesPositions.UpperProvider)
                FollowBullishCross(estimates);
            if (estimates.Value.LinesPosition == LinesPositions.UpperSignal)
                FollowBearishCross(estimates);
        }
        public void FollowBullishCross(KeyValuePair<AugmentedCandle, SignalEstimate> est)
        {
            if (est.Value.CandlesToEMAPosition == CandlesToEMAPositions.UpperCandle)//second criteria met
            {
                byte winchance = 75;
                var newSignal = new Signal()
                {
                    Id = Guid.NewGuid(),
                    CreateDate = DateTime.Now,
                    EnterPrice = est.Key.Close,
                    IsActive = true,
                    StartDate = est.Key.Time,
                    Symbol = est.Key.Symbol,
                    Timeframe = est.Key.Timeframe,
                    SignalType = (int)SignalTypes.Bullish
                };
                newSignal.SignalProvider = (int)SignalProviders.MaStrategyRevision;
                newSignal.StopLostPrice = est.Key.Close - CalcCurrentSL(est.Key.Close, SignalTypes.Bullish);
                newSignal.TakeProfitPrice = est.Key.Close + CalcCurrentTP(est.Key.Close, SignalTypes.Bullish);
                if (est.Value.HistogramPosition == HistogramPositions.UpperBaseLineAscending ||
                    est.Value.HistogramPosition == HistogramPositions.BellowBaseLineAscending)//optional forth criteria
                    winchance += 10;
                newSignal.WinChanceEstimate = winchance;
                _dbRepository.Add(newSignal);
                SignalStatus = SignalCreationStatus.BullSignalCreated;
                currentSignal = newSignal;
            }
        }
        public void FollowBearishCross(KeyValuePair<AugmentedCandle, SignalEstimate> est)
        {
            if (est.Value.CandlesToEMAPosition == CandlesToEMAPositions.UpperEMA)//second criteria met
            {
                byte winchance = 75;
                var newSignal = new Signal()
                {
                    Id = Guid.NewGuid(),
                    CreateDate = DateTime.Now,
                    EnterPrice = est.Key.Close,
                    IsActive = true,
                    StartDate = est.Key.Time,
                    Symbol = est.Key.Symbol,
                    Timeframe = est.Key.Timeframe,
                    SignalType = (int)SignalTypes.Bearish
                };
                newSignal.SignalProvider = (int)SignalProviders.MaStrategyRevision;
                newSignal.StopLostPrice = est.Key.Close + CalcCurrentSL(est.Key.Close, SignalTypes.Bearish);
                newSignal.TakeProfitPrice = est.Key.Close - CalcCurrentTP(est.Key.Close, SignalTypes.Bearish);
                if (est.Value.HistogramPosition == HistogramPositions.UpperBaseLineDescending ||
                    est.Value.HistogramPosition == HistogramPositions.BellowBaseLineDescending)//optional forth criteria
                    winchance += 10;
                newSignal.WinChanceEstimate = winchance;
                _dbRepository.Add(newSignal);
                SignalStatus = SignalCreationStatus.BearSignalCreated;
                currentSignal = newSignal;
            }
        }
        public void FollowActiveSignals(KeyValuePair<AugmentedCandle, SignalEstimate> estimate, bool IsTerminator = false)
        {
            switch ((SignalTypes)currentSignal.SignalType)
            {
                case SignalTypes.Bullish:
                    FollowBullishSignal(estimate, currentSignal, IsTerminator);
                    break;
                case SignalTypes.Bearish:
                    FollowBearishSignal(estimate, currentSignal, IsTerminator);
                    break;
            }
        }
        public void FollowBullishSignal(KeyValuePair<AugmentedCandle, SignalEstimate> est, Signal currentSignal, bool IsTerminator = false)
        {
            bool IsClosure = false;
            SignalResultClosureTypes res = SignalResultClosureTypes.closedInMiddleByProvider;
            if (est.Value.CandlesToEMAPosition == CandlesToEMAPositions.UpperEMA || IsTerminator)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.closedInMiddleByProvider;
            }
            else if (est.Value.LinesPosition == LinesPositions.UpperSignal || IsTerminator)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.closedInMiddleByProvider;
            }
            else if (est.Key.Close >= currentSignal.TakeProfitPrice)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.tpHitted;
            }
            else if (est.Key.Close <= currentSignal.StopLostPrice)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.slHitted;
            }
            if (IsClosure)
            {
                var newSignalResult = new SignalResult()
                {
                    Id = Guid.NewGuid(),
                    CreateDate = DateTime.Now,
                    SignalId = currentSignal.Id,
                    CloseDate = est.Key.Time,
                    ClosePrice = est.Key.Close,
                    ClosureType = (int)res,
                    //Duration = (est.Key.Time - currentSignal.StartDate).Duration().Minutes,
                    Duration = Convert.ToInt32(est.Key.Time.Subtract(currentSignal.StartDate).TotalMinutes),
                    ProfitPercentage = CommonTradeOperations.CalcProfit(currentSignal.EnterPrice, est.Key.Close, currentSignal.StopLostPrice, (SignalTypes)currentSignal.SignalType)
                };
                if (newSignalResult.Duration <= 0)
                    newSignalResult.Status = (int)SignalResultStatus.equal;
                else
                    newSignalResult.Status = newSignalResult.ProfitPercentage >= 0 ? (int)SignalResultStatus.successful : (int)SignalResultStatus.unsuccessful;
                if (_dbRepository.Add(newSignalResult))
                {
                    currentSignal.IsActive = false;
                    _dbRepository.Update(currentSignal);
                    SignalStatus = SignalCreationStatus.Observing;
                }
            }
        }
        public void FollowBearishSignal(KeyValuePair<AugmentedCandle, SignalEstimate> est, Signal currentSignal, bool IsTerminator = false)
        {
            bool IsClosure = false;
            SignalResultClosureTypes res = SignalResultClosureTypes.closedInMiddleByProvider;
            if (est.Value.CandlesToEMAPosition == CandlesToEMAPositions.UpperCandle || IsTerminator)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.closedInMiddleByProvider;
            }
            else if (est.Value.LinesPosition == LinesPositions.UpperProvider || IsTerminator)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.closedInMiddleByProvider;
            }
            else if (est.Key.Close <= currentSignal.TakeProfitPrice)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.tpHitted;
            }
            else if (est.Key.Close >= currentSignal.StopLostPrice)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.slHitted;
            }
            if (IsClosure)
            {
                var newSignalResult = new SignalResult()
                {
                    Id = Guid.NewGuid(),
                    CreateDate = DateTime.Now,
                    SignalId = currentSignal.Id,
                    CloseDate = est.Key.Time,
                    ClosePrice = est.Key.Close,
                    ClosureType = (int)res,
                    //Duration = (est.Key.Time - currentSignal.StartDate).Duration().Minutes,
                    Duration = Convert.ToInt32(est.Key.Time.Subtract(currentSignal.StartDate).TotalMinutes),
                    ProfitPercentage = CommonTradeOperations.CalcProfit(currentSignal.EnterPrice, est.Key.Close, currentSignal.StopLostPrice, (SignalTypes)currentSignal.SignalType)
                };
                newSignalResult.Status = newSignalResult.ProfitPercentage >= 0 ? (int)SignalResultStatus.successful : (int)SignalResultStatus.unsuccessful;
                if (_dbRepository.Add(newSignalResult))
                {
                    currentSignal.IsActive = false;
                    _dbRepository.Update(currentSignal);
                    SignalStatus = SignalCreationStatus.Observing;
                }
            }
        }
        public double CalcCurrentSL(double close, SignalTypes signalTypes)
        {
            switch (signalTypes)
            {
                case SignalTypes.Bullish:
                    if (Math.Abs(CurrentFloor - close) > minSlTpPip && Math.Abs(CurrentFloor - close) <= maxSlTpPip)
                        return CurrentFloor;
                    else
                        return FixedSlPip;
                case SignalTypes.Bearish:
                    if (Math.Abs(CurrentCeiling - close) > minSlTpPip && Math.Abs(CurrentCeiling - close) <= maxSlTpPip)
                        return CurrentCeiling;
                    else
                        return FixedSlPip;
                default:
                    return FixedSlPip;
            }
        }
        public double CalcCurrentTP(double close, SignalTypes signalTypes)
        {
            switch (signalTypes)
            {
                case SignalTypes.Bullish:
                    if (Math.Abs(CurrentFloor - close) > minSlTpPip && Math.Abs(CurrentFloor - close) <= maxSlTpPip)
                        return CurrentFloor * 2;
                    else
                        return FixedTpPip;
                case SignalTypes.Bearish:
                    if (Math.Abs(CurrentCeiling - close) > minSlTpPip && Math.Abs(CurrentCeiling - close) <= maxSlTpPip)
                        return CurrentCeiling * 2;
                    else
                        return FixedTpPip;
                default:
                    return FixedTpPip;
            }
        }
        public void DeleteSignals(Symbol symbol, Timeframe timeframe, SignalProviders provider)
        {
            foreach (var item in _dbRepository.GetList<SignalResult>(p => p.Signal.Symbol == (int)symbol && p.Signal.Timeframe == (int)timeframe && p.Signal.SignalProvider == (int)provider))
            {
                _dbRepository.Delete(item);
            }
            foreach (var item in _dbRepository.GetList<Signal>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.SignalProvider == (int)provider))
            {
                _dbRepository.Delete(item);
            }
        }
        #endregion
    }
}