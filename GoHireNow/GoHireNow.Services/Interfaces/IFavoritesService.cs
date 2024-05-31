using GoHireNow.Models.ClientModels;
using GoHireNow.Models.WorkerModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IFavoritesService
    {
        Task<int> AddClientFavotite(string clientId, string workerId);
        Task<bool> RemoveClientFavotite(string clientId, string workerId);
        Task<List<ClientFavoritesResponse>> GetFavoriteWorkers(string userId,int roleId);
        Task<List<WorkerSummaryForClientResponse>> GetFavoriteWorkersNew(string userId,int roleId, int page = 1, int size = 5);
        Task<bool> IsWorkerInMyFavorite(string clientId, string workerId);

        //Worker methods
        Task<int> AddFavoriteJob(string userId, int jobId);
        Task<bool> RemoveFavoriteJob(string userId, int jobId);
        Task<List<JobSummaryForWorkerResponse>> GetFavoriteJobs(string userId, int roleId);
    }
}
