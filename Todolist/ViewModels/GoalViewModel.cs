using System;
using System.Collections.Generic;
using Todolist.Models;

namespace Todolist.ViewModels
{
    public class GoalViewModel
    {
        public List<Goal> Goals { get; set; }
        public List<Models.Task> Tasks { get; set; }
        public string[] bgColor { get; set; } = new string[] { "aquamarine", "burlywood", "lemonchiffon", "azure", "cadetblue", "chartreuse", "lightcoral", "lightsteelblue", "plum", "lightseagreen", "peru", "cornflowerblue", "darkgray", "darkkhaki", "lightblue", "bisque", "violet", "mediumseagreen", "palegreen", "paleturquoise", "tan", "hotpink", "cyan", "thistle", "goldenrod", "darksalmon" };
    }
}
