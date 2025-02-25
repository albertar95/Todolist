﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class ToDoListDbEntities : DbContext
    {
        public ToDoListDbEntities()
            : base("name=ToDoListDbEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Goal> Goals { get; set; }
        public DbSet<NoteGroup> NoteGroups { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<RoutineProgress> RoutineProgresses { get; set; }
        public DbSet<Routine> Routines { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Shield> Shields { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<MarketDataCredential> MarketDataCredentials { get; set; }
        public DbSet<TransactionGroup> TransactionGroups { get; set; }
        public DbSet<SignalResult> SignalResults { get; set; }
        public DbSet<Signal> Signals { get; set; }
        public DbSet<NotifyLog> NotifyLogs { get; set; }
        public DbSet<AugmentedCandle> AugmentedCandles { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
