using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class Ipaddresses
    {
        public int Id { get; set; }
        public double Ipfrom { get; set; }
        public double Ipto { get; set; }
        public int? CountryId { get; set; }
        public string CountryCode { get; set; }
    }
}
