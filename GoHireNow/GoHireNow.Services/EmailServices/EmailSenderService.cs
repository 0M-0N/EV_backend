using GoHireNow.Service.Interfaces;
using System;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace GoHireNow.Service.EmailServices
{
    public class EmailSenderService : IEmailSender
    {
        public void SendEmailAsync(string email, string subject, string message,string template)
        {
            this.SendEmailUsingAmazonSMTP(email, subject, message,template);
        }

        public void SendEmailConfirmationAsync(string email, string link, string template)
        {
            var subject = "Confirm your email";
            var body = $"Please confirm your account by clicking this link: <a href='{HttpUtility.UrlEncode(link)}'>link</a>";
            this.SendEmailUsingAmazonSMTP(email, subject, body, template);
        }

        public void SendNewCompanyEmailAsync(string email, string subject, string message, string template)
        {
            this.SendEmailUsingAmazonSMTP(email, subject, message, template);
        }

        public void SendNewWorkerEmailAsync(string email, string subject, string message, string template)
        {
            this.SendEmailUsingAmazonSMTP(email, subject, message, template);
        }

        private void SendEmailUsingAmazonSMTP(string toEmail, string subject, string message,string template)
        {
            using (MailMessage messageObj = new MailMessage
            {
                IsBodyHtml = true,
                BodyEncoding =  System.Text.Encoding.UTF8,
                From = new MailAddress("no-reply@evirtualassistants.com", "eVirtualAssistants")
            })
            {
                messageObj.To.Add(new MailAddress(toEmail));
                messageObj.Subject = subject;
                messageObj.Body = template;
                using (var client = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587))
                {
                    client.Credentials =
                        new NetworkCredential("AKIAR42GWVPWKV3X2R7V", "BHGDkUT/Aii4IXBNOO8UOA0PEeNkgwiqAIjItX9bWNaB");
                    client.EnableSsl = true;
                    try
                    {
                        client.Send(messageObj);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }

    }
}
