namespace GoHireNow.Service.Interfaces
{
    public interface IEmailSender
    {
        void SendEmailAsync(string email, string subject, string message,string template);
        void SendNewWorkerEmailAsync(string email, string subject, string message, string template);
        void SendNewCompanyEmailAsync(string email, string subject, string message, string template);
        void SendEmailConfirmationAsync(string email, string link, string template);
    }
}
