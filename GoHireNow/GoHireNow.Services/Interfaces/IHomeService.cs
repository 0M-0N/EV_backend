using GoHireNow.Models.HomeModels;

namespace GoHireNow.Service.Interfaces
{
    public interface IHomeService
    {
        bool SubmitInquiry(ContactUsResponse model);
    }
}
