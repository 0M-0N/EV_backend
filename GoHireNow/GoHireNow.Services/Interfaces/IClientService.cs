using GoHireNow.Database;
using GoHireNow.Database.ComplexTypes;
using GoHireNow.Models.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IClientService
    {
        Task<ClientDashboardResponse> GetDashboard(string userId, int roleId);
        Task<List<Transactions>> GetClientTransaction(string id);
        Task<List<WorkerSummaryForClientResponse>> GetWorkersByIds(List<string> ids, int roleId, string userId);
        Task<List<WorkerSummaryForClientResponse>> GetRelatedWorker(string clientId, int roleId, int page, int size);
        Task<ClientSearchWorkerResponse> SearchWorkers(SearchWorkerRequest model, int roleId, string userId);
        Task<spGetTotalCoAccountBalanced> GetAccountBalance(string userId);
        Task<ClientProfileProgressResponse> GetProfileProgress(string userId);
        Task<List<HRProfilesResponse>> GetHRAccounts(int size, int page);
        Task<List<WorkerSummaryForClientResponse>> FeaturedWorkers(SearchWorkerRequest model, int roleId, string userId);
        Task<ClientHRProfileResponse> GetHRAccountDetail(string id);
        Task<bool> UpdateToFreePlan(string userId);
        Task<bool> ProcessCurrentPricingPlan(string userId);
        Task<int> PayByBalance(decimal amount, string userId, int type);
        Task<bool> RequestReferences(EmailsRequest model);
        Task<bool> SentRequest(EmailsRequest model);
    }
}
