using System;
using System.Collections.Generic;
using Todolist.Models;

namespace Todolist.ViewModels
{
    public class GoalPageViewModel
    {
        public Goal Goal { get; set; }
        public List<Models.Task> Tasks { get; set; }
        public List<Progress> Progresses { get; set; }
    }
}
