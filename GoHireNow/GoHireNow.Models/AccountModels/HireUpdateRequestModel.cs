namespace GoHireNow.Models.AccountModels
{
    public class HireUpdateRequestModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string CompanyName { get; set; }
        public string CompanyIntroduction { get; set; }
        public int CountryId { get; set; }
    }
}
