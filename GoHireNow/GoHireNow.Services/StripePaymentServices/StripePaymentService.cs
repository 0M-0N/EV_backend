using GoHireNow.Database;
using GoHireNow.Database.GoHireNowTools;
using GoHireNow.Database.GoHireNowTools.Models;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Models.StripeModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Service.StripePaymentServices
{
    public class StripePaymentService : IStripePaymentService
    {
        public async Task<List<TransactionResponse>> GetAllTransactions(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                return await _context.Transactions
                    .Include(x => x.GlobalPlan)
                    .Where(x => x.UserId == userId)
                    .Select(x => new TransactionResponse
                    {
                        Id = x.Id,
                        Amount = x.Amount,
                        UserId = x.UserId,
                        CardName = x.CardName,
                        GlobalPlanId = x.GlobalPlanId,
                        GlobalPlanName = x.GlobalPlanId.ToGlobalPlanName(),
                        Receipt = x.Receipt,
                        ReceiptId = x.ReceiptId,
                        Status = x.Status,
                        CreateDate = x.CreateDate,
                        CustomId = x.CustomId,
                        CustomType = x.CustomType,
                        IsDeleted = x.IsDeleted
                    }).ToListAsync();
            }
        }

        public async Task<List<TransactionResponse>> GetTransactionDetails(int jobId)
        {
            using (var _context = new GoHireNowContext())
            {
                return await _context.Transactions
                    .Include(x => x.GlobalPlan)
                    .Where(x => x.CustomId == jobId)
                    .Select(x => new TransactionResponse
                    {
                        Id = x.Id,
                        Amount = x.Amount,
                        UserId = x.UserId,
                        CardName = x.CardName,
                        GlobalPlanId = x.GlobalPlanId,
                        GlobalPlanName = x.GlobalPlanId.ToGlobalPlanName(),
                        Receipt = x.Receipt,
                        ReceiptId = x.ReceiptId,
                        Status = x.Status,
                        CreateDate = x.CreateDate,
                        CustomId = x.CustomId,
                        CustomType = x.CustomType,
                        IsDeleted = x.IsDeleted
                    }).ToListAsync();
            }
        }

        public TransactionResponse GetUserLastTransactions(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.Transactions
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(c => c.CreateDate)
                    .Select(x => new TransactionResponse
                    {
                        Id = x.Id,
                        Amount = x.Amount,
                        CardName = x.CardName,
                        GlobalPlanId = x.GlobalPlanId,
                        Receipt = x.Receipt,
                        ReceiptId = x.ReceiptId,
                        Status = x.Status,
                        UserId = x.UserId
                    }).FirstOrDefault();
            }
        }

        public List<TransactionResponse> GetUserTransactions(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.Transactions.Where(x => x.UserId == userId)
                    .OrderByDescending(c => c.CreateDate)
                    .Select(x => new TransactionResponse
                    {
                        Id = x.Id,
                        UserId = x.UserId,
                        Status = x.Status,
                        ReceiptId = x.ReceiptId,
                        Receipt = x.Receipt,
                        GlobalPlanId = x.GlobalPlanId,
                        CardName = x.CardName,
                        Amount = x.Amount
                    }).ToList();
            }
        }

        public bool PostTransaction(Transactions model)
        {
            using (var _context = new GoHireNowContext())
            {
                _context.Transactions.Add(model);
                _context.SaveChanges();
                return true;
            }
        }

        public async Task<bool> SendInvoiceToClient(string planName,string companyName,string invoiceNumber,string amount,string email)
        {
            using (var _context = new GoHireNowContext())
            {
                string htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "PaymentReceipt.html");
                htmlContent = htmlContent.Replace("[PlanName]", planName);
                htmlContent = htmlContent.Replace("[InvoiceNumber]", invoiceNumber);
                htmlContent = htmlContent.Replace("[CompanyName]", companyName);
                htmlContent = htmlContent.Replace("[Amount]", amount);
                htmlContent = htmlContent.Replace("[CurrentDate]", DateTime.UtcNow.ToString("dd-MM-yyyy"));

                using (var _toolsContext = new GoHireNowToolsContext())
                {
                    var sender = new mailer_sender();
                    sender.ms_custom_id = 0;
                    sender.ms_custom_type = 13;
                    sender.ms_date = DateTime.Now;
                    sender.ms_send_date = DateTime.Now;
                    sender.ms_email = email;
                    sender.ms_name = "";
                    sender.ms_subject = "Payment Receipt";
                    sender.ms_message = htmlContent;
                    sender.ms_from_email = "no-reply@evirtualassistants.com";
                    sender.ms_from_name = "eVirtualAssistants";
                    sender.ms_priority = 0;
                    sender.ms_issent = 0;
                    sender.ms_unsubscribe = Guid.NewGuid();

                    await _toolsContext.mailer_sender.AddAsync(sender);
                    await _toolsContext.SaveChangesAsync();
                }

                // using (MailMessage messageObj = new MailMessage
                // {
                //     IsBodyHtml = true,
                //     BodyEncoding = System.Text.Encoding.UTF8,
                //     From = new MailAddress("no-reply@evirtualassistants.com", "eVirtualAssistants")
                // })
                // {
                //     messageObj.To.Add(new MailAddress(email));
                //     messageObj.Subject = "Payment Receipt";
                //     messageObj.Body = htmlContent;
                //     using (var client = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587))
                //     {
                //         client.Credentials =
                //             new NetworkCredential("AKIAIA5SSI4EFREFBKVQ", "AjeA3B59Qu1048gPkxN+xD5nw1uxduuYsVaojohTUa0A");
                //         client.EnableSsl = true;
                //         try
                //         {
                //             client.Send(messageObj);
                //         }
                //         catch (Exception ex)
                //         {

                //         }
                //     }
                // }
            }
            return true;
        }

        public async Task<GlobalPlanDetailResponse> GetGlobalPlanDetail(int id)
        {
            using (var _context = new GoHireNowContext())
            {
               var plan = await _context.GlobalPlans.FirstOrDefaultAsync(o=>o.Id == id);
                return new GlobalPlanDetailResponse()
                {
                    AccessId = plan.AccessId,
                    Id = plan.Id,
                    Name = plan.Name,
                    Price = plan.Price,
                    IsActive = plan.IsActive
                };
            }
        }

        public async Task<int> CreateStripePayment(string userId, string customerId, string cardId, string paymentMethodId)
        {
            StripePayments stripePayment = MapStripePayment(userId, customerId, cardId, paymentMethodId);

            return await AddStripePayment(stripePayment);
        }

        public async Task<StripePayments> GetStripePayment(string customerId, string cardId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    StripePayments stripePayment;
                    var keyid = _context.StripePayments.Where(x => x.CustomerId == customerId && x.CardId == cardId && x.IsDeleted == false).Select(j => j.Id).FirstOrDefault();

                    if (keyid > 0)
                    {
                        stripePayment = await _context.StripePayments.FindAsync(keyid);
                        return stripePayment;
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> DeleteStripePayment(string customerId, string cardId)
        {

            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var keyid = _context.StripePayments.Where(x => customerId == x.CustomerId && cardId == x.CardId).Select(j => j.Id).FirstOrDefault();

                    if (keyid > 0)
                    {
                        var stripePayment = await _context.StripePayments.FindAsync(keyid);
                        if (stripePayment != null)
                        {
                            stripePayment.IsDeleted = true;
                            await _context.SaveChangesAsync();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else { return false; }
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private StripePayments MapStripePayment(string userid, string CustomerId, string CardId, string PaymentMethodId)
        {
            var stripePayment = new StripePayments();
            try
            {
                stripePayment.CustomerId = CustomerId;
                stripePayment.CardId = CardId;
                stripePayment.PaymentMethodId = PaymentMethodId;
                stripePayment.IsDeleted = false;
                stripePayment.CreatedDate = DateTime.UtcNow;
            }
            catch (Exception)
            {
                throw new CustomException(500, "Error mapping stripe payment request with required model");
            }

            return stripePayment;
        }

        private async Task<int> AddStripePayment(StripePayments stripePayment)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    await _context.StripePayments.AddAsync(stripePayment);
                    await _context.SaveChangesAsync();
                    return stripePayment.Id;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
