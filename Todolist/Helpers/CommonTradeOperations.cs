using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Todolist.Models;
using Todolist.Models.Dto;
using static Todolist.Models.TradeModels;

namespace Todolist.Helpers
{
    public class CommonTradeOperations
    {
        public static string FileSourcePath = ConfigurationManager.AppSettings["FileSourcePath"];
        public static double linesClosenessMargin = float.Parse(ConfigurationManager.AppSettings["linesClosenessMargin"]);
        public static double linesOnMacdClosenessMargin = float.Parse(ConfigurationManager.AppSettings["linesOnMacdClosenessMargin"]);
        public static double smaClosenessMargin = float.Parse(ConfigurationManager.AppSettings["smaClosenessMargin"]);
        public static double smaAndCandleClosenessMargin = float.Parse(ConfigurationManager.AppSettings["smaAndCandleClosenessMargin"]);
        private static DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            return dateTime.AddMilliseconds(unixTimeStamp).ToUniversalTime();
        }
        public static long DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
        }
        public static DateTime UnixTimeStampToLocalDateTime(long unixTimeStamp)
        {
            return dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
        }
        public static long LocalDateTimeToUnixTimeStamp(DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();
        }
        public static double EmaCalculator(double TargetValue, double LastEma, double Period)
        {
            double alfa = 2F / (1F + Period);
            return (TargetValue * alfa) + (LastEma * (1F - alfa));
        }
        public static float RSICalculator(List<Candle> candles)
        {
            float sumGain = 0F;
            float sumLoss = 0F;
            for (int i = 1; i <= candles.Count - 1; i++)
            {
                if ((candles[i].Close - candles[i - 1].Close) > 0)
                    sumGain += candles[i].Close - candles[i - 1].Close;
                else
                    sumLoss += (candles[i].Close - candles[i - 1].Close) * -1;
            }
            var rs = sumGain / sumLoss;
            return 100 - (100 / (1 + rs));
        }
        public static LinesOnMacdPositions CalcLinesOnMacdMapPosition(double macd, double signal, double closenessMargin)
        {
            if (Math.Abs(macd) < closenessMargin && Math.Abs(signal) < closenessMargin)//near state
                return LinesOnMacdPositions.BothNearBaseLine;
            else if (Math.Abs(macd) > closenessMargin && Math.Abs(signal) < closenessMargin)//signal near state
            {
                if (macd > 0)
                    return LinesOnMacdPositions.SignalNearBaseLineAndMacdUpper;
                else
                    return LinesOnMacdPositions.SignalUpperBaseLineAndMacdBellow;
            }
            else if (Math.Abs(macd) < closenessMargin && Math.Abs(signal) > closenessMargin)//macd near state
            {
                if (signal > 0)
                    return LinesOnMacdPositions.MacdNearBaseLineAndSignalUpper;
                else
                    return LinesOnMacdPositions.MacdNearBaseLineAndSignalBellow;
            }
            else
            {
                if (macd < 0 && signal < 0)
                    return LinesOnMacdPositions.BothBellowBaseLine;
                else if (macd > 0 && signal < 0)
                    return LinesOnMacdPositions.MacdUpperBaseLineAndSignalBellow;
                else if (macd < 0 && signal > 0)
                    return LinesOnMacdPositions.SignalUpperBaseLineAndMacdBellow;
                else
                    return LinesOnMacdPositions.BothUpperBaseLine;
            }
        }
        public static LinesPositions CalcLinesPosition(double macd, double signal, double closenessMargin)
        {
            if (Math.Abs(macd - signal) < closenessMargin)
                return LinesPositions.Equal;
            else
            {
                if (macd > signal)
                    return LinesPositions.UpperProvider;
                else
                    return LinesPositions.UpperSignal;
            }
        }
        public static SMAPositions CalcSMAPosition(double sma50, double sma100, double closenessMargin)
        {
            if (Math.Abs(sma50 - sma100) < closenessMargin)
                return SMAPositions.Equal;
            else
            {
                if (sma50 > sma100)
                    return SMAPositions.Upper50;
                else
                    return SMAPositions.Upper100;
            }
        }
        public static CandlesToSMAPositions CalcCandlesToSMAPosition(double close, double sma50, double sma100, double closenessMargin)
        {
            double diff50 = Math.Abs(close - sma50);
            double diff100 = Math.Abs(close - sma100);
            if (diff50 < closenessMargin && diff100 < closenessMargin)//near state
                return CandlesToSMAPositions.CandlesNearBoth;
            else if (diff50 > closenessMargin && diff100 < closenessMargin)//signal near state
            {
                if (close >= sma50)
                    return CandlesToSMAPositions.CandlesNear100AndUpper50;
                else
                    return CandlesToSMAPositions.CandlesNear100AndBellow50;
            }
            else if (diff50 < closenessMargin && diff100 > closenessMargin)//macd near state
            {
                if (close >= sma100)
                    return CandlesToSMAPositions.CandlesNear50AndUpper100;
                else
                    return CandlesToSMAPositions.CandlesNear50AndBellow100;
            }
            else
            {
                if (close >= sma50 && close >= sma100)
                    return CandlesToSMAPositions.CandlesUpperBoth;
                else if (close >= sma50 && close <= sma100)
                    return CandlesToSMAPositions.CandlesUpper50AndBellow100;
                else if (close <= sma50 && close >= sma100)
                    return CandlesToSMAPositions.CandlesUpper100AndBellow50;
                else
                    return CandlesToSMAPositions.CandlesBellowBoth;
            }
        }
        public static CandlesToEMAPositions CalcCandlesToEMAPosition(double close, double ema50, double closenessMargin)
        {
            double diff50 = Math.Abs(close - ema50);
            if (diff50 < closenessMargin)//near state
                return CandlesToEMAPositions.Equal;
            else
            {
                if (close >= ema50)
                    return CandlesToEMAPositions.UpperCandle;
                else
                    return CandlesToEMAPositions.UpperEMA;
            }
        }
        public static HistogramPositions CalcHistogramPosition(double currentHistogram, double previousHistogram)
        {
            if (currentHistogram > 0)
            {
                if (currentHistogram >= previousHistogram)
                    return HistogramPositions.UpperBaseLineAscending;
                else
                    return HistogramPositions.UpperBaseLineDescending;
            }
            else
            {
                if (currentHistogram >= previousHistogram)
                    return HistogramPositions.BellowBaseLineAscending;
                else
                    return HistogramPositions.BellowBaseLineDescending;
            }
        }
        public static Dictionary<AugmentedCandle, SignalEstimate> GenerateSignalEstimates(List<AugmentedCandle> inputs
            , double m_linesOnMacdClosenessMargin, double m_linesClosenessMargin, double m_smaClosenessMargin
            , double m_smaAndCandleClosenessMargin)
        {
            Dictionary<AugmentedCandle, SignalEstimate> result = new Dictionary<AugmentedCandle, SignalEstimate>();
            var candleArray = inputs.OrderBy(p => p.Time).ToArray();
            for (int i = 0; i < candleArray.Length; i++)
            {
                var tmpCandle = candleArray[i];
                SignalEstimate tmpEstimate = new SignalEstimate()
                {
                    AugId = tmpCandle.Id,
                    Id = Guid.NewGuid(),
                    LinesOnMacdMapPosition = CalcLinesOnMacdMapPosition(tmpCandle.MACDLine, tmpCandle.SignalLine, m_linesOnMacdClosenessMargin),
                    LinesPosition = CalcLinesPosition(tmpCandle.MACDLine, tmpCandle.SignalLine, m_linesClosenessMargin),
                    SmaPosition = CalcSMAPosition(tmpCandle.Sma50, tmpCandle.Sma100, m_smaClosenessMargin),
                    CandlesToSmaPosition = CalcCandlesToSMAPosition(tmpCandle.Close, tmpCandle.Sma50, tmpCandle.Sma100, smaAndCandleClosenessMargin),
                    signalType = SignalTypes.NotSet,
                    CandlesToEMAPosition = CalcCandlesToEMAPosition(tmpCandle.Close,tmpCandle.Ema50 ?? 0, smaAndCandleClosenessMargin)
                };
                if (i <= 0)
                    tmpEstimate.HistogramPosition = HistogramPositions.unknown;
                else
                    tmpEstimate.HistogramPosition = CalcHistogramPosition(tmpCandle.Histogram, candleArray[i - 1].Histogram);
                result.Add(tmpCandle, tmpEstimate);
            }
            return result;
        }
        public static double CalcProfit(double EnterPrice, double CurrentPrice, double SlPrice, SignalTypes type)
        {
            if (type == SignalTypes.Bullish)
                return (CurrentPrice - EnterPrice) / (EnterPrice - SlPrice);
            else
                return (EnterPrice - CurrentPrice) / (SlPrice - EnterPrice);
        }
        public static string ConvertToPersianDate(DateTime input)
        {
            PersianCalendar pc = new PersianCalendar();
            return $"{pc.GetYear(input).ToString("0000")}-{pc.GetMonth(input).ToString("00")}-{pc.GetDayOfMonth(input).ToString("00")} {input.Hour.ToString("00")}:{input.Minute.ToString("00")}";
        }
        public static SignalProgress CalcSignalProgress(SignalDto signal, AugmentedCandle candle)
        {
            return new SignalProgress()
            {
                Duration = $"{candle.Time.Subtract(signal.StartDate).ToString(@"hh\:mm")}",
                PriceProgress = ((candle.Close - signal.EnterPrice)).ToString("0.0##"),
                MacdMapPosition = CalcLinesOnMacdMapPosition(candle.MACDLine, candle.SignalLine,GetCloseness(linesOnMacdClosenessMargin,candle.Close)),
                LinePosition = CalcLinesPosition(candle.MACDLine, candle.SignalLine, GetCloseness(linesClosenessMargin, candle.Close)),
                smaPosition = CalcSMAPosition(candle.Sma50, candle.Sma100, GetCloseness(smaClosenessMargin, candle.Close)),
                Profit = CalcProfit(signal.EnterPrice, candle.Close, signal.StopLostPrice, signal.SignalType),
                CandlesToEMAPosition = CalcCandlesToEMAPosition(candle.Close, candle.Ema50 ?? 0, GetCloseness(smaAndCandleClosenessMargin,candle.Close))
            };
        }
        public static string CastCandleToCsv(AugmentedCandle input)
        {
            return $"{input.Time},{input.Open},{input.High},{input.Low},{input.Close},{input.Volume},{input.Sma50},{input.Sma100},{input.Ema12},{input.Ema26}," +
                   $"{input.MACDLine},{input.SignalLine},{input.Histogram},{input.RSI}";
        }
        public static string CastCandleEstimateToCsv(KeyValuePair<AugmentedCandle, SignalEstimate> input)
        {
            return $"{input.Key.Time},{input.Key.Open},{input.Key.High},{input.Key.Low},{input.Key.Close}," +
                   $"{input.Key.MACDLine},{input.Key.SignalLine},{input.Key.Histogram},{input.Key.RSI}," +
                   $"{input.Value.CandlesToEMAPosition.ToString()},{input.Value.LinesPosition.ToString()}," +
                   $"{input.Value.LinesOnMacdMapPosition.ToString()}";
        }
        public static string CastSignalToCsv(SignalDto input)
        {
            return $"{input.Id},{input.SignalProvider.ToString()},{input.Symbol.ToString()},{input.Timeframe.ToString()},{input.SignalType.ToString()},{ConvertToPersianDate(input.StartDate)}," +
                $"{input.EnterPrice},{input.StopLostPrice},{input.TakeProfitPrice},{ConvertToPersianDate(input.CreateDate)},{input.IsActive},{input.WinChanceEstimate}";
        }
        public static string CastSignalResultToCsv(SignalResultDto input)
        {
            return $"{input.Id},{input.SignalProvider.ToString()},{input.Symbol.ToString()},{input.Timeframe.ToString()},{input.SignalType.ToString()}," +
                   $"{ConvertToPersianDate(input.StartDate)},{input.EnterPrice},{input.StopLostPrice},{input.TakeProfitPrice}," +
                   $"{ConvertToPersianDate(input.CreateDate)},{input.IsActive},{input.WinChanceEstimate}," +
                   $"{input.SignalId},{input.Status.ToString()},{input.ClosePrice},{input.ProfitPercentage}," +
                   $"{input.Duration},{ConvertToPersianDate(input.CloseDate)},{ConvertToPersianDate(input.CreateDate)},{input.ClosureType}";
        }
        public static SignalDto CastSignalToDto(Signal signal)
        {
            return new SignalDto() { CreateDate = signal.CreateDate, EnterPrice = signal.EnterPrice, Id = signal.Id, IsActive = signal.IsActive, SignalType = (SignalTypes)signal.SignalType, StartDate = signal.StartDate, StopLostPrice = signal.StopLostPrice, Symbol = (Symbol)signal.Symbol, TakeProfitPrice = signal.TakeProfitPrice, Timeframe = (Timeframe)signal.Timeframe, WinChanceEstimate = signal.WinChanceEstimate, SignalProvider = (SignalProviders)signal.SignalProvider };
        }
        public static SignalResultDto CastSignalResultToDto(SignalResult signal)
        {
            return new SignalResultDto()
            {
                CreateDate = signal.CreateDate,
                CloseDate = signal.CloseDate,
                Id = signal.Id,
                ClosePrice = signal.ClosePrice,
                ClosureType = (SignalResultClosureTypes)signal.ClosureType,
                Duration = signal.Duration,
                ProfitPercentage = signal.ProfitPercentage,
                SignalId = signal.SignalId,
                Status = (SignalResultStatus)signal.Status,
                Symbol = (Symbol)signal.Signal.Symbol,
                Timeframe = (Timeframe)signal.Signal.Timeframe,
                EnterPrice = signal.Signal.EnterPrice,
                IsActive = signal.Signal.IsActive,
                SignalProvider = (SignalProviders)signal.Signal.SignalProvider,
                SignalType = (SignalTypes)signal.Signal.SignalType,
                StartDate = signal.Signal.StartDate,
                StopLostPrice = signal.Signal.StopLostPrice,
                TakeProfitPrice = signal.Signal.TakeProfitPrice,
                WinChanceEstimate = signal.Signal.WinChanceEstimate
            };
        }
        public static void WriteToLogFile(string path, string message)
        {
            File.AppendAllText(path, Environment.NewLine + message);
        }

        public static Dictionary<AugmentedCandle, SignalEstimate> GenerateSignalEstimates(List<AugmentedCandle> inputs)
        {
            var tmpcandle = inputs.FirstOrDefault().Close;
            return GenerateSignalEstimates(inputs, GetCloseness(linesOnMacdClosenessMargin,tmpcandle)
                , GetCloseness(linesClosenessMargin, tmpcandle), GetCloseness(smaClosenessMargin, tmpcandle),
                GetCloseness(smaAndCandleClosenessMargin, tmpcandle));
        }
        public static void DownloadSignals(List<SignalResult> signals)
        {
            var filename = Path.Combine(FileSourcePath, $"signals_{DateTime.Now.Ticks}.csv");
            using (StreamWriter writer = new StreamWriter(new FileStream(filename,
            FileMode.Create, FileAccess.Write)))
            {
                writer.WriteLine("CreateDate,Symbol,Timeframe,StartDate,EnterPrice,SignalType,StopLostPrice,TakeProfitPrice,WinChanceEstimate,CloseDate" +
                    ",ClosePrice,ClosureType,Duration,ProfitPercentage,Status");
                signals.ForEach(x => { writer.WriteLine(CastSignalToCsv(x)); });
            }
            Console.WriteLine($"file create in {filename} at {DateTime.Now}");
        }
        public static void SignalReport(List<SignalResult> signals)
        {
            var filename = Path.Combine(FileSourcePath, $"signalreport_{DateTime.Now.Ticks}.csv");
            using (StreamWriter writer = new StreamWriter(new FileStream(filename,
            FileMode.Create, FileAccess.Write)))
            {
                writer.WriteLine("Symbol,Timeframe,SignalCount,CountOfBullish,CountOfBearish,CountOfSuccess,CountOfUnsuccess,CountOfSlhit,CountOfTpHit,CountOfMiddle" +
                    ",SumOfSuccessProfit,SumOfUnsuccessProfit");
                writer.WriteLine($"{signals.FirstOrDefault().Signal.Symbol},{signals.FirstOrDefault().Signal.Timeframe},{signals.Count}," +
                    $"{signals.Count(p => p.Signal.SignalType == (int)SignalTypes.Bullish)},{signals.Count(p => p.Signal.SignalType == (int)SignalTypes.Bearish)}," +
                    $"{signals.Count(p => p.Status == (int)SignalResultStatus.successful)},{signals.Count(p => p.Status == (int)SignalResultStatus.unsuccessful)}," +
                    $"{signals.Count(p => p.ClosureType == (int)SignalResultClosureTypes.slHitted)},{signals.Count(p => p.ClosureType == (int)SignalResultClosureTypes.tpHitted)}," +
                    $"{signals.Count(p => p.ClosureType == (int)SignalResultClosureTypes.closedInMiddleByProvider)}," +
                    $"{signals.Where(q => q.Status == (int)SignalResultStatus.successful).Sum(p => p.ProfitPercentage)}," +
                    $"{signals.Where(q => q.Status == (int)SignalResultStatus.unsuccessful).Sum(p => p.ProfitPercentage)}");
            }
            Console.WriteLine($"file create in {filename} at {DateTime.Now}");
        }
        static string CastSignalToCsv(SignalResult input)
        {
            return $"{input.Signal.CreateDate},{input.Signal.Symbol.ToString()},{input.Signal.Timeframe.ToString()},{input.Signal.StartDate.ToUniversalTime()}," +
                   $"{input.Signal.EnterPrice},{input.Signal.SignalType.ToString()},{input.Signal.StopLostPrice},{input.Signal.TakeProfitPrice}," +
                   $"{input.Signal.WinChanceEstimate},{input.CloseDate.ToUniversalTime()},{input.ClosePrice},{input.ClosureType.ToString()},{input.Duration}," +
                   $"{input.ProfitPercentage},{input.Status.ToString()}";
        }
        static double GetCloseness(double input,double candleClose)
        {
            return  input * (0.0001 * Math.Pow(10, ((int)candleClose).ToString().Length));
        }
    }
    public class ApiHelper
    {
        public enum HttpMethods
        {
            Get, Post, Put, Patch, Delete, Option
        }
        public static async Task<ApiReturnResult> Call(HttpMethods method, string actionUrl, StringContent content = null)
        {
            var result = new ApiReturnResult();
            switch (method)
            {
                case HttpMethods.Get:
                    using (var client = new HttpClient())
                    {
                        var response = client.GetAsync(actionUrl);
                        result.ResultCode = response.Result.StatusCode;
                        result.Content = await response.Result.Content.ReadAsStringAsync();
                    }
                    break;
                case HttpMethods.Post:
                    using (var client = new HttpClient())
                    {
                        var response = client.PostAsync(actionUrl, content);
                        result.ResultCode = response.Result.StatusCode;
                        result.Content = await response.Result.Content.ReadAsStringAsync();
                    }
                    break;
                case HttpMethods.Put:
                    using (var client = new HttpClient())
                    {
                        var response = client.PutAsync(actionUrl, content);
                        result.ResultCode = response.Result.StatusCode;
                        result.Content = await response.Result.Content.ReadAsStringAsync();
                    }
                    break;
                case HttpMethods.Patch:
                    using (var client = new HttpClient())
                    {
                        var response = client.PostAsync(actionUrl, content);
                        result.ResultCode = response.Result.StatusCode;
                        result.Content = await response.Result.Content.ReadAsStringAsync();
                    }
                    break;
                case HttpMethods.Delete:
                    using (var client = new HttpClient())
                    {
                        var response = client.DeleteAsync(actionUrl);
                        result.ResultCode = response.Result.StatusCode;
                        result.Content = await response.Result.Content.ReadAsStringAsync();
                    }
                    break;
                case HttpMethods.Option:
                    using (var client = new HttpClient())
                    {
                        var response = client.GetAsync(actionUrl);
                        result.ResultCode = response.Result.StatusCode;
                        result.Content = await response.Result.Content.ReadAsStringAsync();
                    }
                    break;
            }
            return result;
        }

        public static StringContent Serialize(object obj)
        {
            return new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
        }
        public static T Deserialize<T>(string obj)
        {
            return JsonConvert.DeserializeObject<T>(obj, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            });
        }
    }
    public class ApiReturnResult
    {
        public string Content { get; set; } = null;
        public HttpStatusCode ResultCode { get; set; }
        public bool IsSuccessfulResult()
        {
            if ((int)ResultCode >= 200 && (int)ResultCode < 300)
                return true;
            else
                return false;
        }
    }
}