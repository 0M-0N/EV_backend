using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class HRPremiumContracts
    {
        public int Id { get; set; }
        public string CompanyId { get; set; }
        public string ClientName { get; set; }
        public string ClientEmail { get; set; }
        public string WorkerId { get; set; }
        public decimal Hourly { get; set; }
        public int Status { get; set; }
        public int ContractId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastBilledDate { get; set; }
        public int IsDeleted { get; set; }
    }
}
