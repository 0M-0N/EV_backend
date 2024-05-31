using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IPricingService
    {
        Task<bool> CanClientPostJob(string userId);
        Task<bool> CanWorkerApplyForThisJob(int jobId, string clientId, string workerId);
        Task<ClientCurrentPlanResponse> GetSubscriptionDetails(string userId);
        Task<WorkerCurrentPlanResponse> GetWorkerSubscriptionDetails(string userId);
        Task<GlobalPlanDetailResponse> GetCurrentPlan(string userId);
        Task<int> GetAllowedJobsByClientId(string userId);
        UserCapablePricingPlanResponse IsCapable(string userId, string entryType, string toUserId);
        Task<IEnumerable<GlobalPlanDetailResponse>> GetGlobalPlanDetails();
    }
}
