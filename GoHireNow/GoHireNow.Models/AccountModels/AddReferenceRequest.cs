namespace GoHireNow.Models.AccountModels
{
    public class AddReferenceRequest
    {
        public string JobTitle { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string Email { get; set; }
    }
}
