//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Todolist.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SignalResult
    {
        public System.Guid Id { get; set; }
        public System.Guid SignalId { get; set; }
        public int Status { get; set; }
        public double ClosePrice { get; set; }
        public double ProfitPercentage { get; set; }
        public int Duration { get; set; }
        public System.DateTime CloseDate { get; set; }
        public System.DateTime CreateDate { get; set; }
        public int ClosureType { get; set; }
    
        public virtual Signal Signal { get; set; }
    }
}