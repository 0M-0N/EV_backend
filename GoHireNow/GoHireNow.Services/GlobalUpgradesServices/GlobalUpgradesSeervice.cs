using GoHireNow.Database;
using GoHireNow.Models.GlobalUpgradeModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.HireModels;
using GoHireNow.Models.JobsModels;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Service.HireServices
{
    public class GlobalUpgradesService : IGlobalUpgradesService
    {
        private readonly IUserRoleService _userRoleService;
        public GlobalUpgradesService(IUserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }
        public async Task<List<GlobalUpgradeDetailResponse>> GetGlobalUpgrades()
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {

                    var list = new List<GlobalUpgradeDetailResponse>();
                    var upgrades = await _context.GlobalUpgrades.Where(x => x.isActive == true ).OrderByDescending(x=> x.ID).ToListAsync();

                    foreach (var item in upgrades)
                    {
                        var res = new GlobalUpgradeDetailResponse();
                        res.ID = item.ID;
                        res.Name = item.Name;
                        res.Price = item.Price;
                        res.ProductId = item.ProductId;
                        res.isActive = item.isActive;
                        list.Add(res);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}