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
        public TradesController(IRequestProcessor requestProcessor,IHistoricalDataGrabber historicalDataGrabber)
        {
            _requestProcessor = requestProcessor;
            _historicalDataGrabber = historicalDataGrabber;
        }
        public ActionResult Index()
        {
            return View(_requestProcessor.GetTradeDashboard());
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
        public ActionResult MarketDataCredentials(Symbol symbol = Symbol.EURUSD, Timeframe timeframe = Timeframe.M5)
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
        public ActionResult AutoRefreshCandles()
        {
            _historicalDataGrabber.AutoRefreshCandles();
            return Json(new { hasValue = true });
        }
        public ActionResult AutoRefreshSignals()
        {
            _historicalDataGrabber.AutoRefreshCandles();
            return Json(new { hasValue = true });
        }
    }
}