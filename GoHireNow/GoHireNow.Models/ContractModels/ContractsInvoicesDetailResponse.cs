using System;

namespace GoHireNow.Models.ContractModels
{
    public class ContractsInvoicesDetailResponse
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? PayoutDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal Hours { get; set; }
        public decimal Amount { get; set; }
        public decimal PayoutCommission { get; set; }
        public int StatusId { get; set; }
        public int PayoutStatusId { get; set; }
        public int? InvoiceType { get; set; }
        public int? PayoutId { get; set; }
        public int IsDeleted { get; set; }
        public string Comment { get; set; }
        public int? SecuredId { get; set; }
        public decimal? ContractRate { get; set; }
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string ContractName { get; set; }
        public string WorkerName { get; set; }
    }
}
