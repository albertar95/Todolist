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
        private readonly List<string> NotifyEmails;

        public static Signal currentSignal;
        public static float minSlTpPercentage;
        public static float maxSlTpPercentage;
        public static float FixedSlPercentage;
        public static float FixedTpPercentage;
        public static float EvenSignalResultLimit;
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
            NotifyEmails = getNotifyEmails(ConfigurationManager.AppSettings["NotifyEmails"]);
            currentSignal = new Signal();
            minSlTpPercentage = float.Parse(ConfigurationManager.AppSettings["minSlTpPercentage"]);
            maxSlTpPercentage = float.Parse(ConfigurationManager.AppSettings["maxSlTpPercentage"]);
            FixedSlPercentage = float.Parse(ConfigurationManager.AppSettings["FixedSlPercentage"]);
            FixedTpPercentage = float.Parse(ConfigurationManager.AppSettings["FixedTpPercentage"]);
            EvenSignalResultLimit = float.Parse(ConfigurationManager.AppSettings["EvenSignalResultLimit"]);
            CurrentSmaPosition = SMAPositions.Equal;
            IsSignalProcessInitialized = false;
            ProcessedCandleCheckpoint = DateTime.MinValue;
            SignalStatus = SignalCreationStatus.Observing;
        }
        #region CoreMethods
        public void AutoRefreshSignals()
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
                var lastResult = _dbRepository.GetMax<SignalResult, DateTime>(p => p.CloseDate,q => q.Signal.Symbol == (int)symbol && q.Signal.Timeframe == (int)timeframe);
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
        public static List<string> getNotifyEmails(string input)
        {
            List<string> result = new List<string>();
            input.ToLower().Split(',').ToList().ForEach(x => { result.Add(x); });
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
                }
                ProcessedCandleCheckpoint = estimates.OrderByDescending(p => p.Key.Time).FirstOrDefault().Key.Time;
            }
        }
        public void SignalGenerator2(KeyValuePair<AugmentedCandle, SignalEstimate> estimates)
        {
            if (estimates.Value.LinesPosition == LinesPositions.UpperProvider 
                && (estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothBellowBaseLine
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothNearBaseLine
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdNearBaseLineAndSignalBellow
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdUpperBaseLineAndSignalBellow
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalNearBaseLineAndMacdBellow
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalUpperBaseLineAndMacdBellow))
                FollowBullishCross(estimates);
            if (estimates.Value.LinesPosition == LinesPositions.UpperSignal
                && (estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothUpperBaseLine
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothNearBaseLine
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdNearBaseLineAndSignalUpper
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdUpperBaseLineAndSignalBellow
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalNearBaseLineAndMacdUpper
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalUpperBaseLineAndMacdBellow))
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
                newSignal.StopLostPrice = CalcCurrentSL(est.Key.Close, SignalTypes.Bullish);
                newSignal.TakeProfitPrice = CalcCurrentTP(est.Key.Close, SignalTypes.Bullish);
                if (est.Value.HistogramPosition == HistogramPositions.UpperBaseLineAscending ||
                    est.Value.HistogramPosition == HistogramPositions.BellowBaseLineAscending)//optional forth criteria
                    winchance += 10;
                newSignal.WinChanceEstimate = winchance;
                _dbRepository.Add(newSignal);
                SignalStatus = SignalCreationStatus.BullSignalCreated;
                currentSignal = newSignal;
                NotifyEmails.ForEach(x => { NotifyHelper.SendMail(GetEmailMessage(est), "bullish signal occured", x); });
            }else
                NotifyEmails.ForEach(x => { NotifyHelper.SendMail(GetEmailMessage(est), "bullish cross occured", x); });
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
                newSignal.StopLostPrice = CalcCurrentSL(est.Key.Close, SignalTypes.Bearish);
                newSignal.TakeProfitPrice = CalcCurrentTP(est.Key.Close, SignalTypes.Bearish);
                if (est.Value.HistogramPosition == HistogramPositions.UpperBaseLineDescending ||
                    est.Value.HistogramPosition == HistogramPositions.BellowBaseLineDescending)//optional forth criteria
                    winchance += 10;
                newSignal.WinChanceEstimate = winchance;
                _dbRepository.Add(newSignal);
                SignalStatus = SignalCreationStatus.BearSignalCreated;
                currentSignal = newSignal;
                NotifyEmails.ForEach(x => { NotifyHelper.SendMail(GetEmailMessage(est), "bearish signal occured", x); });
            }else
                NotifyEmails.ForEach(x => { NotifyHelper.SendMail(GetEmailMessage(est), "bearish cross occured", x); });
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
            else if (est.Key.Close >= currentSignal.TakeProfitPrice || est.Key.High >= currentSignal.TakeProfitPrice)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.tpHitted;
            }
            else if (est.Key.Close <= currentSignal.StopLostPrice || est.Key.Low <= currentSignal.StopLostPrice)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.slHitted;
            }
            if (IsClosure)
            {
                if (est.Key.Time < currentSignal.StartDate)
                    return;
                var newSignalResult = new SignalResult()
                {
                    Id = Guid.NewGuid(),
                    CreateDate = DateTime.Now,
                    SignalId = currentSignal.Id,
                    CloseDate = est.Key.Time,
                    ClosePrice = est.Key.High > est.Key.Close ? est.Key.High : est.Key.Close,
                    ClosureType = (int)res,
                    //Duration = (est.Key.Time - currentSignal.StartDate).Duration().Minutes,
                    Duration = Convert.ToInt32(est.Key.Time.Subtract(currentSignal.StartDate).TotalMinutes),
                    ProfitPercentage = est.Key.High > est.Key.Close ? CommonTradeOperations.CalcProfit(currentSignal.EnterPrice, est.Key.High, currentSignal.StopLostPrice, (SignalTypes)currentSignal.SignalType)
                    : CommonTradeOperations.CalcProfit(currentSignal.EnterPrice, est.Key.Close, currentSignal.StopLostPrice, (SignalTypes)currentSignal.SignalType)
                };
                if (newSignalResult.Duration <= 0)
                    newSignalResult.Status = (int)SignalResultStatus.equal;
                else
                {
                    if (newSignalResult.ProfitPercentage <= EvenSignalResultLimit && newSignalResult.ProfitPercentage >= 0)
                        newSignalResult.Status = (int)SignalResultStatus.equal;
                    else if (newSignalResult.ProfitPercentage >= (EvenSignalResultLimit*-1) && newSignalResult.ProfitPercentage <= 0)
                        newSignalResult.Status = (int)SignalResultStatus.equal;
                    else if(newSignalResult.ProfitPercentage <= (EvenSignalResultLimit * -1) && newSignalResult.ProfitPercentage <= 0)
                        newSignalResult.Status = (int)SignalResultStatus.unsuccessful;
                    else if (newSignalResult.ProfitPercentage >= EvenSignalResultLimit && newSignalResult.ProfitPercentage >= 0)
                        newSignalResult.Status = (int)SignalResultStatus.successful;
                }
                if (_dbRepository.Add(newSignalResult))
                {
                    currentSignal.IsActive = false;
                    _dbRepository.Update(currentSignal);
                    SignalStatus = SignalCreationStatus.Observing;
                    NotifyEmails.ForEach(x => { NotifyHelper.SendMail(GetEmailMessage(est) + $"terminate type : {res.ToString()}", "bullish signal terminated", x); });
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
            else if (est.Key.Close <= currentSignal.TakeProfitPrice || est.Key.Low <= currentSignal.TakeProfitPrice)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.tpHitted;
            }
            else if (est.Key.Close >= currentSignal.StopLostPrice || est.Key.High >= currentSignal.StopLostPrice)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.slHitted;
            }
            if (IsClosure)
            {
                if(est.Key.Time < currentSignal.StartDate)
                    return;
                var newSignalResult = new SignalResult()
                {
                    Id = Guid.NewGuid(),
                    CreateDate = DateTime.Now,
                    SignalId = currentSignal.Id,
                    CloseDate = est.Key.Time,
                    ClosePrice = est.Key.Low < est.Key.Close ? est.Key.Low : est.Key.Close,
                    ClosureType = (int)res,
                    //Duration = (est.Key.Time - currentSignal.StartDate).Duration().Minutes,
                    Duration = Convert.ToInt32(est.Key.Time.Subtract(currentSignal.StartDate).TotalMinutes),
                    ProfitPercentage = est.Key.Low < est.Key.Close ? CommonTradeOperations.CalcProfit(currentSignal.EnterPrice, est.Key.Low, currentSignal.StopLostPrice, (SignalTypes)currentSignal.SignalType)
                    : CommonTradeOperations.CalcProfit(currentSignal.EnterPrice, est.Key.Close, currentSignal.StopLostPrice, (SignalTypes)currentSignal.SignalType)
                };
                if (newSignalResult.Duration <= 0)
                    newSignalResult.Status = (int)SignalResultStatus.equal;
                else
                {
                    if (newSignalResult.ProfitPercentage <= EvenSignalResultLimit && newSignalResult.ProfitPercentage >= 0)
                        newSignalResult.Status = (int)SignalResultStatus.equal;
                    else if (newSignalResult.ProfitPercentage >= (EvenSignalResultLimit * -1) && newSignalResult.ProfitPercentage <= 0)
                        newSignalResult.Status = (int)SignalResultStatus.equal;
                    else if (newSignalResult.ProfitPercentage <= (EvenSignalResultLimit * -1) && newSignalResult.ProfitPercentage <= 0)
                        newSignalResult.Status = (int)SignalResultStatus.unsuccessful;
                    else if (newSignalResult.ProfitPercentage >= EvenSignalResultLimit && newSignalResult.ProfitPercentage >= 0)
                        newSignalResult.Status = (int)SignalResultStatus.successful;
                }
                if (_dbRepository.Add(newSignalResult))
                {
                    currentSignal.IsActive = false;
                    _dbRepository.Update(currentSignal);
                    SignalStatus = SignalCreationStatus.Observing;
                    NotifyEmails.ForEach(x => { NotifyHelper.SendMail(GetEmailMessage(est) + $"terminate type : {res.ToString()}", "bearish signal terminated", x); });
                }
            }
        }
        public double CalcCurrentSL(double close, SignalTypes signalTypes)
        {
            switch (signalTypes)
            {
                case SignalTypes.Bullish:
                    return close - CalcSLTPWithPercentage(close, FixedSlPercentage);
                case SignalTypes.Bearish:
                    return close + CalcSLTPWithPercentage(close, FixedSlPercentage);
                default:
                    return CalcSLTPWithPercentage(close, FixedSlPercentage);
            }
        }
        public double CalcCurrentTP(double close, SignalTypes signalTypes)
        {
            switch (signalTypes)
            {
                case SignalTypes.Bullish:
                    return close + CalcSLTPWithPercentage(close, FixedTpPercentage);
                case SignalTypes.Bearish:
                    return close - CalcSLTPWithPercentage(close, FixedTpPercentage);
                default:
                    return CalcSLTPWithPercentage(close, FixedTpPercentage);
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
        private double CalcSLTPWithPercentage(double input,double percentage)
        {
            return (input * percentage) / 100F;
        }
        private string GetEmailMessage(KeyValuePair<AugmentedCandle, SignalEstimate> est)
        {
            return $"candle time : {est.Key.Time}{Environment.NewLine}symbol : {Enum.GetName(typeof(Symbol), est.Key.Symbol)}{Environment.NewLine}timeframe : {Enum.GetName(typeof(Timeframe), est.Key.Timeframe)}{Environment.NewLine}";
        }
        #endregion
    }
}