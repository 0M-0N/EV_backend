using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.StripeModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IStripePaymentService
    {
        Task<List<TransactionResponse>> GetAllTransactions(string userId);
        Task<List<TransactionResponse>> GetTransactionDetails(int jobId);
        TransactionResponse GetUserLastTransactions(string userId);
        Task<bool> DeleteStripePayment(string customerId, string cardId);
        List<TransactionResponse> GetUserTransactions(string userId);
        bool PostTransaction(Transactions model);
        Task<GlobalPlanDetailResponse> GetGlobalPlanDetail(int id);
        Task<bool> SendInvoiceToClient(string planName, string companyName, string invoiceNumber, string amount, string email);
        Task<int> CreateStripePayment(string userId, string customerId, string cardId, string paymentMethodId);
        Task<StripePayments> GetStripePayment(string customerId, string cardId);
    }
}
