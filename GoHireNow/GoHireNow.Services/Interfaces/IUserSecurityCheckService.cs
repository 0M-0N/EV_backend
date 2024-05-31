using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IUserSecurityCheckService
    {
        Task<List<UserSecurityCheckResponse>> GetAllUserSecurityCheck(string userId);
        bool PostUserSecurityCheck(UserSecurityCheck model);
        bool UserChecked(string userId, int workerId);
    }
}
