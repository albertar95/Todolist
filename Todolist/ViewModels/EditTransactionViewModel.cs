﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Todolist.Models;

namespace Todolist.ViewModels
{
    public class EditTransactionViewModel
    {
        public List<Account> Accounts { get; set; }
        public Transaction Transaction { get; set; }
        public DateTime StartOfMonth { get; set; }
        public string[] bgColor { get; set; } = new string[] { "aquamarine", "burlywood", "lemonchiffon", "azure", "cadetblue", "chartreuse", "lightcoral", "lightsteelblue", "plum", "lightseagreen", "peru", "cornflowerblue", "darkgray", "darkkhaki", "lightblue", "bisque", "violet", "mediumseagreen", "palegreen", "paleturquoise", "tan", "hotpink", "cyan", "thistle", "goldenrod", "darksalmon" };
        public static List<Tuple<byte, string>> TransactionTypes { get; set; } = new List<Tuple<byte, string>>()
        { new Tuple<byte, string>(1,"Pay"),new Tuple<byte, string>(2,"Lend"),new Tuple<byte, string>(3,"LendBack") };
        public Guid NextTrId { get; set; }
        public List<TransactionGroup> Groups { get; set; }
    }
}