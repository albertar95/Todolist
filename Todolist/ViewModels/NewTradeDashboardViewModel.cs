using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Todolist.Models;
using Todolist.Models.Dto;
using static Todolist.Models.TradeModels;

namespace Todolist.ViewModels
{
    public class NewTradeDashboardViewModel
    {
        public SignalResultsViewModel SignalResultsVM { get; set; }
        public AugmentedCandle Candle { get; set; }
        public SignalEstimate Estimate { get; set; }
        public SignalDto Signal { get; set; }
        public SignalProgress SignalProgress { get; set; }
        public Tuple<int, int, int, int> AllTimeStat { get; set; }
        public Tuple<double, double, double, double> AllTimeStatPercentage { get; set; }
    }
}