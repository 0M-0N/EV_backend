using GoHireNow.Models.GlobalUpgradeModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IGlobalUpgradesService
    {
        Task<List<GlobalUpgradeDetailResponse>> GetGlobalUpgrades();
    }
}
