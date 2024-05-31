using GoHireNow.Models.ClientModels;
using GoHireNow.Models.HireModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IHireService
    {
        Task<List<GlobalJobCategoriesListModel>> GetGlobalJobTitles();
        Task<WorkerSummaryForHireResponse> GetJobTitleRelatedWorkers(int jobTitleId, int size, int page, string userId,int roleId);
        Task<List<WorkerSummaryForClientResponse>> GetRelatedWorkersOffline(string UserId, int size, int page);
        Task<List<JobTitleRelatedWorkersModel>> GetWorkerProfile(string userId,string log_UserId,int roleId);
    }

}
    