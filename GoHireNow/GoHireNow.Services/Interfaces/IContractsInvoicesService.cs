using GoHireNow.Models.ContractModels;
using System.Threading.Tasks;
using GoHireNow.Database;
using System.Collections.Generic;
using System;

namespace GoHireNow.Service.Interfaces
{
    public interface IContractsInvoicesService
    {
        Task<ContractsInvoicesDetailResponse> GetInvoiceDetail(string userId, int invoiceId);
        Task<List<ContractsInvoicesDetailResponse>> GetContractsInvoicesDetails(int ContractId);
    }
}
