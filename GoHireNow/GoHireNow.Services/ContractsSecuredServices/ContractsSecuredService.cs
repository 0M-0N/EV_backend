using GoHireNow.Database;
using GoHireNow.Models.ContractModels;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Service.Interfaces;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace GoHireNow.Service.ContractsSecuredServices
{
    public class ContractsSecuredService : IContractsSecuredService
    {
        public async Task<List<ContractsSecuredDetailResponse>> GetContractsSecuredDetails(int ContractId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var secured = await _context.ContractsSecured.Where(x => x.ContractId == ContractId && x.IsDeleted == 0).OrderByDescending(x => x.Id).ToListAsync();
                    var list = new List<ContractsSecuredDetailResponse>();

                    foreach (var item in secured)
                    {
                        DateTime endDate = item.PeriodDate.AddDays(6);
                        var res = new ContractsSecuredDetailResponse();
                        if (item.Type == 2)
                        {
                            var invoice = _context.ContractsInvoices.Where(x => x.SecuredId == item.Id && x.IsDeleted == 0).FirstOrDefault();
                            if (invoice != null)
                            {
                                res.InvoiceId = invoice.Id;
                            }
                        }
                        res.Id = item.Id;
                        res.ContractId = item.ContractId;
                        res.CreatedDate = item.CreatedDate;
                        res.Method = item.Method;
                        res.Amount = item.Amount;
                        res.Type = item.Type;
                        res.PeriodDate = item.PeriodDate;
                        res.endDate = endDate;
                        res.IsDeleted = item.IsDeleted;
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

        public bool PostContractsSecured(ContractsSecured model)
        {
            using (var _context = new GoHireNowContext())
            {
                _context.ContractsSecured.Add(model);
                _context.SaveChanges();
                return true;
            }
        }

    }
}
