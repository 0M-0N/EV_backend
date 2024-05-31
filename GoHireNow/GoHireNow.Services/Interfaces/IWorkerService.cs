using GoHireNow.Database;
using GoHireNow.Database.ComplexTypes;
using GoHireNow.Models.AccountModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.JobModels;
using GoHireNow.Models.JobsModels;
using GoHireNow.Models.WorkerModels;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IWorkerService
    {
        Task<WorkerDashboardResponse> GetDashboard(string userId, int roleId);
        Task<bool> AddPortifolios(string userId, string path, IFormFile[] portifolios);
        Task<int> DeletePortifolio(string userId, int id);
        Task<int> DeleteYoutube(string userId, int id);
        Task<List<SkillResponse>> GetWorkerSkills(string userId);
        Task<List<PortfolioResponse>> GetPortfolios(string userId, bool isFull = true);
        Task<List<YoutubeResponse>> GetYoutubes(string userId, bool isFull = true);
        Task<WorkerSearchJobResponse> SearchJobs(SearchJobRequest model, string rootPath, int roleId, string userId);
        Task<WorkerFeaturedJobResponse> FeaturedJobs(SearchJobRequest model, string rootPath, int roleId, string userId);
        Task<bool> ChatExist(string userId, string clientId);
        Task<WorkerProfileProgressResponse> GetProfileProgress(string userId);
        Task<spGetTotalAccountBalanced> GetAccountBalance(string userId);
        Task<spGetGlobalGroupByCountry> GetGlobalGroupByCountry(int countryId);
        Task<List<JobSummaryResponse>> GetJobs(string userId, int page, int size, int status, int roleId, string workerId, bool isactive = true);
        Task<List<JobTitleRelatedGlobalJobsListModel>> GetSkillsRelatedJobs(string skillIds, string userId, int roleId);
        Task<bool> AddYoutube(string userId, AddYoutubeModel model);
        Task<int> AddReference(string userId, AddReferenceRequest model);
        Task<int> AddReferenceForContract(string userId, AddReferenceForContractRequest request);
        Task<List<Academy>> GetAcademies(string userId);
        Task<List<UserNotifications>> GetLastNotifications(string userId);
        Task<List<UserNotifications>> GetNotifications(string userId);
    }
}
