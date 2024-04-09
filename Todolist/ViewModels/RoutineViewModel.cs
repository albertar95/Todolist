using System;
using System.Collections.Generic;
using Todolist.Models;

namespace Todolist.ViewModels
{
    public class RoutineViewModel
    {
        public List<Routine> Routines { get; set; }
        public List<RoutineProgress> RoutineProgresses { get; set; }
        public string[] DatePeriodInfo { get; set; }
        public string[] PersianDatePeriodInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
