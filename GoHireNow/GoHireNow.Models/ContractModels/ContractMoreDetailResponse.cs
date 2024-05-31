using System;

namespace GoHireNow.Models.ContractModels
{
    public class ContractMoreDetailResponse
    {
        public decimal securedAmount { get; set; }
        public decimal unbilled { get; set; }
        public decimal lastPayment { get; set; }
        public decimal contractBalance { get; set; }
        public int securityStatusId { get; set; }
        public decimal securityAmount { get; set; }
        public decimal totalRevenue { get; set; }
        public decimal current { get; set; }
        public decimal next { get; set; }
        public decimal amount { get; set; }
    }
}
