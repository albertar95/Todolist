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
    
    public partial class Progress
    {
        public System.Guid NidProgress { get; set; }
        public System.Guid ScheduleId { get; set; }
        public short ProgressTime { get; set; }
        public System.DateTime CreateDate { get; set; }
        public string Description { get; set; }
        public System.Guid UserId { get; set; }
    
        public virtual Schedule Schedule { get; set; }
        public virtual User User { get; set; }
    }
}
