namespace GoHireNow.Models.ContractModels
{
    public class CreateModel
    {
        public string Name { get; set; }
        public string ContractName { get; set; }
        public string ToUserId { get; set; }
        public decimal Hours { get; set; }
        public decimal Rate { get; set; }
    }
}
