using GoHireNow.Models.ClientModels;
using System.Threading.Tasks;

namespace GoHireNow.Api.Handlers.ClientHandlers.Interfaces
{
    public interface IClientJobHandler
    {
        Task<int> PostJob(string userId, PostJobRequest model);
    }
}
