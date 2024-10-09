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
        public bool EnableNotify;
        public bool RsiBullSaturation;
        public bool RsiBearSaturation;

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
            EnableNotify = bool.Parse(ConfigurationManager.AppSettings["EnableNotify"]);
            CurrentSmaPosition = SMAPositions.Equal;
            IsSignalProcessInitialized = false;
            ProcessedCandleCheckpoint = DateTime.MinValue;
            SignalStatus = SignalCreationStatus.Observing;
            RsiBearSaturation = false;
            RsiBullSaturation = false;
        }
        #region CoreMethods
        public void AutoRefreshSignals(SignalProviders provider = SignalProviders.MaStrategyRevision)
        {
            foreach (var sym in Symbol)
            {
                foreach (var tf in Timeframes)
                {
                    if (_dbRepository.Any<Signal>(p => p.Symbol == (int)sym && p.Timeframe == (int)tf 
                    && p.SignalProvider == (int)provider))
                        UpdateSignals(sym, tf,provider);
                    else
                        SeedSignals(sym, tf,false,provider);
                }
            }
        }
        public void SeedSignals(Symbol symbol, Timeframe timeframe, bool clearPrevious = false, SignalProviders provider = SignalProviders.MaStrategyRevision)
        {
            if (clearPrevious)
                DeleteSignals(symbol, timeframe, provider);
            var allCandles = _dbRepository.GetList<AugmentedCandle>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe, 10000).OrderBy(q => q.Time).ToList();
            var LastsignalEstimates = CommonTradeOperations.GenerateSignalEstimates(allCandles);
            EnableNotify = false;
            GenerateSignalAndFollow(LastsignalEstimates,provider);
        }
        public void UpdateSignals(Symbol symbol, Timeframe timeframe, SignalProviders provider = SignalProviders.MaStrategyRevision)
        {
            List<AugmentedCandle> allCandles = new List<AugmentedCandle>();
            var activeSignals = _dbRepository.GetList<Signal>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe 
            && p.SignalProvider == (int)provider && p.IsActive == true);
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
                var lastResult = _dbRepository.GetMax<SignalResult, DateTime>(p => p.CloseDate,q => q.Signal.Symbol == (int)symbol 
                && q.Signal.Timeframe == (int)timeframe && q.Signal.SignalProvider == (int)provider);
                if (lastResult != null)
                    ProcessedCandleCheckpoint = lastResult.CloseDate;
                else
                    ProcessedCandleCheckpoint = DateTime.MinValue;
            }
            allCandles = _dbRepository.GetList<AugmentedCandle>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.Time > ProcessedCandleCheckpoint, 10000).OrderBy(q => q.Time).ToList();
            var LastsignalEstimates = CommonTradeOperations.GenerateSignalEstimates(allCandles);
            GenerateSignalAndFollow(LastsignalEstimates,provider);
        }
        public string GetSignalsWithResult(Symbol symbol, Timeframe timeframe, SignalProviders provider = SignalProviders.MaStrategyRevision)
        {
            return CommonTradeOperations.DownloadSignals(_dbRepository.GetList<SignalResult>(p => p.Signal.Timeframe == (int)timeframe 
            && p.Signal.Symbol == (int)symbol && p.Signal.SignalProvider == (int)provider));
        }
        public void GetSignalReport(Symbol symbol, Timeframe timeframe, SignalProviders provider)
        {
            CommonTradeOperations.SignalReport(_dbRepository.GetList<SignalResult>(p => p.Signal.Timeframe == (int)timeframe 
            && p.Signal.Symbol == (int)symbol && p.Signal.SignalProvider == (int)provider));
        }
        public string GetSignalEstimates(Symbol symbol,Timeframe timeframe,int CandleCounts = 10000)
        {
            string result = "time,open,high,low,close,ema,macdline,signalline,histogram,rsi,CandlesToEMAPosition,LinesPosition,LinesOnMacdMapPosition" + Environment.NewLine;
            var allCandles = _dbRepository.GetList<AugmentedCandle>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe, CandleCounts).OrderBy(q => q.Time).ToList();
            var LastsignalEstimates = CommonTradeOperations.GenerateSignalEstimates(allCandles);
            LastsignalEstimates.ToList().ForEach(x => { result += CommonTradeOperations.CastCandleEstimateToCsv(x) + Environment.NewLine; });
            return result;
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
        public void GenerateSignalAndFollow(Dictionary<AugmentedCandle, SignalEstimate> estimates, SignalProviders provider = SignalProviders.MaStrategyRevision)
        {
            if (estimates.Count != 0)
            {
                foreach (var est in estimates.OrderBy(p => p.Key.Time))
                {
                    switch (provider)
                    {
                        case SignalProviders.MaStrategyRevision:
                            if (SignalStatus == SignalCreationStatus.Observing)
                                MaSignalGenerator(est);
                            else
                                MaFollowActiveSignals(est);
                            break;
                        case SignalProviders.RsiStrategy:
                            if (SignalStatus == SignalCreationStatus.Observing)
                                RsiSignalGenerator(est);
                            else
                                RsiFollowActiveSignals(est);
                            break;
                    }
                }
                ProcessedCandleCheckpoint = estimates.OrderByDescending(p => p.Key.Time).FirstOrDefault().Key.Time;
            }
        }
        public double CalcCurrentSL(double close, SignalTypes signalTypes)
        {
            switch (signalTypes)
            {
                case SignalTypes.Bullish:
                    return close - CalcSLTPWithPercentage(close, true);
                case SignalTypes.Bearish:
                    return close + CalcSLTPWithPercentage(close, true);
                default:
                    return CalcSLTPWithPercentage(close, true);
            }
        }
        public double CalcCurrentTP(double close, SignalTypes signalTypes)
        {
            switch (signalTypes)
            {
                case SignalTypes.Bullish:
                    return close + CalcSLTPWithPercentage(close, false);
                case SignalTypes.Bearish:
                    return close - CalcSLTPWithPercentage(close, false);
                default:
                    return CalcSLTPWithPercentage(close, false);
            }
        }
        public void DeleteSignals(Symbol symbol, Timeframe timeframe, SignalProviders provider = SignalProviders.MaStrategyRevision)
        {
            _dbRepository.DeleteBatch(_dbRepository.GetList<SignalResult>(p => p.Signal.Symbol == (int)symbol && p.Signal.Timeframe == (int)timeframe && p.Signal.SignalProvider == (int)provider));
            _dbRepository.DeleteBatch(_dbRepository.GetList<Signal>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.SignalProvider == (int)provider));
        }
        private double CalcSLTPWithPercentage(double input,bool IsSl)
        {
            if(IsSl)
                return (input * FixedSlPercentage) / 100F;
            else
                return (input * FixedSlPercentage * FixedTpPercentage) / 100F;
        }
        private string GetEmailMessage(NotifyType typo, KeyValuePair<AugmentedCandle, SignalEstimate> est, SignalTypes signalType, SignalResultClosureTypes closure = SignalResultClosureTypes.closedInMiddleByProvider)
        {
            var message = "";
            message += $"notify type :{typo.ToString()}{Environment.NewLine}";
            message += $"signal type : {signalType.ToString()}{Environment.NewLine}";
            message += $"candle time : {est.Key.Time}{Environment.NewLine}";
            message += $"symbol : {Enum.GetName(typeof(Symbol), est.Key.Symbol)}{Environment.NewLine}";
            message += $"timeframe : {Enum.GetName(typeof(Timeframe), est.Key.Timeframe)}{Environment.NewLine}";
            message += $"line position : {est.Value.LinesPosition.ToString()}{Environment.NewLine}";
            message += $"line on macd position : {est.Value.LinesOnMacdMapPosition.ToString()}{Environment.NewLine}";
            message += $"candles to ema position : {est.Value.CandlesToEMAPosition.ToString()}{Environment.NewLine}";
            if(typo == NotifyType.Terminate)
                message += $"terminate type : {closure.ToString()}{Environment.NewLine}";
            return message;
        }
        private void SignalNotify(NotifyType typo, KeyValuePair<AugmentedCandle, SignalEstimate> est,SignalTypes signalType, SignalResultClosureTypes closure = SignalResultClosureTypes.closedInMiddleByProvider)
        {
            if (!EnableNotify)
                return;
            bool notifyUpdated = false;
            if(typo == NotifyType.preSignal)
            {
                var lastNotify = _dbRepository.GetMax<NotifyLog>(p => p.Symbol == est.Key.Symbol && p.Timeframe == est.Key.Timeframe);
                if (lastNotify != null)
                {
                    if (est.Key.Time <= lastNotify.LastNotify)
                        return;
                }
            }
            NotifyEmails.ForEach(x => {
                if (NotifyHelper.SendMail(GetEmailMessage(typo, est,signalType,closure), DateTime.Now.ToLongTimeString(), x))
                {
                    if (!notifyUpdated)
                    {
                        var notifylog = _dbRepository.GetMax<NotifyLog>(p => p.Symbol == est.Key.Symbol && p.Timeframe == est.Key.Timeframe);
                        if (notifylog != null)
                        {
                            notifylog.LastNotify = est.Key.Time;
                            _dbRepository.Update(notifylog);
                        }
                        else
                            _dbRepository.Add(new NotifyLog() { Id = Guid.NewGuid(), LastNotify = est.Key.Time, Symbol = est.Key.Symbol, Timeframe = est.Key.Timeframe });
                        notifyUpdated = true;
                    }
                }
            });
        }
        #endregion
        #region RsiStrategyMethods
        public void RsiSignalGenerator(KeyValuePair<AugmentedCandle, SignalEstimate> estimates)
        {
            if (estimates.Key.RSI <= 30)
            {
                RsiBearSaturation = true;
                RsiBullSaturation = false;
            }
            else if (estimates.Key.RSI >= 70)
            {
                RsiBearSaturation = false;
                RsiBullSaturation = true;
            }

            if (RsiBearSaturation)
            {
                RsiFollowBullishCross(estimates);
            }else if(RsiBullSaturation)
                RsiFollowBearishCross(estimates);
        }
        public void RsiFollowBullishCross(KeyValuePair<AugmentedCandle, SignalEstimate> est)
        {
            if (est.Value.LinesPosition == LinesPositions.UpperProvider
                && (est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothBellowBaseLine
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothNearBaseLine
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdNearBaseLineAndSignalBellow
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdUpperBaseLineAndSignalBellow
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalNearBaseLineAndMacdBellow
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalUpperBaseLineAndMacdBellow))//second criteria met
            {
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
                newSignal.SignalProvider = (int)SignalProviders.RsiStrategy;
                newSignal.StopLostPrice = CalcCurrentSL(est.Key.Close, SignalTypes.Bullish);
                newSignal.TakeProfitPrice = CalcCurrentTP(est.Key.Close, SignalTypes.Bullish);
                newSignal.WinChanceEstimate = 75;
                _dbRepository.Add(newSignal);
                SignalStatus = SignalCreationStatus.BullSignalCreated;
                currentSignal = newSignal;
                SignalNotify(NotifyType.Signal, est, SignalTypes.Bullish);
            }
            //else
            //    SignalNotify(NotifyType.preSignal, est, SignalTypes.Bullish);
        }
        public void RsiFollowBearishCross(KeyValuePair<AugmentedCandle, SignalEstimate> est)
        {
            if (est.Value.LinesPosition == LinesPositions.UpperSignal
                && (est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothUpperBaseLine
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothNearBaseLine
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdNearBaseLineAndSignalUpper
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdUpperBaseLineAndSignalBellow
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalNearBaseLineAndMacdUpper
                || est.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalUpperBaseLineAndMacdBellow))//second criteria met
            {
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
                newSignal.SignalProvider = (int)SignalProviders.RsiStrategy;
                newSignal.StopLostPrice = CalcCurrentSL(est.Key.Close, SignalTypes.Bearish);
                newSignal.TakeProfitPrice = CalcCurrentTP(est.Key.Close, SignalTypes.Bearish);
                newSignal.WinChanceEstimate = 75;
                _dbRepository.Add(newSignal);
                SignalStatus = SignalCreationStatus.BearSignalCreated;
                currentSignal = newSignal;
                SignalNotify(NotifyType.Signal, est, SignalTypes.Bearish);
            }
            //else
            //    SignalNotify(NotifyType.preSignal, est, SignalTypes.Bearish);
        }
        public void RsiFollowActiveSignals(KeyValuePair<AugmentedCandle, SignalEstimate> estimate, bool IsTerminator = false)
        {
            switch ((SignalTypes)currentSignal.SignalType)
            {
                case SignalTypes.Bullish:
                    RsiFollowBullishSignal(estimate, currentSignal, IsTerminator);
                    break;
                case SignalTypes.Bearish:
                    RsiFollowBearishSignal(estimate, currentSignal, IsTerminator);
                    break;
            }
        }
        public void RsiFollowBullishSignal(KeyValuePair<AugmentedCandle, SignalEstimate> est, Signal currentSignal, bool IsTerminator = false)
        {
            bool IsClosure = false;
            SignalResultClosureTypes res = SignalResultClosureTypes.closedInMiddleByProvider;
            if (est.Key.RSI >= 70 || IsTerminator)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.closedInMiddleByProvider;
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
                if (newSignalResult.ProfitPercentage < (FixedSlPercentage * -1))
                    newSignalResult.ProfitPercentage = (FixedSlPercentage * -1);
                if (newSignalResult.Duration <= 0)
                {
                    newSignalResult.Status = (int)SignalResultStatus.equal;
                    newSignalResult.ProfitPercentage = 0;
                }
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
                    SignalNotify(NotifyType.Terminate, est, SignalTypes.Bullish, res);
                }
                if(est.Key.RSI >= 70)
                    RsiBullSaturation = true;
                else
                    RsiBullSaturation = false;
                RsiBearSaturation = false;
            }
        }
        public void RsiFollowBearishSignal(KeyValuePair<AugmentedCandle, SignalEstimate> est, Signal currentSignal, bool IsTerminator = false)
        {
            bool IsClosure = false;
            SignalResultClosureTypes res = SignalResultClosureTypes.closedInMiddleByProvider;
            if (est.Key.RSI <= 30 || IsTerminator)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.closedInMiddleByProvider;
            }
            else if (est.Key.Close >= currentSignal.StopLostPrice || est.Key.High >= currentSignal.StopLostPrice)
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
                    ClosePrice = est.Key.Low < est.Key.Close ? est.Key.Low : est.Key.Close,
                    ClosureType = (int)res,
                    //Duration = (est.Key.Time - currentSignal.StartDate).Duration().Minutes,
                    Duration = Convert.ToInt32(est.Key.Time.Subtract(currentSignal.StartDate).TotalMinutes),
                    ProfitPercentage = est.Key.Low < est.Key.Close ? CommonTradeOperations.CalcProfit(currentSignal.EnterPrice, est.Key.Low, currentSignal.StopLostPrice, (SignalTypes)currentSignal.SignalType)
                    : CommonTradeOperations.CalcProfit(currentSignal.EnterPrice, est.Key.Close, currentSignal.StopLostPrice, (SignalTypes)currentSignal.SignalType)
                };
                if (newSignalResult.ProfitPercentage < (FixedSlPercentage * -1))
                    newSignalResult.ProfitPercentage = (FixedSlPercentage * -1);
                if (newSignalResult.Duration <= 0)
                {
                    newSignalResult.Status = (int)SignalResultStatus.equal;
                    newSignalResult.ProfitPercentage = 0;
                }
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
                    SignalNotify(NotifyType.Terminate, est, SignalTypes.Bearish, res);
                }
                if(est.Key.RSI <= 30)
                    RsiBearSaturation = true;
                else
                    RsiBearSaturation = false;

                RsiBullSaturation = false;
            }
        }
        #endregion
        #region MaStrategyMethods
        public void MaSignalGenerator(KeyValuePair<AugmentedCandle, SignalEstimate> estimates)
        {
            if (estimates.Value.LinesPosition == LinesPositions.UpperProvider
                && (estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothBellowBaseLine
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothNearBaseLine
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdNearBaseLineAndSignalBellow
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdUpperBaseLineAndSignalBellow
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalNearBaseLineAndMacdBellow
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalUpperBaseLineAndMacdBellow))
                MaFollowBullishCross(estimates);
            if (estimates.Value.LinesPosition == LinesPositions.UpperSignal
                && (estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothUpperBaseLine
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.BothNearBaseLine
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdNearBaseLineAndSignalUpper
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.MacdUpperBaseLineAndSignalBellow
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalNearBaseLineAndMacdUpper
                || estimates.Value.LinesOnMacdMapPosition == LinesOnMacdPositions.SignalUpperBaseLineAndMacdBellow))
                MaFollowBearishCross(estimates);
        }
        public void MaFollowBullishCross(KeyValuePair<AugmentedCandle, SignalEstimate> est)
        {
            if (est.Value.CandlesToEMAPosition == CandlesToEMAPositions.UpperCandle)//second criteria met
            {
                if (est.Key.Time.Hour >= 6 && est.Key.Time.Hour <= 21)
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
                    SignalNotify(NotifyType.Signal, est, SignalTypes.Bullish);
                }
            }
            //else
            //    SignalNotify(NotifyType.preSignal, est, SignalTypes.Bullish);
        }
        public void MaFollowBearishCross(KeyValuePair<AugmentedCandle, SignalEstimate> est)
        {
            if (est.Value.CandlesToEMAPosition == CandlesToEMAPositions.UpperEMA)//second criteria met
            {
                if (est.Key.Time.Hour >= 6 && est.Key.Time.Hour <= 21)
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
                    SignalNotify(NotifyType.Signal, est, SignalTypes.Bearish);
                }
            }
            //else
            //    SignalNotify(NotifyType.preSignal, est, SignalTypes.Bearish);
        }
        public void MaFollowActiveSignals(KeyValuePair<AugmentedCandle, SignalEstimate> estimate, bool IsTerminator = false)
        {
            switch ((SignalTypes)currentSignal.SignalType)
            {
                case SignalTypes.Bullish:
                    MaFollowBullishSignal(estimate, currentSignal, IsTerminator);
                    break;
                case SignalTypes.Bearish:
                    MaFollowBearishSignal(estimate, currentSignal, IsTerminator);
                    break;
            }
        }
        public void MaFollowBullishSignal(KeyValuePair<AugmentedCandle, SignalEstimate> est, Signal currentSignal, bool IsTerminator = false)
        {
            bool IsClosure = false;
            SignalResultClosureTypes res = SignalResultClosureTypes.closedInMiddleByProvider;
            if (est.Value.CandlesToEMAPosition == CandlesToEMAPositions.UpperEMA || IsTerminator)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.closedInMiddleByProvider;
            }
            //else if (est.Value.LinesPosition == LinesPositions.UpperSignal || IsTerminator)
            //{
            //    IsClosure = true;
            //    res = SignalResultClosureTypes.closedInMiddleByProvider;
            //}
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
                if (newSignalResult.ProfitPercentage > FixedTpPercentage)
                    newSignalResult.ProfitPercentage = FixedTpPercentage;
                if (newSignalResult.ProfitPercentage < (FixedSlPercentage * -1))
                    newSignalResult.ProfitPercentage = (FixedSlPercentage * -1);
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
                    SignalNotify(NotifyType.Terminate, est, SignalTypes.Bullish, res);
                }
            }
        }
        public void MaFollowBearishSignal(KeyValuePair<AugmentedCandle, SignalEstimate> est, Signal currentSignal, bool IsTerminator = false)
        {
            bool IsClosure = false;
            SignalResultClosureTypes res = SignalResultClosureTypes.closedInMiddleByProvider;
            if (est.Value.CandlesToEMAPosition == CandlesToEMAPositions.UpperCandle || IsTerminator)
            {
                IsClosure = true;
                res = SignalResultClosureTypes.closedInMiddleByProvider;
            }
            //else if (est.Value.LinesPosition == LinesPositions.UpperProvider || IsTerminator)
            //{
            //    IsClosure = true;
            //    res = SignalResultClosureTypes.closedInMiddleByProvider;
            //}
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
                if (est.Key.Time < currentSignal.StartDate)
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
                if (newSignalResult.ProfitPercentage > FixedTpPercentage)
                    newSignalResult.ProfitPercentage = FixedTpPercentage;
                if (newSignalResult.ProfitPercentage < (FixedSlPercentage * -1))
                    newSignalResult.ProfitPercentage = (FixedSlPercentage * -1);
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
                    SignalNotify(NotifyType.Terminate, est, SignalTypes.Bearish, res);
                }
            }
        }
        #endregion
    }
}