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
    
    public partial class NoteGroup
    {
        public NoteGroup()
        {
            this.Notes = new HashSet<Note>();
        }
    
        public System.Guid NidGroup { get; set; }
        public string Title { get; set; }
        public System.DateTime CreateDate { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public System.Guid UserId { get; set; }
    
        public virtual ICollection<Note> Notes { get; set; }
        public virtual User User { get; set; }
    }
}
