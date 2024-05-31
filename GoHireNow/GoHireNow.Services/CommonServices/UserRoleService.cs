using GoHireNow.Database;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Service.CommonServices
{
    public class UserRoleService : IUserRoleService
    {
        public async Task<bool> GetClientPlan(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var planId = await _context.AspNetUsers.Where(o => o.Id == userId).Select(o => o.GlobalPlanId).FirstOrDefaultAsync();
                return planId > 0 ? false : true;

            }

        }

        public async Task<bool> ChatExist(string userId, string clientId)
        {
            bool chatExist = false;
            using (var _context = new GoHireNowContext())
            {
                var mail = await _context.Mails.FirstOrDefaultAsync(x => x.UserIdFrom == clientId && x.UserIdTo == userId && x.IsDeleted == false);
                chatExist = mail != null ? true : false;
            }
            return chatExist;
        }

        public async Task<bool> TextFilterCondition(string userId, int roleId, string clientOrWorkerId)
        {
            bool applyFilter = false;
            if (userId == clientOrWorkerId)
            {
                return applyFilter;
            }
            if (roleId == (int)UserTypeEnum.Client)
            {
                applyFilter = await GetClientPlan(userId) ? false : true;
            }
            if(roleId==(int)UserTypeEnum.Worker)
            {
                applyFilter = await ChatExist(userId, clientOrWorkerId.ToString()) ? false : true;
            }
            return applyFilter;
        }

        public async Task<string> GetUserId(int userUniqueId)
        {
            using (var _context = new GoHireNowContext())
            {
                var user = await _context.spGetUserIdResult.FromSql("spGetUserId @UserUniqueId = {0}", userUniqueId).FirstOrDefaultAsync();

                return user?.UserId;
            }
        }
    }
}
