using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Todolist.Models;

namespace Todolist.ViewModels
{
    public class FinancialReportViewModel
    {
        public int CurrentMonth { get; set; }
        public Tuple<string, string,decimal> MonthlySpenceBarChart { get; set; }
        public Tuple<string, string, decimal> GroupMonthlySpenceBarChart { get; set; }
        public Tuple<string, string, decimal> GroupMonthlyIncomeBarChart { get; set; }
        public Tuple<string, string, decimal> MonthlyIncomeBarChart { get; set; }
        public Tuple<decimal,decimal,decimal> YearlyCardStat { get; set; }
        public Tuple<string, string, decimal> MonthSpencesBarChart { get; set; }
        public Tuple<string, string, decimal> TopFiveGroupBarChart { get; set; }
        public Tuple<string, string, decimal> GroupSpenceBarChart { get; set; }
        public Tuple<string, string, decimal> GroupIncomeBarChart { get; set; }
        public Tuple<string, string> FundDistributionPieChart { get; set; }
        public Tuple<string, string> FundAccumulationAreaChart { get; set; }
        public List<TransactionGroup> Groups { get; set; }
        public decimal TotalCurrentMonthSpence { get; set; }
        public decimal TotalCurrentMonthIncome { get; set; }
    }
}