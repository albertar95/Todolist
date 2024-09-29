﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using Todolist.Helpers;
using Todolist.Models;
using Todolist.Services.Contracts;
using static Todolist.Models.TradeModels;

namespace Todolist.Services
{
    public class HistoricalDataGrabber : IHistoricalDataGrabber
    {
        #region Variables
        private readonly int keepDataInterval = int.Parse(ConfigurationManager.AppSettings["keepDataInterval"]);
        private readonly List<AugmentedCandle> InitCandle = new List<AugmentedCandle>(){
            new AugmentedCandle(){
            Id = new Guid(),
            Sma50 = 141.24F,
            Sma100 = 142.11F,
            Ema12 = 140.98F,
            Ema26 = 141.23F,
            Ema50 = 141.38F,
            Ema100 = 141.65,
            Ema200 = 141.27,
            MACDLine = -0.24F,
            SignalLine = -0.07F,
            Histogram = 0.17F,
            Timeframe = (int)Timeframe.M15,
            Symbol = (int)TradeModels.Symbol.SOLUSDT,
            Close = 164.26F,
            High = 164.58F,
            Low = 164.26F,
            Open = 164.54F,
            Time = new DateTime(2024, 07, 11, 08, 0, 0),
            Volume = 0,
            RSI = 41.02F
        },
            new AugmentedCandle(){
            Id = new Guid(),
            Sma50 = 67713F,
            Sma100 = 67693F,
            Ema12 = 67594F,
            Ema26 = 67657F,
            Ema50 = 67682F,
            MACDLine = -63F,
            SignalLine = -30F,
            Histogram = -33F,
            Timeframe = (int)Timeframe.M15,
            Symbol = (int)TradeModels.Symbol.BTCUSDT,
            Close = 67446F,
            High = 67531F,
            Low = 67446F,
            Open = 67482F,
            Time = new DateTime(2024, 06, 02, 10, 0, 0),
            Volume = 0,
            RSI = 33.13F
        },
            new AugmentedCandle(){
            Id = new Guid(),
            Sma50 = 181.22F,
            Sma100 = 182.52F,
            Ema12 = 179.82F,
            Ema26 = 180.30F,
            Ema50 = 181.00F,
            MACDLine = -0.48F,
            SignalLine = -0.52F,
            Histogram = 0.04F,
            Timeframe = (int)Timeframe.M5,
            Symbol = (int)TradeModels.Symbol.SOLUSDT,
            Close = 179.44F,
            High = 179.57F,
            Low = 179.29F,
            Open = 179.57F,
            Time = new DateTime(2024, 07, 22, 6, 30, 0),
            Volume = 0,
            RSI = 37.72F
        },
            new AugmentedCandle(){
            Id = new Guid(),
            Sma50 = 67379F,
            Sma100 = 67662F,
            Ema12 = 67250F,
            Ema26 = 67280F,
            Ema50 = 67393F,
            MACDLine = -31F,
            SignalLine = -56F,
            Histogram = 25F,
            Timeframe = (int)Timeframe.M5,
            Symbol = (int)TradeModels.Symbol.BTCUSDT,
            Close = 67303F,
            High = 67321F,
            Low = 67263F,
            Open = 67314F,
            Time = new DateTime(2024, 07, 22, 9, 30, 0),
            Volume = 0,
            RSI = 51.75F
        },
            new AugmentedCandle(){
            Id = new Guid(),
            Sma50 = 43318F,
            Sma100 = 41824F,
            Ema12 = 43811F,
            Ema26 = 43511F,
            Ema50 = 43079F,
            Ema100 = 42125,
            Ema200 = 41960,
            MACDLine = 308F,
            SignalLine = 308F,
            Histogram = 0F,
            Timeframe = (int)Timeframe.H4,
            Symbol = (int)TradeModels.Symbol.BTCUSDT,
            Close = 43966F,
            High = 43966F,
            Low = 43371F,
            Open = 43592F,
            Time = new DateTime(2022, 02, 17, 7, 30, 0),
            Volume = 0,
            RSI = 54.76F
        }
        };
        private readonly IDbRepository _dbRepository;
        private readonly List<Timeframe> Timeframes;
        private readonly List<Symbol> Symbol;
        #endregion
        public HistoricalDataGrabber(IDbRepository dbRepository)
        {
            _dbRepository = dbRepository;
            Timeframes = getTimeframes(ConfigurationManager.AppSettings["Timeframes"]);
            Symbol = getSymbol(ConfigurationManager.AppSettings["Symbol"]);
        }
        #region CoreMethods
        public Tuple<bool, List<Tuple<int, string>>> AutoRefreshCandles()
        {
            var resultMessage = new List<Tuple<int, string>>();
            bool hasError = false;
            try
            {
                foreach (var sym in Symbol)
                {
                    foreach (var tf in Timeframes)
                    {
                        var initial = _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)sym);
                        if (initial != null)
                        {
                            if (!UpdateData(sym, tf, initial))
                            {
                                resultMessage.Add(new Tuple<int, string>((int)sym, "bulk update error"));
                                hasError = true;
                            }
                        }
                        else
                        {
                            if(!SeedData(sym, tf))
                            {
                                resultMessage.Add(new Tuple<int, string>((int)sym, "seed update error"));
                                hasError = true;
                            }
                        }
                        initial = _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)sym);
                        var lastHourResult = GetLastHourData(sym, tf, initial).GetAwaiter().GetResult();
                        if (lastHourResult.Item1)
                            resultMessage.Add(new Tuple<int, string>((int)sym, "success"));
                        else
                        {
                            resultMessage.Add(new Tuple<int, string>((int)sym, lastHourResult.Item2));
                            hasError = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resultMessage.Add(new Tuple<int, string>(1, ex.Message));
                hasError = true;
            }
            return new Tuple<bool, List<Tuple<int, string>>>(hasError,resultMessage);
        }
        public DateTime RefreshCandles(Symbol sym,Timeframe tf)
        {
            try
            {
                var initial = _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)sym);
                if (initial != null)
                {
                    UpdateData(sym, tf, initial);
                    GetLastHourData(sym, tf, initial).GetAwaiter().GetResult();
                }
                else
                {
                    SeedData(sym, tf);
                    initial = _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)sym);
                    GetLastHourData(sym, tf, initial).GetAwaiter().GetResult();
                }
                return _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)sym).Time;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }
        public bool UpdateData(Symbol symbol, Timeframe tf,AugmentedCandle lastCandle)
        {
            try
            {
                if (Convert.ToInt32(DateTime.Now.ToLocalTime().Subtract(lastCandle.Time).TotalMinutes) < 60)
                    return true;
                var candles = GetCandlesFromHost(tf, symbol);
                var convertedCandles = CandlesAugmentation(candles.Where(p => p.Time >= lastCandle.Time).OrderBy(q => q.Time).ToArray(), lastCandle, symbol, tf);
                _dbRepository.AddBatch(convertedCandles);
                DeleteKeepIntervalOverflowCandles(symbol, tf);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool SeedData(Symbol symbol, Timeframe tf)
        {
            try
            {
                var candles = GetCandlesFromHost(tf, symbol);
                if (candles.Min(p => p.Time) <= InitCandle.FirstOrDefault(q => q.Symbol == (int)symbol && q.Timeframe == (int)tf).Time)
                {
                    var convertedCandles = CandlesAugmentation(candles.Where(p => p.Time >= InitCandle.FirstOrDefault(q => q.Symbol == (int)symbol && q.Timeframe == (int)tf).Time).OrderBy(q => q.Time).ToArray(), InitCandle.FirstOrDefault(q => q.Symbol == (int)symbol && q.Timeframe == (int)tf), symbol, tf);
                    _dbRepository.AddBatch<AugmentedCandle>(convertedCandles.OrderBy(p => p.Time).Skip(150).ToList().Where(q => q.Time >= DateTime.Now.Date.AddDays(keepDataInterval * -1)).ToList());
                    //DeleteKeepIntervalOverflowCandles(symbol, tf);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<Tuple<bool,string>> GetLastHourData(Symbol symbol, Timeframe timeframe, AugmentedCandle lastCandle)
        {
            try
            {
                if (DateTime.Now.ToLocalTime().Hour >= 21 && DateTime.Now.ToLocalTime().Hour <= 6)
                    return new Tuple<bool, string>(true, "");
                if (Convert.ToInt32(DateTime.Now.ToLocalTime().Subtract(lastCandle.Time).TotalMinutes) < (int)timeframe)
                    return new Tuple<bool, string>(true,"");
                if((int)timeframe < 60)
                {
                    if (Convert.ToInt32(DateTime.Now.ToLocalTime().Subtract(lastCandle.Time).TotalMinutes) > 60)
                        return new Tuple<bool, string>(true, "");
                }
                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                    return new Tuple<bool, string>(true, "");
                if (lastCandle == null)
                    return new Tuple<bool, string>(false, "last calndle is null");
                else
                {
                    int toCallCount = Convert.ToInt32(DateTime.Now.ToLocalTime().Subtract(lastCandle.Time).TotalMinutes) / (int)timeframe;
                    if (toCallCount > 0)
                    {
                        DateTime dt = lastCandle.Time.ToUniversalTime();
                        var apikey = GetDecentMarketDataCredential(symbol, timeframe, toCallCount);
                        for (int i = 1; i <= toCallCount; i++)
                        {
                            var response = await ApiHelper.Call(ApiHelper.HttpMethods.Get, GetMarketDataUrl(symbol, timeframe, dt.AddMinutes((int)timeframe * i).ToString("yyyy-MM-dd-HH:mm"), apikey.ApiKey ?? "BIAEPPWq2_8APwh4QwGv"));
                            if (response.IsSuccessfulResult())
                            {
                                UpdateCredentialCounter(apikey.Id);
                                MarketDataCandle candle = ApiHelper.Deserialize<MarketDataCandle>(response.Content);
                                if (candle != null)
                                {
                                    if (candle.message != null)
                                    {
                                        if (candle.message.ToLower() == "1000 per 1 month")
                                        {
                                            UpdateCredentialCounter(apikey.Id, true);
                                            return new Tuple<bool, string>(false, "api 1000 counter is full");
                                        }
                                    }
                                    if (candle.open == 0 || candle.high == 0 || candle.low == 0 || candle.close == 0)
                                        return new Tuple<bool, string>(false, "wronge data from api");
                                    candle.UrlRequestTime = lastCandle.Time.AddMinutes((int)timeframe * i);
                                    var convertedCandles = SingleCandlesAugmentation(castMarketDataToCandle(candle, symbol, timeframe), lastCandle, symbol, timeframe);
                                    var result = _dbRepository.Add(convertedCandles);
                                }
                                else
                                    new Tuple<bool, string>(false, "error in deserializing candle");
                            }
                            else
                                new Tuple<bool, string>(false, "http error in calling api");
                        }
                        DeleteKeepIntervalOverflowCandles(symbol, timeframe);
                    }
                    return new Tuple<bool, string>(true, "");
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, ex.Message);
            }
        }
        public DateTime GetLastCandle(Symbol sym, Timeframe tf)
        {
            try
            {
                return _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)sym).Time;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }
        #endregion
        #region PrivateSubMethods
        private List<Candle> GetCandlesFromHost(Timeframe tf, Symbol symbol)
        {
            try
            {
                Timeframe FetchTimeframe = tf;
                if (tf >= Timeframe.H1)
                    FetchTimeframe = Timeframe.M30;
                var hostResponse = callSourceUrl(symbol.ToString(), ((int)FetchTimeframe).ToString());
                var bins = overallReadbin(hostResponse);
                List<Candle> result = new List<Candle>();
                result = symbol.ToString().ToLower().EndsWith("usdt") ? parseResponse(bins, 100) : parseResponse(bins);
                if (tf >= Timeframe.H1)
                    result = ProcessUpperMinuteCandles(result,tf);
                    return result;
            }
            catch (Exception)
            {
                return new List<Candle>();
            }
        }
        private List<Candle> ProcessUpperMinuteCandles(List<Candle> candles,Timeframe target)
        {
            List<Candle> result = new List<Candle>();
            var candles2 = candles.OrderBy(p => p.Time).Skip(candles.FindIndex(q => q.Time.Hour % 4 == 0)).ToList();
            var ratio = (int)target / 30;
            for (int i = 0; i < candles2.Count / ratio; i++)
            {
                result.Add(candles2[i*ratio]);
            }
            return result;
        }
        private byte[] callSourceUrl(string symbol, string timeframe)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                    //client.DefaultRequestHeaders.Add("If-None-Match", "64fef899-838");
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback =
                    delegate (
                        object s,
                        X509Certificate certificate,
                        X509Chain chain,
                        SslPolicyErrors sslPolicyErrors
                    ) {
                        return true;
                    };
                    HttpResponseMessage response = new HttpResponseMessage();
                    if (!symbol.ToLower().EndsWith("usdt"))
                        response = client.GetAsync($"https://data.forexsb.com/datafeed/data/dukascopy/{symbol}{timeframe}.lb.gz").GetAwaiter().GetResult();
                    else
                        response = client.GetAsync($"https://data.forexsb.com/datafeed/data/binance/{symbol}{timeframe}.lb.gz").GetAwaiter().GetResult();
                    var cont = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    var decomp = Decompress(cont);
                    return decomp;
                    //return cont;
                }
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }
        private byte[] Decompress(Stream source)
        {
            try
            {
                using (var gzip = new GZipStream(source, CompressionMode.Decompress))
                {
                    using (var decompressed = new MemoryStream())
                    {
                        gzip.CopyTo(decompressed);
                        return decompressed.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                return new byte[0];
            }
        }
        private byte[] Decompress(byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    return Decompress(ms);
                }
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }
        private List<int> overallReadbin(byte[] buffer)
        {
            List<int> binValues = new List<int>();
            try
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(buffer)))
                {
                    for (var offset = 0; offset < buffer.LongLength; offset += 4)
                    {
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        binValues.Add(reader.ReadInt32());
                    }
                }
            }
            catch (Exception)
            {
            }
            return binValues;
        }
        private List<Candle> parseResponse(List<int> Bins,float priceScale = 100000F)
        {
            List<Candle> ds = new List<Candle>();
            try
            {
                var dt2000 = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var millennium = ((DateTimeOffset)dt2000).ToUnixTimeMilliseconds();
                int chunkSize = CommonTradeOperations.UnixTimeStampToLocalDateTime(millennium + (Convert.ToInt64(Bins[6]) * Convert.ToInt64(60000))) >
                    CommonTradeOperations.UnixTimeStampToLocalDateTime(millennium + (Convert.ToInt64(Bins[0]) * Convert.ToInt64(60000))) ? 6 : 7;
                for (var offset = 0; offset < Bins.Count; offset += chunkSize)
                {
                    ds.Add(
                        new Candle
                        {
                            Id = Guid.NewGuid(),
                            Time = CommonTradeOperations.UnixTimeStampToLocalDateTime(millennium + (Convert.ToInt64(Bins[offset]) * Convert.ToInt64(60000))),
                            Open = float.Parse(Bins[offset + 1].ToString()) / priceScale,
                            High = float.Parse(Bins[offset + 2].ToString()) / priceScale,
                            Low = float.Parse(Bins[offset + 3].ToString()) / priceScale,
                            Close = float.Parse(Bins[offset + 4].ToString()) / priceScale,
                            Volume = int.Parse(Bins[offset + 5].ToString())
                        });
                }
            }
            catch (Exception)
            {
            }
            return ds;
        }
        private List<AugmentedCandle> CandlesAugmentation(Candle[] candles, AugmentedCandle init, Symbol symbol, Timeframe timeframe)
        {
            List<AugmentedCandle> result = new List<AugmentedCandle>();
            try
            {
                result.Add(init);
                for (int i = 1; i <= candles.Length - 1; i++)
                {
                    var tmpAug = new AugmentedCandle();
                    tmpAug.Id = Guid.NewGuid();
                    tmpAug.Time = candles[i].Time;
                    tmpAug.Open = candles[i].Open;
                    tmpAug.High = candles[i].High;
                    tmpAug.Low = candles[i].Low;
                    tmpAug.Close = candles[i].Close;
                    tmpAug.Volume = candles[i].Volume;
                    if (i < 50)
                        tmpAug.Sma50 = 0;
                    else
                        tmpAug.Sma50 = float.Parse(candles.Skip(i - 50).Take(50).Sum(p => p.Close).ToString()) / 50F;
                    if (i < 100)
                        tmpAug.Sma100 = 0;
                    else
                        tmpAug.Sma100 = float.Parse(candles.Skip(i - 100).Take(100).Sum(p => p.Close).ToString()) / 100F;
                    tmpAug.Ema12 = CommonTradeOperations.EmaCalculator(candles[i].Close, result[i - 1].Ema12, 12F);
                    tmpAug.Ema26 = CommonTradeOperations.EmaCalculator(candles[i].Close, result[i - 1].Ema26, 26F);
                    tmpAug.Ema50 = CommonTradeOperations.EmaCalculator(candles[i].Close, result[i - 1].Ema50 ?? 0F, 50F);
                    tmpAug.Ema100 = CommonTradeOperations.EmaCalculator(candles[i].Close, result[i - 1].Ema100 ?? 0F, 100F);
                    tmpAug.Ema200 = CommonTradeOperations.EmaCalculator(candles[i].Close, result[i - 1].Ema200 ?? 0F, 200F);
                    tmpAug.MACDLine = tmpAug.Ema12 - tmpAug.Ema26;
                    tmpAug.SignalLine = CommonTradeOperations.EmaCalculator(tmpAug.MACDLine, result[i - 1].SignalLine, 9F);
                    tmpAug.Histogram = tmpAug.SignalLine - tmpAug.MACDLine;
                    if (i > 14)
                        tmpAug.RSI = CommonTradeOperations.RSICalculator(candles.Skip(i - 14).Take(14).ToList());
                    else
                        tmpAug.RSI = 0F;
                    tmpAug.Symbol = (int)symbol;
                    tmpAug.Timeframe = (int)timeframe;
                    result.Add(tmpAug);
                }
                return result.Skip(1).ToList();
            }
            catch (Exception)
            {
                return result;
            }
        }
        private AugmentedCandle SingleCandlesAugmentation(Candle candles, AugmentedCandle init, Symbol symbol, Timeframe timeframe)
        {
            var tmpAug = new AugmentedCandle();
            try
            {
                tmpAug.Id = Guid.NewGuid();
                tmpAug.Time = candles.Time;
                tmpAug.Open = candles.Open;
                tmpAug.High = candles.High;
                tmpAug.Low = candles.Low;
                tmpAug.Close = candles.Close;
                tmpAug.Volume = candles.Volume;
                var cndls = _dbRepository.GetList<AugmentedCandle>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe).OrderByDescending(q => q.Time).Take(100);
                if (cndls.Count() < 50)
                    tmpAug.Sma50 = 0;
                else
                    tmpAug.Sma50 = float.Parse(cndls.Skip(50).Take(50).Sum(p => p.Close).ToString()) / 50F;
                if (cndls.Count() < 100)
                    tmpAug.Sma100 = 0;
                else
                    tmpAug.Sma100 = float.Parse(cndls.Sum(p => p.Close).ToString()) / 100F;
                tmpAug.Ema12 = CommonTradeOperations.EmaCalculator(candles.Close, init.Ema12, 12F);
                tmpAug.Ema26 = CommonTradeOperations.EmaCalculator(candles.Close, init.Ema26, 26F);
                tmpAug.Ema50 = CommonTradeOperations.EmaCalculator(candles.Close, init.Ema50 ?? 0F, 50F);
                tmpAug.Ema100 = CommonTradeOperations.EmaCalculator(candles.Close, init.Ema100 ?? 0F, 100F);
                tmpAug.Ema200 = CommonTradeOperations.EmaCalculator(candles.Close, init.Ema200 ?? 0F, 200F);
                tmpAug.MACDLine = tmpAug.Ema12 - tmpAug.Ema26;
                tmpAug.SignalLine = CommonTradeOperations.EmaCalculator(tmpAug.MACDLine, init.SignalLine, 9F);
                tmpAug.Histogram = tmpAug.SignalLine - tmpAug.MACDLine;
                tmpAug.RSI = 0F;
                tmpAug.Symbol = (int)symbol;
                tmpAug.Timeframe = (int)timeframe;
            }
            catch (Exception)
            {
            }
            return tmpAug;
        }
        private List<Timeframe> getTimeframes(string input)
        {
            List<Timeframe> result = new List<Timeframe>();
            try
            {
                input.ToLower().Split(',').ToList().ForEach(x => { result.Add((Timeframe)(int.Parse(x))); });
            }
            catch (Exception)
            {
            }
            return result;
        }
        private List<Symbol> getSymbol(string input)
        {
            List<Symbol> result = new List<Symbol>();
            try
            {
                input.ToLower().Split(',').ToList().ForEach(x => { result.Add((Symbol)(int.Parse(x))); });
            }
            catch (Exception)
            {
            }
            return result;
        }
        private string GetMarketDataUrl(Symbol symbol, Timeframe timeframe, string time, string key)
        {
            try
            {
                var symbolName = symbol.ToString().ToLower().Replace("usdt", "usd");
                return $"https://marketdata.tradermade.com/api/v1/minute_historical?currency={symbolName}&date_time={time}&api_key={key}";
            }
            catch (Exception)
            {
                return "";
            }
        }
        private Candle castMarketDataToCandle(MarketDataCandle marketData, Symbol symbol, Timeframe timeframe)
        {
            try
            {
                return new Candle()
                {
                    Close = marketData.close,
                    High = marketData.high,
                    Id = Guid.NewGuid(),
                    Low = marketData.low,
                    Open = marketData.open,
                    Symbol = symbol,
                    Timeframe = timeframe,
                    Volume = 0,
                    Time = marketData.UrlRequestTime
                };
            }
            catch (Exception)
            {
                return new Candle();
            }
        }
        private MarketDataCredential GetDecentMarketDataCredential(Symbol symbol, Timeframe timeframe,int numberOfCalls = 1)
        {
            MarketDataCredential result = new MarketDataCredential();
            try
            {
                foreach (var mdc in _dbRepository.GetList<MarketDataCredential>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe).OrderByDescending(q => q.CallCounter))
                {
                    if (DateTime.Now.Date.Subtract(mdc.RefreshDate).TotalDays > 30)
                    {
                        mdc.CallCounter = 0;
                        mdc.RefreshDate = DateTime.Now;
                        _dbRepository.Update(mdc);
                        result = mdc;
                        break;
                    }
                    if (mdc.CallCounter < 1000)
                    {
                        if (numberOfCalls <= 1)
                        {
                            result = mdc;
                            break;
                        }
                        else
                        {
                            if ((1000 - mdc.CallCounter) >= numberOfCalls)
                            {
                                result = mdc;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return result;
        }
        private bool UpdateCredentialCounter(Guid Id, bool Overflow = false)
        {
            try
            {
                var credential = _dbRepository.Get<MarketDataCredential>(p => p.Id == Id);
                if (credential != null)
                {
                    if (!Overflow)
                        credential.CallCounter = credential.CallCounter + 1;
                    else
                        credential.CallCounter = 1000;
                    return _dbRepository.Update(credential);
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private void DeleteKeepIntervalOverflowCandles(Symbol symbol, Timeframe timeframe)
        {
            try
            {
                _dbRepository.DeleteBatch(_dbRepository.GetList<AugmentedCandle>
                    (p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.Time > DateTime.Now.Date.AddDays(keepDataInterval * -1)));
            }
            catch (Exception)
            {
            }
        }
        #endregion
    }
}