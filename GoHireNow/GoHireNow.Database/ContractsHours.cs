using System;

namespace GoHireNow.Database
{
    public partial class ContractsHours
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? WorkedDate { get; set; }
        public decimal Hours { get; set; }
        public string Description { get; set; }
        public int IsDeleted { get; set; }
    }
}
