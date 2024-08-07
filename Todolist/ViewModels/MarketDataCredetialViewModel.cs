using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Todolist.Models.TradeModels;

namespace Todolist.ViewModels
{
    public class MarketDataCredetialViewModel
    {
        public List<Todolist.Models.MarketDataCredential> Credentials { get; set; }
        public Symbol Symbol { get; set; }
        public Timeframe Timeframe { get; set; }
    }
}