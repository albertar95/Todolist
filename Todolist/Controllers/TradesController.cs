using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Todolist.Models;
using Todolist.Services.Contracts;
using static Todolist.Models.TradeModels;

namespace Todolist.Controllers
{
    [Authorize]
    public class TradesController : Controller
    {
        private readonly IRequestProcessor _requestProcessor;
        private readonly IHistoricalDataGrabber _historicalDataGrabber;
        private readonly ISignalGenerator _signalGenerator;
        public TradesController(IRequestProcessor requestProcessor,IHistoricalDataGrabber historicalDataGrabber,ISignalGenerator signalGenerator)
        {
            _requestProcessor = requestProcessor;
            _historicalDataGrabber = historicalDataGrabber;
            _signalGenerator = signalGenerator;
        }
        public ActionResult Index(Symbol symbol = Symbol.SOLUSDT, Timeframe timeframe = Timeframe.M15)
        {
            return View(_requestProcessor.GetTradeDashboard(symbol,timeframe));
        }
        public ActionResult RefreshCandles(Symbol symbol,Timeframe timeframe)
        {
            var lastCandle = _historicalDataGrabber.RefreshCandles(symbol,timeframe);
            return Json(new { hasValue = true, lastCandle = lastCandle == DateTime.MinValue ? "" : lastCandle.ToString() });
        }
        public ActionResult GetLastCandle(Symbol symbol, Timeframe timeframe)
        {
            var lastCandle = _historicalDataGrabber.GetLastCandle(symbol, timeframe);
            return Json(new { hasValue = true, lastCandle = lastCandle == DateTime.MinValue ? "" : lastCandle.ToString() });
        }
        public ActionResult MarketDataCredentials(Symbol symbol = Symbol.SOLUSDT, Timeframe timeframe = Timeframe.M15)
        {
            return View(_requestProcessor.GetMarketDataCredentials(symbol, timeframe));
        }
        [HttpPost]
        public ActionResult AddCredential(MarketDataCredential credential)
        {
            if (_requestProcessor.PostMarketDataCredential(credential))
                TempData["CredentialSuccess"] = "credential created successfully";
            else
                TempData["CredentialError"] = "error occured in creating credential.try again later";
            return RedirectToAction("MarketDataCredentials", "Trades",new { symbol = credential.Symbol, timeframe = credential.Timeframe });
        }
        public ActionResult DeleteCredential(Guid id, Symbol symbol = Symbol.SOLUSDT, Timeframe timeframe = Timeframe.M15)
        {
            if (_requestProcessor.DeleteMarketDataCredential(id))
                TempData["CredentialSuccess"] = "credential deleted successfully";
            else
                TempData["CredentialError"] = "error occured in deleted credential.try again later";
            return RedirectToAction("MarketDataCredentials","Trades",new { symbol = symbol, timeframe = timeframe });
        }
        public ActionResult SignalResults(Symbol symbol = Symbol.SOLUSDT, Timeframe timeframe = Timeframe.M15,int Month = 0)
        {
            if (Month == 0)
                Month = Helpers.Dates.CurrentMonth();
            return View(_requestProcessor.GetSignalResults(symbol,timeframe,Month));
        }
        public ActionResult AutoRefreshAll(Symbol symbol,Timeframe timeframe)
        {
            _historicalDataGrabber.AutoRefreshCandles();
            _signalGenerator.AutoRefreshSignals();
            return RedirectToAction("Index","Trades", new { symbol = symbol, timeframe = timeframe });
        }
        [HttpGet]
        public ActionResult GetSignalEstimates(Symbol symbol = Symbol.SOLUSDT,Timeframe timeframe = Timeframe.M5)
        {
            //System.IO.File.WriteAllText(@"D:\tmp\estimates.csv", _signalGenerator.GetSignalEstimates(Symbol.SOLUSDT,Timeframe.M5));
            return File(Encoding.UTF8.GetBytes(_signalGenerator.GetSignalEstimates(symbol,timeframe)), "text/csv", "estimates.csv");
        }
        public ActionResult DeleteSignals(Symbol symbol, Timeframe timeframe)
        {
            _signalGenerator.DeleteSignals(symbol, timeframe);
            return RedirectToAction("SignalResults", "Trades",new { symbol = symbol, timeframe = timeframe });
        }
        public ActionResult DownloadSignalResult(Symbol symbol, Timeframe timeframe)
        {
            return File(Encoding.UTF8.GetBytes(_signalGenerator.GetSignalsWithResult(symbol, timeframe)), "text/csv", $"results_{symbol.ToString()}_{timeframe.ToString()}.csv");
        }

        //windows service methods
        [AllowAnonymous]
        [HttpPost]
        public ActionResult AutoRefreshCandles()
        {
            var result = _historicalDataGrabber.AutoRefreshCandles();
            string message = "";
            result.Item2.ForEach(x => { message += $"{x.Item1} - {x.Item2},"; });
            return Json(new { hasError = result.Item1, message = message });
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult AutoRefreshSignals()
        {
            _signalGenerator.AutoRefreshSignals();
            return Json(new { hasValue = true });
        }
    }
}