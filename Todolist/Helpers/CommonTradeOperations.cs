using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public static LinesOnMacdPositions CalcLinesOnMacdMapPosition(double macd, double signal, double closenessMargin = 0.000002F)
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
        public static LinesPositions CalcLinesPosition(double macd, double signal, double closenessMargin = 0.000005F)
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
        public static SMAPositions CalcSMAPosition(double sma50, double sma100, double closenessMargin = 0.0001F)
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
        public static CandlesToSMAPositions CalcCandlesToSMAPosition(double close, double sma50, double sma100, double closenessMargin = 0.0001F)
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
            , float linesOnMacdClosenessMargin = 0.000002F, float linesClosenessMargin = 0.000005F, float smaClosenessMargin = 0.0001F
            , float smaAndCandleClosenessMargin = 0.0001F)
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
                    LinesOnMacdMapPosition = CalcLinesOnMacdMapPosition(tmpCandle.MACDLine, tmpCandle.SignalLine, linesOnMacdClosenessMargin),
                    LinesPosition = CalcLinesPosition(tmpCandle.MACDLine, tmpCandle.SignalLine, linesClosenessMargin),
                    SmaPosition = CalcSMAPosition(tmpCandle.Sma50, tmpCandle.Sma100, smaClosenessMargin),
                    CandlesToSmaPosition = CalcCandlesToSMAPosition(tmpCandle.Close, tmpCandle.Sma50, tmpCandle.Sma100, smaAndCandleClosenessMargin),
                    signalType = SignalTypes.NotSet
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
                PriceProgress = ((candle.Close - signal.EnterPrice) * 10000F).ToString(),
                MacdMapPosition = CalcLinesOnMacdMapPosition(candle.MACDLine, candle.SignalLine),
                LinePosition = CalcLinesPosition(candle.MACDLine, candle.SignalLine),
                smaPosition = CalcSMAPosition(candle.Sma50, candle.Sma100),
                Profit = CalcProfit(signal.EnterPrice, candle.Close, signal.StopLostPrice, signal.SignalType)
            };
        }
        public static string CastCandleToCsv(AugmentedCandle input)
        {
            return $"{input.Time},{input.Open},{input.High},{input.Low},{input.Close},{input.Volume},{input.Sma50},{input.Sma100},{input.Ema12},{input.Ema26}," +
                   $"{input.MACDLine},{input.SignalLine},{input.Histogram},{input.RSI}";
        }
        public static string CastCandleEstimateToCsv(KeyValuePair<AugmentedCandle, SignalEstimate> input)
        {
            return $"{input.Key.Time},{input.Key.Open},{input.Key.High},{input.Key.Low},{input.Key.Close},{input.Key.Volume},{input.Key.Sma50},{input.Key.Sma100},{input.Key.Ema12},{input.Key.Ema26}," +
                   $"{input.Key.MACDLine},{input.Key.SignalLine},{input.Key.Histogram},{input.Key.RSI}," +
                   $"{input.Value.SmaPosition.ToString()},{input.Value.CandlesToSmaPosition.ToString()}," +
                   $"{input.Value.LinesOnMacdMapPosition.ToString()},{input.Value.LinesPosition.ToString()}";
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
        public static SignalDto CastSignalToDto(Signals signal)
        {
            return new SignalDto() { CreateDate = signal.CreateDate, EnterPrice = signal.EnterPrice, Id = signal.Id, IsActive = signal.IsActive, SignalType = signal.SignalType, StartDate = signal.StartDate, StopLostPrice = signal.StopLostPrice, Symbol = signal.Symbol, TakeProfitPrice = signal.TakeProfitPrice, Timeframe = signal.Timeframe, WinChanceEstimate = signal.WinChanceEstimate, SignalProvider = signal.SignalProvider };
        }
        public static SignalResultDto CastSignalResultToDto(SignalResults signal)
        {
            return new SignalResultDto()
            {
                CreateDate = signal.CreateDate,
                CloseDate = signal.CloseDate,
                Id = signal.Id,
                ClosePrice = signal.ClosePrice,
                ClosureType = signal.ClosureType,
                Duration = signal.Duration,
                ProfitPercentage = signal.ProfitPercentage,
                SignalId = signal.SignalId,
                Status = signal.Status,
                Symbol = signal.Signal.Symbol,
                Timeframe = signal.Signal.Timeframe,
                EnterPrice = signal.Signal.EnterPrice,
                IsActive = signal.Signal.IsActive,
                SignalProvider = signal.Signal.SignalProvider,
                SignalType = signal.Signal.SignalType,
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