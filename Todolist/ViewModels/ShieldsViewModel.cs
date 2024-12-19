using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Todolist.Models;

namespace Todolist.ViewModels
{
    public class ShieldsViewModel
    {
        public List<Shield> Shields { get; set; }
        public bool HasMasterPass { get; set; } = false;
        public string MasterPass { get; set; } = string.Empty;
    }
}