using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Models.ClientModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GoHireNow.Service.UserSecurityCheckServices
{
    public class UserSecurityCheckService : IUserSecurityCheckService
    {
        public bool PostUserSecurityCheck(UserSecurityCheck model)
        {
            using (var _context = new GoHireNowContext())
            {
                _context.UserSecurityCheck.Add(model);
                _context.SaveChanges();
                return true;
            }
        }

        public async Task<List<UserSecurityCheckResponse>> GetAllUserSecurityCheck(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                return await _context.UserSecurityCheck
                    .Where(x => x.CompanyId == userId )
                    .Select(x => new UserSecurityCheckResponse
                    {
                        Id = x.Id,
                        UserId = x.UserId,
                        CompanyId = x.CompanyId,
                        CreateDate = x.CreatedDate,
                        IsDeleted = x.isDeleted
                    }).ToListAsync();
            }
        }

        public bool UserChecked(string userId, int workerId)
        {
            using (var _context = new GoHireNowContext())
            {
                var transactions = _context.Transactions
                    .Where(x => x.UserId == userId && x.CustomId == workerId && x.CustomType == 2 && x.IsDeleted == false && x.Status == "paid")
                    .ToList();

                if (transactions.Count() > 0)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
