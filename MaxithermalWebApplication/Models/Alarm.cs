//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MaxithermalWebApplication.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Alarm
    {
        public string Serial { get; set; }
        public bool AlarmStatus1 { get; set; }
        public double HighAlarmTemp { get; set; }
        public double LowAlarmTemp { get; set; }
        public bool AlarmStatus2 { get; set; }
        public double HighAlarmHumid { get; set; }
        public double LowAlarmHumid { get; set; }
        public double TttAlarm1 { get; set; }
        public double TttLowAlarm1 { get; set; }
        public double TttAlarm2 { get; set; }
        public double TttLowAlarm2 { get; set; }
        public string TimeUpdated { get; set; }
    
        public virtual Setting Setting { get; set; }
    }
}
