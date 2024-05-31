namespace GoHireNow.Models.AccountModels
{
    public class AddReferenceForContractRequest
    {
        public string Feedback { get; set; }
        public string UserId { get; set; }
        public int ContractId { get; set; }
        public string Company { get; set; }
        public string Email { get; set; }
        public int Rate { get; set; }
    }
}
