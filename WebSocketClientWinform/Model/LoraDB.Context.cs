﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace UDPServerAndWebSocketClient.Model
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class Pexo63LorawanEntities : DbContext
    {
        public Pexo63LorawanEntities()
            : base("name=Pexo63LorawanEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Data> Data1 { get; set; }
        public virtual DbSet<Realtime> Realtimes { get; set; }
        public virtual DbSet<Alarm> Alarms { get; set; }
        public virtual DbSet<Setting> Settings { get; set; }
        public virtual DbSet<Device> Devices { get; set; }
        public virtual DbSet<sysdiagram> sysdiagrams { get; set; }
    }
}
