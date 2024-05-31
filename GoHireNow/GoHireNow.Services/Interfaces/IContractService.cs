using GoHireNow.Models.ContractModels;
using GoHireNow.Models.PayoutTransactionModels;
using System.Threading.Tasks;
using GoHireNow.Database;
using System.Collections.Generic;

namespace GoHireNow.Service.Interfaces
{
    public interface IContractService
    {
        Task<CreateContractResponse> CreateContract(string userId, CreateModel model);
        Task<int> AddContract(Contracts contract);
        Task<bool> EndContract(string userId, int contractid, string ip);
        Task<List<EndContractResponse>> WeeklyEndContract();
        Task<bool> UpdateContractStatus(string userId, UpdateStatusModel model);
        Task<PayoutRecipients> GetPayoutRecipient(string userId);
        Task<ContractDetailResponse> GetContractDetails(string userId, int contractid);
        Task<ContractMoreDetailResponse> GetContractSecured(string userId, int contractid);
        Task<ContractMoreDetailResponse> GetContractUnbilled(string userId, int contractid);
        Task<ContractMoreDetailResponse> GetContractBalance(string userId, int contractid);
        Task<ContractMoreDetailResponse> GetAccountBalance(string userId, int contractid);
        Task<ContractMoreDetailResponse> GetContractStatus(string userId, int contractid);
        Task<ContractMoreDetailResponse> GetContractTotalRevenue(string userId, int contractid);
        Task<ContractMoreDetailResponse> GetContractCommission(string userId, int contractid);
        Task<ContractMoreDetailResponse> GetContractLastPayment(string userId, int contractid);
        Task<bool> DeleteContract(string userId, int contractid);
        Task<List<ContractDetailResponse>> ListContractsCompany(string userId);
        Task<List<ContractDetailResponse>> ListContractsUser(string userId);
        Task<bool> isautomaticbilling(string userId, UpdateAutomaticBillingModel model);
        Task<int> GetActiveContractsCount(string userId);
        Task<bool> isautomaticdeposit(string userId, UpdateAutomaticDepositModel model);
        Task<bool> isautomaticrelease(string userId, UpdateAutomaticReleaseModel model, string ip);
        Task<bool> Archive(string userId, int contractId);
        Task<bool> UpdateHours(string userId, UpdateHoursModel model);
        Task<bool> AddHour(string userId, AddHourModel model);
        Task<bool> DeleteHour(int id, string userId);
        Task<bool> DeletePayoutTransaction(int id);
        Task<List<ContractsHours>> GetWorkingHours(int contractId, int week);
        Task<List<PayoutTransactions>> GetPayoutTransactions(string userId);
        Task<int> CreatePayoutTransaction(CreatePayoutTransactionModel model, string userId);
        Task<List<ContractReleaseInvoiceResponse>> GetReleaseInvoices(int ContractId, string userId);
        Task<bool> Release(int ContractId, string ip, string userId);
        Task<int> GetUnapprovalCount(string userId);
        Task<int> GetUndepositCount(string userId);
        Task NewMailService(int customId, int customType, string emailTo, string nameTo, string subject, string headtitle, string buttonurl, string buttoncaption, string description, string text, string emailFrom, string nameFrom, int priority, string fileName, string howtotext = "", string howtourl = "", string img = "");
        Task PersonalEmailService(int customId, int customType, string emailTo, string nameTo, string subject, string clientName, string vaName, string emailFrom, string nameFrom, int priority, string fileName);
        Task<bool> Dispute(DisputeModel model, string userId);
        Task<bool> SendReport(string userId, ReportModel model);
        Task<bool> CheckContracted(string userId, string toUserId);
        Task<List<HRPremiumContracts>> GetHRContract(string workerId);
        Task<bool> AddHRContract(string companyId, string workerId);
        Task<bool> ApprovePayoutTransaction(int id, string receiptId);
    }

    public class CreateContractResponse
    {
        public bool Status { get; set; }
        public int Id { get; set; }
    }

    public class EndContractResponse
    {
        public int contractId { get; set; }
        public decimal amountDue { get; set; }
        public string stripeChargeId { get; set; }
    }
}
