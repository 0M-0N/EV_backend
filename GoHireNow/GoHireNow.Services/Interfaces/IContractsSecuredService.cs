using GoHireNow.Models.ContractModels;
using System.Threading.Tasks;
using GoHireNow.Database;
using System.Collections.Generic;
using System;

namespace GoHireNow.Service.Interfaces
{
    public interface IContractsSecuredService
    {
        Task<List<ContractsSecuredDetailResponse>> GetContractsSecuredDetails(int ContractId);
        bool PostContractsSecured(ContractsSecured model);
    }
}
