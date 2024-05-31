using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserHRLanguages
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string HRLang { get; set; }
        public int HRWrite { get; set; }
        public int HRSpeak { get; set; }
    }
}
