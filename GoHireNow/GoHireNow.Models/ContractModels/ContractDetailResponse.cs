using System;

namespace GoHireNow.Models.ContractModels
{
    public class ContractDetailResponse
    {
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
        public decimal Unbilled { get; set; }
        public DateTime? UnbilledDate { get; set; }
        public decimal LastPayment { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public string Avatar { get; set; }
        public string CompanyName { get; set; }
        public string WorkerName { get; set; }
        public int SecurityStatusId { get; set; }
        public bool IsDisputed { get; set; }
        public int IsArchived { get; set; }
    }
}
