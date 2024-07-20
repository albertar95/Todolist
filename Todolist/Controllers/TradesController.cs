using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
            _historicalDataGrabber.RefreshCandles(symbol,timeframe);
            return Json(new { });
        }
    }
}