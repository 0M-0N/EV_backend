namespace GoHireNow.Models.ContractModels
{
    public class AddHourModel
    {
        public decimal workHour { get; set; }
        public string workDate { get; set; }
        public string description { get; set; }
        public int contractId { get; set; }
    }
}
