using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Todolist.Models;
using Todolist.Services.Contracts;
using static Todolist.Models.TradeModels;

namespace Todolist.Controllers
{
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
            return RedirectToAction("MarketDataCredentials", "Trades");
        }
        public ActionResult DeleteCredential(Guid id)
        {
            if (_requestProcessor.DeleteMarketDataCredential(id))
                TempData["CredentialSuccess"] = "credential deleted successfully";
            else
                TempData["CredentialError"] = "error occured in deleted credential.try again later";
            return RedirectToAction("MarketDataCredentials","Trades");
        }

        //windows service methods
        [HttpPost]
        public ActionResult AutoRefreshCandles()
        {
            _historicalDataGrabber.AutoRefreshCandles();
            return Json(new { hasValue = true });
        }
        [HttpPost]
        public ActionResult AutoRefreshSignals()
        {
            _signalGenerator.AutoRefreshSignals();
            return Json(new { hasValue = true });
        }
        public ActionResult AutoRefreshAll(Symbol symbol,Timeframe timeframe)
        {
            _historicalDataGrabber.AutoRefreshCandles();
            _signalGenerator.AutoRefreshSignals();
            return RedirectToAction("Index","Trades", new { symbol = symbol, timeframe = timeframe });
        }
    }
}