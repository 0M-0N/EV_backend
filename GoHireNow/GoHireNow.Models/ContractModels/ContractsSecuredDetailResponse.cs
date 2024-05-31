using System;

namespace GoHireNow.Models.ContractModels
{
    public class ContractsSecuredDetailResponse
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public decimal Amount { get; set; }
        public int Method { get; set; }
        public DateTime? PeriodDate { get; set; }
        public DateTime? endDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int Type { get; set; }
        public int? InvoiceId { get; set; }
        public int IsDeleted { get; set; }
    }
}
