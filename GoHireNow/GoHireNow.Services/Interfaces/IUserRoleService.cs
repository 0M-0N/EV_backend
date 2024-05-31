using GoHireNow.Database.ComplexTypes;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.WorkerModels;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IUserRoleService
    {
        Task<bool> GetClientPlan(string userId);
        Task<bool> ChatExist(string userId, string clientId);
        Task<bool> TextFilterCondition(string userId, int roleId, string clientOrWorkerId);
        Task<string> GetUserId(int userUniqueId);

    }
}
