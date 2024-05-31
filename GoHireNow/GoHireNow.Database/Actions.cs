using System;

namespace GoHireNow.Database
{
    public partial class Actions
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int Type { get; set; }
        public DateTime? RunnedDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int Processed { get; set; }
        public decimal CustomAmount { get; set; }
        public string CustomReceipt { get; set; }
        public int CustomContractId { get; set; }
        public int? CustomPayoutId { get; set; }
        public int? IsApproved { get; set; }
        public int? CustomType { get; set; }
    }
}
