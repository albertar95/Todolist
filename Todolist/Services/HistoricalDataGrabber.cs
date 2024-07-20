using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private readonly AugmentedCandle InitCandle = new AugmentedCandle()
        {
            Id = new Guid(),
            Sma50 = 1.09065F,
            Sma100 = 1.09006F,
            Ema12 = 1.09024F,
            Ema26 = 1.09034F,
            MACDLine = -0.00014F,
            SignalLine = -0.00017F,
            Histogram = -0.00003F,
            Timeframe = (int)Timeframe.M5,
            Symbol = (int)TradeModels.Symbol.SOLUSDT,
            Close = 1.09527F,
            High = 1.09052F,
            Low = 1.09011F,
            Open = 1.09011F,
            Time = new DateTime(2023, 11, 20, 1, 0, 0),
            Volume = 0,
            RSI = 54.11F
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
        public void AutoRefreshCandles()
        {
            foreach (var sym in Symbol)
            {
                foreach (var tf in Timeframes)
                {
                    var initial = _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)sym);
                    if (initial != null)
                        UpdateData(sym, tf);
                    else
                        SeedData(sym, tf);

                    GetLastHourData(sym, tf).GetAwaiter().GetResult();
                }
            }
        }
        public void RefreshCandles(Symbol sym,Timeframe tf)
        {
            var initial = _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)sym);
            if (initial != null)
                UpdateData(sym, tf);
            else
                SeedData(sym, tf);

            GetLastHourData(sym, tf).GetAwaiter().GetResult();
        }
        public void UpdateData(Symbol symbol, Timeframe tf)
        {
            AugmentedCandle lastCandle = _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)tf && q.Symbol == (int)symbol);
            var candles = GetCandlesFromHost(tf, symbol);
            var convertedCandles = CandlesAugmentation(candles.Where(p => p.Time >= lastCandle.Time).OrderBy(q => q.Time).ToArray(), lastCandle, symbol, tf);
            convertedCandles.ForEach(x => { _dbRepository.Add<AugmentedCandle>(x); });
            DeleteKeepIntervalOverflowCandles(symbol, tf);
        }
        public void SeedData(Symbol symbol, Timeframe tf)
        {
            var candles = GetCandlesFromHost(tf, symbol);
            if (candles.Min(p => p.Time) <= InitCandle.Time)
            {
                var convertedCandles = CandlesAugmentation(candles.Where(p => p.Time >= InitCandle.Time).OrderBy(q => q.Time).ToArray(), InitCandle, symbol, tf);
                convertedCandles.OrderBy(p => p.Time).Skip(150).ToList().ForEach(x => { _dbRepository.Add<AugmentedCandle>(x); });
                DeleteKeepIntervalOverflowCandles(symbol, tf);
            }
        }
        public async Task<bool> GetLastHourData(Symbol symbol, Timeframe timeframe)
        {
            if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                return false;
            else
            {
                AugmentedCandle lastCandle = _dbRepository.GetMax<AugmentedCandle, DateTime>(p => p.Time, q => q.Timeframe == (int)timeframe && q.Symbol == (int)symbol);
                int toCallCount = Convert.ToInt32(DateTime.Now.Subtract(lastCandle.Time).TotalMinutes) / (int)timeframe;
                if (toCallCount > 0)
                {
                    DateTime dt = lastCandle.Time.ToUniversalTime();
                    var apikey = GetDecentMarketDataCredential(symbol, timeframe,toCallCount);
                    for (int i = 1; i <= toCallCount; i++)
                    {
                        var response = await ApiHelper.Call(ApiHelper.HttpMethods.Get, GetMarketDataUrl(symbol, timeframe, dt.AddMinutes((int)timeframe * i).ToString("yyyy-MM-dd-HH:mm"), apikey.ApiKey ?? "BIAEPPWq2_8APwh4QwGv"));
                        if (response.IsSuccessfulResult())
                        {
                            UpdateCredentialCounter(apikey.Id);
                            MarketDataCandle candle = ApiHelper.Deserialize<MarketDataCandle>(response.Content);
                            if (candle.message.ToLower() == "1000 per 1 month")
                            {
                                UpdateCredentialCounter(apikey.Id, true);
                                return false;
                            }
                            if (candle.open == 0 || candle.high == 0 || candle.low == 0 || candle.close == 0)
                                return false;
                            candle.UrlRequestTime = lastCandle.Time.AddMinutes((int)timeframe * i);
                            var convertedCandles = SingleCandlesAugmentation(castMarketDataToCandle(candle, symbol, timeframe), lastCandle, symbol, timeframe);
                            var result = _dbRepository.Add(convertedCandles);
                        }
                    }
                    DeleteKeepIntervalOverflowCandles(symbol, timeframe);
                }
                return true;
            }
        }
        #endregion
        #region PrivateSubMethods
        private List<Candle> GetCandlesFromHost(Timeframe tf, Symbol symbol)
        {
            var hostResponse = callSourceUrl(symbol.ToString(), ((int)tf).ToString());
            var bins = overallReadbin(hostResponse);
            return parseResponse(bins);
        }
        private byte[] callSourceUrl(string symbol, string timeframe)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                //client.DefaultRequestHeaders.Add("If-None-Match", "64fef899-838");
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var response = client.GetAsync($"https://pinegen.com/datafeed/data/premium/{symbol}{timeframe}.lb.gz").GetAwaiter().GetResult();
                var cont = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                var decomp = Decompress(cont);
                return decomp;
                //return cont;
            }
        }
        private byte[] Decompress(Stream source)
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
        private byte[] Decompress(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return Decompress(ms);
            }
        }
        private List<int> overallReadbin(byte[] buffer)
        {
            List<int> binValues = new List<int>();
            using (BinaryReader reader = new BinaryReader(new MemoryStream(buffer)))
            {
                for (var offset = 0; offset < buffer.LongLength; offset += 4)
                {
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    binValues.Add(reader.ReadInt32());
                }
            }
            return binValues;
        }
        private List<Candle> parseResponse(List<int> Bins)
        {
            List<Candle> ds = new List<Candle>();
            var dt2000 = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var millennium = ((DateTimeOffset)dt2000).ToUnixTimeMilliseconds();
            var priceScale = 100000F;
            for (var offset = 0; offset < Bins.Count; offset += 6)
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
            return ds;
        }
        private List<AugmentedCandle> CandlesAugmentation(Candle[] candles, AugmentedCandle init, Symbol symbol, Timeframe timeframe)
        {
            List<AugmentedCandle> result = new List<AugmentedCandle>();
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
        private AugmentedCandle SingleCandlesAugmentation(Candle candles, AugmentedCandle init, Symbol symbol, Timeframe timeframe)
        {
            List<AugmentedCandle> result = new List<AugmentedCandle>();
            var tmpAug = new AugmentedCandle();
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
            tmpAug.MACDLine = tmpAug.Ema12 - tmpAug.Ema26;
            tmpAug.SignalLine = CommonTradeOperations.EmaCalculator(tmpAug.MACDLine, init.SignalLine, 9F);
            tmpAug.Histogram = tmpAug.SignalLine - tmpAug.MACDLine;
            tmpAug.RSI = 0F;
            tmpAug.Symbol = (int)symbol;
            tmpAug.Timeframe = (int)timeframe;
            return tmpAug;
        }
        private List<Timeframe> getTimeframes(string input)
        {
            List<Timeframe> result = new List<Timeframe>();
            input.ToLower().Split(',').ToList().ForEach(x => { result.Add((Timeframe)(int.Parse(x))); });
            return result;
        }
        private List<Symbol> getSymbol(string input)
        {
            List<Symbol> result = new List<Symbol>();
            input.ToLower().Split(',').ToList().ForEach(x => { result.Add((Symbol)(int.Parse(x))); });
            return result;
        }
        private string getTimeFrameName(Timeframe timeframe)
        {
            var tfVal = (int)timeframe;
            if (tfVal < 60)
                return $"{tfVal}m";
            else if (tfVal >= 60 && tfVal < 1440)
                return $"{tfVal / 60}h";
            else if (tfVal == 1440)
                return $"1d";
            else
                return "";
        }
        private string GetMarketDataUrl(Symbol symbol, Timeframe timeframe, string time, string key)
        {
            return $"https://marketdata.tradermade.com/api/v1/minute_historical?currency={symbol.ToString()}&date_time={time}&api_key={key}";
        }
        private Candle castMarketDataToCandle(MarketDataCandle marketData, Symbol symbol, Timeframe timeframe)
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
        private MarketDataCredential GetDecentMarketDataCredential(Symbol symbol, Timeframe timeframe,int numberOfCalls = 1)
        {
            MarketDataCredential result = new MarketDataCredential();
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
                    if(numberOfCalls <= 1)
                    {
                        result = mdc;
                        break;
                    }
                    else
                    {
                        if((1000 - mdc.CallCounter) >= numberOfCalls)
                        {
                            result = mdc;
                            break;
                        }
                    }
                }
            }
            return result;
        }
        private bool UpdateCredentialCounter(Guid Id, bool Overflow = false)
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
        private void DeleteKeepIntervalOverflowCandles(Symbol symbol, Timeframe timeframe)
        {
            try
            {
                foreach (var item in _dbRepository.GetList<AugmentedCandle>
                    (p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.Time > DateTime.Now.Date.AddDays(keepDataInterval * -1)))
                {
                    _dbRepository.Delete<AugmentedCandle>(item);
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion
    }
}