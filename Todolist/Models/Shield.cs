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
    
    public partial class Shield
    {
        public System.Guid Id { get; set; }
        public System.DateTime CreateDate { get; set; }
        public Nullable<System.DateTime> LastModified { get; set; }
        public string Title { get; set; }
        public string TargetUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public System.Guid UserId { get; set; }
    
        public virtual User User { get; set; }
    }
}
