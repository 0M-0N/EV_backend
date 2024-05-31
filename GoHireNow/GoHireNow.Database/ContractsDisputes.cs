using System;

namespace GoHireNow.Database
{
    public partial class ContractsDisputes
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountProposed { get; set; }
        public decimal AmountResolved { get; set; }
        public string Description { get; set; }
        public int Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public int IsDeleted { get; set; }
        public int IsCompleted { get; set; }
    }
}
