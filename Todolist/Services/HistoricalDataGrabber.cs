using System;
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
            Sma50 = 1.09065F,
            Sma100 = 1.09006F,
            Ema12 = 1.09024F,
            Ema26 = 1.09034F,
            MACDLine = -0.00014F,
            SignalLine = -0.00017F,
            Histogram = -0.00003F,
            Timeframe = (int)Timeframe.M5,
            Symbol = (int)TradeModels.Symbol.EURUSD,
            Close = 1.09527F,
            High = 1.09052F,
            Low = 1.09011F,
            Open = 1.09011F,
            Time = new DateTime(2023, 11, 20, 1, 0, 0),
            Volume = 0,
            RSI = 54.11F
        },new AugmentedCandle(){
            Id = new Guid(),
            Sma50 = 147.97F,
            Sma100 = 147.30F,
            Ema12 = 146.50F,
            Ema26 = 146.59F,
            MACDLine = -0.09F,
            SignalLine = -0.16F,
            Histogram = 0.07F,
            Timeframe = (int)Timeframe.M5,
            Symbol = (int)TradeModels.Symbol.SOLUSDT,
            Close = 146.88F,
            High = 146.94F,
            Low = 146.71F,
            Open = 146.71F,
            Time = new DateTime(2024, 07, 02, 4, 0, 0),
            Volume = 0,
            RSI = 55.42F
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
        public void AutoRefreshCandles()
        {
            try
            {
                foreach (var sym in Symbol)
                {
                    foreach (var tf in Timeframes)
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
                    }
                }
            }
            catch (Exception)
            {
            }
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
        public void UpdateData(Symbol symbol, Timeframe tf,AugmentedCandle lastCandle)
        {
            try
            {
                if (Convert.ToInt32(DateTime.Now.Subtract(lastCandle.Time).TotalMinutes) < 60)
                    return;
                var candles = GetCandlesFromHost(tf, symbol);
                var convertedCandles = CandlesAugmentation(candles.Where(p => p.Time >= lastCandle.Time).OrderBy(q => q.Time).ToArray(), lastCandle, symbol, tf);
                _dbRepository.AddBatch(convertedCandles);
                DeleteKeepIntervalOverflowCandles(symbol, tf);
            }
            catch (Exception)
            {
            }
        }
        public void SeedData(Symbol symbol, Timeframe tf)
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
            }
            catch (Exception)
            {
            }
        }
        public async Task<bool> GetLastHourData(Symbol symbol, Timeframe timeframe, AugmentedCandle lastCandle)
        {
            try
            {
                if (Convert.ToInt32(DateTime.Now.Subtract(lastCandle.Time).TotalMinutes) < (int)timeframe)
                    return false;
                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                    return false;
                else
                {
                    int toCallCount = Convert.ToInt32(DateTime.Now.Subtract(lastCandle.Time).TotalMinutes) / (int)timeframe;
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
                                            return false;
                                        }
                                    }
                                    if (candle.open == 0 || candle.high == 0 || candle.low == 0 || candle.close == 0)
                                        return false;
                                    candle.UrlRequestTime = lastCandle.Time.AddMinutes((int)timeframe * i);
                                    var convertedCandles = SingleCandlesAugmentation(castMarketDataToCandle(candle, symbol, timeframe), lastCandle, symbol, timeframe);
                                    var result = _dbRepository.Add(convertedCandles);
                                }
                            }
                        }
                        DeleteKeepIntervalOverflowCandles(symbol, timeframe);
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
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
                var hostResponse = callSourceUrl(symbol.ToString(), ((int)tf).ToString());
                var bins = overallReadbin(hostResponse);
                List<Candle> result = new List<Candle>();
                result = symbol.ToString().ToLower().EndsWith("usdt") ? parseResponse(bins, 100) : parseResponse(bins);
                return result;
            }
            catch (Exception)
            {
                return new List<Candle>();
            }
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
            catch (Exception)
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