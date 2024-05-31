using GoHireNow.Models.HireModels;
using GoHireNow.Models.JobsModels;
using GoHireNow.Models.WorkerModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IGlobalJobsService
    {
        Task<List<GlobalJobCategoriesListModel>> GetGlobalJobTitles();
        Task<JobTitleRelatedGlobalJobsModel> GetJobTitleRelatedJobs(int jobTitleId, string userId,int roleId);
        Task<JobDetailForWorkerResponse> GetJob(int jobId, string rootPath, string userId, int roleId);
    }

}
