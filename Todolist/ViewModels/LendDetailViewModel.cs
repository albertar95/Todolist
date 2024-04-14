using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Todolist.ViewModels
{
    public class LendDetailViewModel
    {
        public Guid NidAccount { get; set; }
        public string AccountName { get; set; }
        public decimal Amount { get; set; }
        public byte TransactionType { get; set; }
        public string TrType { get; set; }
    }
}