using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class Contracts
    {
        public Contracts()
        {
            ContractsInvoices = new HashSet<ContractsInvoices>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public int isAccepted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public decimal Hours { get; set; }
        public decimal Rate { get; set; }
        public int IsDeleted { get; set; }
        public int Method { get; set; }
        public int AutomaticBilling { get; set; }
        public int AutomaticDeposit { get; set; }
        public int AutomaticRelease { get; set; }
        public int IsArchived { get; set; }
        public virtual AspNetUsers User { get; set; }
        public virtual ICollection<ContractsInvoices> ContractsInvoices { get; set; }
    }
}
