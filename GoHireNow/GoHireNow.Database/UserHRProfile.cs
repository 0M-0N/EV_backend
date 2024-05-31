using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserHRProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string HRTitle1 { get; set; }
        public string HRTitle2 { get; set; }
        public string HRTitle3 { get; set; }
        public string HRDesc1 { get; set; }
        public string HRDesc2 { get; set; }
        public string HRDesc3 { get; set; }
        public decimal HRPrice { get; set; }
        public string HRVideo { get; set; }
        public int HRWpm { get; set; }
        public int HRMbps { get; set; }
        public string HRNotes { get; set; }
        public string HRPosition { get; set; }
        public int HRHours { get; set; }
        public string HRType { get; set; }
        public virtual AspNetUsers User { get; set; }
    }
}
