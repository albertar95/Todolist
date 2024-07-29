using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Todolist.Models.TradeModels;

namespace Todolist.Models.Dto
{
    public class SignalResultDto
    {
        public Guid Id { get; set; }
        public Guid SignalId { get; set; }
        public Symbol Symbol { get; set; }
        public Timeframe Timeframe { get; set; }
        public SignalResultStatus Status { get; set; }
        public double ClosePrice { get; set; }
        public double ProfitPercentage { get; set; }
        public int Duration { get; set; }
        public DateTime CloseDate { get; set; }
        public DateTime CreateDate { get; set; }
        public SignalResultClosureTypes ClosureType { get; set; }
        public SignalTypes SignalType { get; set; }
        public DateTime StartDate { get; set; }
        public double EnterPrice { get; set; }
        public double StopLostPrice { get; set; }
        public double TakeProfitPrice { get; set; }
        public bool IsActive { get; set; }
        public byte WinChanceEstimate { get; set; }
        public SignalProviders SignalProvider { get; set; }
    }
}