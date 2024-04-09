using System;
using System.Collections.Generic;
using Todolist.Models;

namespace Todolist.ViewModels
{
    public class ProgressViewModel
    {
        public List<Models.Task> Tasks { get; set; }
        public List<Schedule> Schedules { get; set; }
        public Progress Progress { get; set; }
    }
}
