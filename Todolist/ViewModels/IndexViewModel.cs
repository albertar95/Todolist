using System;
using System.Collections.Generic;
using Todolist.Models;

namespace Todolist.ViewModels
{
    public class IndexViewModel
    {
        public List<Progress> Progresses { get; set; }
        public List<Models.Schedule> Schedules { get; set; }
        public List<Models.Task> Tasks { get; set; }
        public List<Models.Task> AllTasks { get; set; }
        public List<Goal> Goals { get; set; }
        public List<Goal> AllGoals { get; set; }
        public string[] DatePeriodInfo { get; set; }
        public string[] PersianDatePeriodInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
