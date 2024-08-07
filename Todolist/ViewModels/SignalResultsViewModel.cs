using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Todolist.Models.Dto;
using static Todolist.Models.TradeModels;

namespace Todolist.ViewModels
{
    public class SignalResultsViewModel
    {
        public Symbol Symbol { get; set; }
        public Timeframe Timeframe { get; set; }
        public List<SignalResultDto> SignalResults { get; set; }
        public Tuple<int, int, int,int> MonthlyCardStat { get; set; }
        public Tuple<double, double, double,double> MonthlyCardStatPercentage { get; set; }
        public Tuple<string, string> SignalResultsAreaChart { get; set; }
        public int CurrentMonth { get; set; }
    }
}