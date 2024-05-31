using GoHireNow.Database;
using GoHireNow.Models.ContractModels;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Service.Interfaces;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace GoHireNow.Service.ContractsInvoicesServices
{
    public class ContractsInvoicesService : IContractsInvoicesService
    {
        public async Task<ContractsInvoicesDetailResponse> GetInvoiceDetail(string userId, int invoiceId)
        {
            using (var _context = new GoHireNowContext())
            {
                var invoice = await _context.ContractsInvoices.Include(ci => ci.Contract).ThenInclude(c => c.User).FirstOrDefaultAsync(ci => (ci.Contract.CompanyId == userId || ci.Contract.UserId == userId) && ci.Id == invoiceId && ci.IsDeleted == 0);
                if (invoice == null)
                {
                    return null;
                }
                var company = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == invoice.Contract.CompanyId && !u.IsDeleted);
                return new ContractsInvoicesDetailResponse()
                {
                    Id = invoice.Id,
                    ContractId = invoice.ContractId,
                    CreatedDate = invoice.CreatedDate,
                    Hours = invoice.Hours,
                    Amount = invoice.Amount,
                    StatusId = invoice.StatusId,
                    PayoutCommission = invoice.PayoutCommission,
                    PaidDate = invoice.PaidDate,
                    PayoutStatusId = invoice.PayoutStatusId,
                    InvoiceType = invoice.InvoiceType,
                    PayoutId = invoice.PayoutId,
                    IsDeleted = invoice.IsDeleted,
                    Comment = invoice.Comment,
                    SecuredId = invoice.SecuredId,
                    ContractRate = invoice.Contract.Rate,
                    CompanyName = company != null ? company.Company : "",
                    CompanyEmail = company != null ? company.Email : "",
                    WorkerName = invoice.Contract.User.FullName,
                    ContractName = invoice.Contract.Name
                };
            }
        }

        public async Task<List<ContractsInvoicesDetailResponse>> GetContractsInvoicesDetails(int ContractId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var list = new List<ContractsInvoicesDetailResponse>();
                    var invoices = await _context.ContractsInvoices.Where(x => x.ContractId == ContractId && x.IsDeleted == 0 && x.Amount > 0 && x.StatusId > 0).OrderByDescending(x => x.Id).ToListAsync();

                    foreach (var item in invoices)
                    {
                        var res = new ContractsInvoicesDetailResponse();
                        res.Id = item.Id;
                        res.ContractId = item.ContractId;
                        res.CreatedDate = item.CreatedDate;
                        res.Hours = item.Hours;
                        res.Amount = item.Amount;
                        res.StatusId = item.StatusId;
                        res.PayoutCommission = item.PayoutCommission;
                        res.PayoutDate = item.PayoutDate;
                        res.PayoutStatusId = item.PayoutStatusId;
                        res.InvoiceType = item.InvoiceType;
                        res.PayoutId = item.PayoutId;
                        res.IsDeleted = item.IsDeleted;
                        res.Comment = item.Comment;
                        res.SecuredId = item.SecuredId;
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
