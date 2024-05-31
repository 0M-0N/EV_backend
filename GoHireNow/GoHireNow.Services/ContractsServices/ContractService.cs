using GoHireNow.Database;
using GoHireNow.Models.ContractModels;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Service.Interfaces;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using GoHireNow.Models.PayoutTransactionModels;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using GoHireNow.Models.CommonModels;
using GoHireNow.Database.GoHireNowTools;
using GoHireNow.Database.GoHireNowTools.Models;
using System.Data.SqlClient;
using System.Net.Http;

namespace GoHireNow.Service.ContractService
{
    public class ContractService : IContractService
    {
        private readonly ICustomLogService _customLogService;
        private readonly IPricingService _pricingService;
        private IConfiguration _configuration { get; }
        public ContractService(IConfiguration configuration, IPricingService pricingService, ICustomLogService customLogService)
        {
            _configuration = configuration;
            _pricingService = pricingService;
            _customLogService = customLogService;
        }

        public async Task<CreateContractResponse> CreateContract(string userId, CreateModel model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var planDetail = await _pricingService.GetSubscriptionDetails(userId);
                    var maximumLimit = 10;
                    if (planDetail?.SubscriptionStatus?.Id == 3 || planDetail?.SubscriptionStatus?.Id == 21)
                    {
                        maximumLimit = 35;
                    }
                    else
                    {
                        if (planDetail?.SubscriptionStatus?.Id == 5 || planDetail?.SubscriptionStatus?.Id == 22)
                        {
                            maximumLimit = 70;
                        }
                    }

                    var contractsToday = await _context.Contracts.Where(x => x.CompanyId == userId && x.IsDeleted == 0 && x.CreatedDate >= DateTime.UtcNow.Date).ToListAsync();
                    if (contractsToday != null && contractsToday.Count() >= maximumLimit)
                    {
                        return new CreateContractResponse
                        {
                            Status = false,
                            Id = -2,
                        };
                    }

                    Contracts contract = MapContract(userId, model);
                    CreateContractResponse res;
                    res = new CreateContractResponse
                    {
                        Status = true,
                        Id = await AddContract(contract)
                    };

                    if (res.Id > 0)
                    {
                        var worker = await _context.AspNetUsers.Where(x => x.Id == model.ToUserId && x.IsDeleted == false).FirstOrDefaultAsync();
                        var company = await _context.AspNetUsers.Where(x => x.Id == userId && x.IsDeleted == false).FirstOrDefaultAsync();
                        if (worker != null)
                        {
                            string subject = "You Received A New Job Offer - " + contract.Name;
                            string headtitle = "New Job Offer";
                            string message = contract.Name;
                            string description = company.Company + " just sent you a job offer on our secure platform.";
                            string howtotext = "HOW TO: Accept or Reject A Job Offer";
                            string howtourl = "https://www.youtube.com/watch?v=ZSfiRhMNmGQ";
                            string buttonurl = _configuration.GetValue<string>("WebDomain") + "/my-jobs";
                            string buttoncaption = "View My Job Offer";
                            await NewMailService(0, 19, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "HowToEmailTemplate.html", howtotext, howtourl);
                        }
                    }

                    return res;
                }
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public async Task<bool> CheckContracted(string userId, string toUserId)
        {
            using (var _context = new GoHireNowContext())
            {
                var contracts = await _context.Contracts.Where(x => x.IsDeleted == 0 && x.isAccepted == 1 && x.AutomaticBilling == 1 && ((x.CompanyId == userId && x.UserId == toUserId) || (x.UserId == userId && x.CompanyId == toUserId))).ToListAsync();

                if (contracts != null && contracts.Count() > 0)
                {
                    return true;
                }
                else
                {
                    var user = await _context.AspNetUsers.Where(x => x.Id == toUserId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (user.UserType == 1)
                    {
                        var toUserPlan = await _pricingService.GetCurrentPlan(toUserId);
                        if (toUserPlan.Name.Contains("Enterprise") || toUserPlan.Name.Contains("Agency"))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        public async Task<bool> EndContract(string userId, int contractid, string ip)
        {
            using (var _context = new GoHireNowContext())
            {
                var contract = await _context.Contracts.Where(x => x.Id == contractid && x.IsDeleted == 0 && x.CompanyId == userId).FirstOrDefaultAsync();
                if (contract == null)
                {
                    return false;
                }

                var param = new SqlParameter("@ContractId", contractid);
                await _context.Database.ExecuteSqlCommandAsync("EXEC [dbo].[sp_ContractEnded] @ContractId", param);

                var agreement = new UserAgreements()
                {
                    UserId = userId,
                    Ip = ip,
                    Type = 1,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.UserAgreements.AddAsync(agreement);
                await _context.SaveChangesAsync();

                var worker = await _context.AspNetUsers.Where(x => x.Id == contract.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                var company = await _context.AspNetUsers.Where(x => x.Id == contract.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();
                if (worker != null)
                {
                    string subject = "Your Job Has End - " + contract.Name;
                    string headtitle = "Job Ended";
                    string message = contract.Name;
                    string description = company.Company + " ended your job. Any outstanding balances have been paid.";
                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/billing-detail/" + contractid;
                    string buttoncaption = "View Job";
                    await NewMailService(0, 20, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                }

                var invoices = await _context.ContractsInvoices.Where(ci => ci.ContractId == contractid && ci.Amount > 0 && ci.IsDeleted == 0).ToListAsync();

                if (invoices != null && invoices.Count() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<List<EndContractResponse>> WeeklyEndContract()
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var endContractResult = await _context.sp_ContractsEndsWeekly.FromSql("sp_ContractsEndsWeekly").ToListAsync();
                    var list = new List<EndContractResponse>();

                    foreach (var item in endContractResult)
                    {
                        var rs = new EndContractResponse();
                        rs.stripeChargeId = item.StripeChargeId;
                        rs.contractId = item.contractId;
                        rs.amountDue = item.amountDue;
                        list.Add(rs);
                    }

                    return list;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private Contracts MapContract(string userid, CreateModel model)
        {
            var contract = new Contracts();
            try
            {
                contract.Name = model.ContractName;
                contract.CompanyId = userid;
                contract.UserId = model.ToUserId;
                contract.Hours = model.Hours;
                contract.Rate = model.Rate;
                contract.isAccepted = 0;
                contract.CreatedDate = DateTime.UtcNow;
                contract.UpdatedDate = DateTime.UtcNow;
                contract.IsDeleted = 0;
                contract.Method = 1;
                contract.AutomaticBilling = 0;
                contract.AutomaticDeposit = 0;
                contract.AutomaticRelease = 0;
            }
            catch (Exception)
            {
                throw new CustomException(500, "Error mapping contracts request with required model");
            }

            return contract;
        }

        public async Task<int> AddContract(Contracts contract)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    await _context.Contracts.AddAsync(contract);
                    await _context.SaveChangesAsync();
                    return contract.Id;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> UpdateContractStatus(string userId, UpdateStatusModel model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var keyid = _context.Contracts.Where(x => model.ContractId == x.Id && (userId.Contains(x.UserId) || userId.Contains(x.CompanyId))).Select(j => j.Id).FirstOrDefault();

                    if (keyid > 0)
                    {
                        var contract = await _context.Contracts.FindAsync(keyid);
                        if (contract != null)
                        {
                            contract.isAccepted = model.Status;
                            await _context.SaveChangesAsync();

                            if (model.Status == 1)
                            {
                                var company = await _context.AspNetUsers.Where(x => x.Id == contract.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();
                                var worker = await _context.AspNetUsers.Where(x => x.Id == contract.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                                if (company != null)
                                {
                                    string subject = "Your Contract Offer Was Accepted - " + contract.Name;
                                    string headtitle = "Contract Accepted";
                                    string message = contract.Name;
                                    string description = worker.FullName + " just accepted your contract on our secure platform.";
                                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/user-payment-detail/" + contract.Id;
                                    string buttoncaption = "View Contract";
                                    await NewMailService(0, 21, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");

                                    subject = "Contract Security Deposit Required - " + contract.Name;
                                    headtitle = "Security Deposit Required";
                                    message = contract.Name;
                                    description = "You need to activate the security deposit to start this contract.";
                                    await NewMailService(0, 22, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                                }
                            }
                            else
                            {
                                if (model.Status == 3)
                                {
                                    var company = await _context.AspNetUsers.Where(x => x.Id == contract.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();
                                    var worker = await _context.AspNetUsers.Where(x => x.Id == contract.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                                    if (company != null)
                                    {
                                        string subject = "Your Contract Offer Was Rejected - " + contract.Name;
                                        string headtitle = "Contract Rejected";
                                        string message = contract.Name;
                                        string description = worker.FullName + " just rejected your contract on our secure platform.";
                                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/user-payment-detail/" + contract.Id;
                                        string buttoncaption = "View Contract";
                                        await NewMailService(0, 23, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                                    }
                                }
                                else
                                {
                                    if (model.Status == 2)
                                    {
                                        var company = await _context.AspNetUsers.Where(x => x.Id == contract.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();
                                        var worker = await _context.AspNetUsers.Where(x => x.Id == contract.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                                        if (company != null)
                                        {
                                            string subject = "Your Job Offer Was Canceled - " + contract.Name;
                                            string headtitle = "Job Offer Canceled";
                                            string message = contract.Name;
                                            string description = company.Company + " canceled your job offer on our secure platform.";
                                            string buttonurl = _configuration.GetValue<string>("WebDomain") + "/billing-detail/" + contract.Id;
                                            string buttoncaption = "View Job Offer";
                                            await NewMailService(0, 24, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                                        }
                                    }
                                }
                            }

                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else { return false; }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<PayoutRecipients> GetPayoutRecipient(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var recipient = await _context.PayoutRecipients.Where(x => x.userId == userId && x.isdeleted == 0 && x.statusId > 0).FirstOrDefaultAsync();
                return recipient;
            }
        }

        public async Task<bool> isautomaticbilling(string userId, UpdateAutomaticBillingModel model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {

                    var keyid = _context.Contracts.Where(x => model.ContractId == x.Id && userId.Contains(x.CompanyId) && x.IsDeleted == 0).Select(j => j.Id).FirstOrDefault();

                    if (keyid > 0)
                    {
                        var contract = await _context.Contracts.FindAsync(keyid);
                        if (contract != null)
                        {
                            contract.AutomaticBilling = model.AutomaticBilling;
                            await _context.SaveChangesAsync();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else { return false; }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddHour(string userId, AddHourModel model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var contract = await _context.Contracts.Where(x => x.Id == model.contractId && x.IsDeleted == 0 && x.UserId == userId).FirstOrDefaultAsync();
                    var disputes = await _context.ContractsDisputes.Where(x => x.ContractId == model.contractId && x.Status == 0 && x.IsCompleted == 0 && x.IsDeleted == 0).ToListAsync();
                    if (contract == null || (disputes != null && disputes.Count > 0))
                    {
                        return false;
                    }

                    DateTime dateTime;
                    DateTime.TryParse(model.workDate, out dateTime);
                    var newWorkingHour = new ContractsHours
                    {
                        Hours = model.workHour,
                        WorkedDate = dateTime,
                        ContractId = model.contractId,
                        Description = model.description,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _context.ContractsHours.AddAsync(newWorkingHour);
                    await _context.SaveChangesAsync();

                    var worker = await _context.AspNetUsers.Where(x => x.Id == contract.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                    var company = await _context.AspNetUsers.Where(x => x.Id == contract.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (company != null)
                    {
                        string subject = "Worked Hours Added To Weekly Contract - " + contract.Name;
                        string headtitle = "New Worked Hours";
                        string message = contract.Name;
                        string description = worker.FullName + " added " + model.workHour.ToString("F2") + " worked hours for " + model.workDate + "<br/>Worked on: " + model.description;
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/user-payment-detail/" + model.contractId;
                        string buttoncaption = "View Contract";
                        await NewMailService(0, 25, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }

                    return true;
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<List<ContractsHours>> GetWorkingHours(int contractId, int week)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    DateTime today = DateTime.Today;
                    int daysUntilMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
                    DateTime startOfWeek = today.AddDays(-daysUntilMonday + week * 7);
                    DateTime endOfWeek = startOfWeek.AddDays(7);

                    return await _context.ContractsHours.Where(x => x.ContractId == contractId && x.IsDeleted == 0 && x.WorkedDate >= startOfWeek && x.WorkedDate < endOfWeek).OrderBy(x => x.WorkedDate).ToListAsync();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> DeleteHour(int id, string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var hour = await _context.ContractsHours.FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted == 0);
                    if (hour != null)
                    {
                        var contract = await _context.Contracts.Where(x => x.Id == hour.ContractId && x.IsDeleted == 0 && x.UserId == userId).FirstOrDefaultAsync();
                        if (contract == null)
                        {
                            return false;
                        }

                        hour.IsDeleted = 1;
                        await _context.SaveChangesAsync();

                        var worker = await _context.AspNetUsers.Where(x => x.Id == contract.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                        var company = await _context.AspNetUsers.Where(x => x.Id == contract.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();
                        if (company != null)
                        {
                            string subject = "Hours have been removed from the contract - " + contract.Name;
                            string headtitle = "";
                            string message = contract.Name;
                            string description = worker.FullName + " removed " + hour.Hours.ToString("F2") + " worked hours for " + hour.WorkedDate?.ToString("MMM d, yyyy") + "<br/>Worked on: " + hour.Description;
                            string buttonurl = _configuration.GetValue<string>("WebDomain") + "/user-payment-detail/" + hour.ContractId;
                            string buttoncaption = "View Contract";
                            await NewMailService(0, 26, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        public async Task<bool> isautomaticdeposit(string userId, UpdateAutomaticDepositModel model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {

                    var keyid = _context.PayoutRecipients.Where(x => userId == x.userId && x.isdeleted == 0).Select(j => j.id).FirstOrDefault();

                    if (keyid > 0)
                    {
                        var recipient = await _context.PayoutRecipients.FindAsync(keyid);
                        if (recipient != null)
                        {
                            recipient.autodeposit = model.AutomaticDeposit;
                            await _context.SaveChangesAsync();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else { return false; }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> isautomaticrelease(string userId, UpdateAutomaticReleaseModel model, string ip)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {

                    var keyid = _context.Contracts.Where(x => userId == x.CompanyId && x.Id == model.ContractId && x.IsDeleted == 0).Select(j => j.Id).FirstOrDefault();

                    if (keyid > 0)
                    {
                        var contract = await _context.Contracts.FindAsync(keyid);
                        if (contract != null)
                        {
                            contract.AutomaticRelease = model.AutomaticRelease;

                            if (model.AutomaticRelease == 1)
                            {
                                var agreement = new UserAgreements()
                                {
                                    UserId = userId,
                                    Ip = ip,
                                    Type = 3,
                                    CreatedDate = DateTime.UtcNow
                                };

                                await _context.UserAgreements.AddAsync(agreement);
                            }

                            await _context.SaveChangesAsync();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else { return false; }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> Dispute(DisputeModel model, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == model.ContractId && c.IsDeleted == 0 && c.CompanyId == userId);
                if (contract == null)
                {
                    return false;
                }

                var newDispute = new ContractsDisputes
                {
                    CompanyId = userId,
                    UserId = model.UserId,
                    Description = model.Description,
                    ContractId = model.ContractId,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.ContractsDisputes.AddAsync(newDispute);

                var invoice = await _context.ContractsInvoices.Where(x => x.IsDeleted == 0 && x.ContractId == model.ContractId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                if (invoice != null)
                {
                    invoice.PayoutStatusId = 3;
                }

                await _context.SaveChangesAsync();

                var request = new LogSupportRequest()
                {
                    Text = "A dispute has been filed ID: " + newDispute.Id,
                };
                _customLogService.LogSupport(request);

                return true;
            }
        }

        public async Task<bool> Archive(string userId, int contractId)
        {
            using (var _context = new GoHireNowContext())
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && c.IsDeleted == 0 && (c.CompanyId == userId || c.UserId == userId) && c.isAccepted != 1);
                if (contract == null)
                {
                    return false;
                }

                contract.IsArchived = 1;
                await _context.SaveChangesAsync();

                return true;
            }
        }

        public async Task<bool> SendReport(string userId, ReportModel model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    int id;
                    if (model.customTypeId != 2)
                    {
                        var profile = _context.AspNetUsers.Where(u => u.Id == model.customId).FirstOrDefault();
                        id = profile.UserUniqueId;
                    }
                    else
                    {
                        id = int.Parse(model.customId);
                    }

                    var newReport = new UserReports
                    {
                        UserId = userId,
                        CustomTypeId = model.customTypeId,
                        CustomId = id,
                        Reason = model.reason,
                        IsDeleted = 0,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _context.UserReports.AddAsync(newReport);
                    await _context.SaveChangesAsync();

                    var type = "";

                    switch (model.customTypeId)
                    {
                        case 1: type = "Message"; break;
                        case 2: type = "Job"; break;
                        case 3: type = "Company Profile"; break;
                        case 4: type = "Worker Profile"; break;
                        default: break;
                    }

                    var request = new LogSupportRequest()
                    {
                        Text = "From: " + userId + " To: " + id + " Reason: " + model.reason + " Type: " + type
                    };
                    _customLogService.LogSupport(request);

                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateHours(string userId, UpdateHoursModel model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {

                    var keyid = _context.Contracts.Where(x => model.ContractId == x.Id && userId.Contains(x.CompanyId)).Select(j => j.Id).FirstOrDefault();

                    if (keyid > 0)
                    {
                        var contract = await _context.Contracts.FindAsync(keyid);
                        if (contract != null)
                        {
                            contract.Hours = model.hours;
                            await _context.SaveChangesAsync();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else { return false; }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteContract(string userId, int contractid)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {

                    var keyid = _context.Contracts.Where(x => contractid == x.Id && (userId.Contains(x.UserId) || userId.Contains(x.CompanyId))).Select(j => j.Id).FirstOrDefault();

                    if (keyid > 0)
                    {
                        var contract = await _context.Contracts.FindAsync(keyid);
                        if (contract != null)
                        {
                            contract.IsDeleted = 1;
                            await _context.SaveChangesAsync();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else { return false; }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<ContractMoreDetailResponse> GetContractSecured(string userId, int ContractId)
        {
            using (var _context = new GoHireNowContext())
            {
                var cs = _context.spGetContractSecured.FromSql("spGetContractSecured @ContractID = {0}", ContractId).FirstOrDefault();
                if (cs != null)
                {
                    return new ContractMoreDetailResponse
                    {
                        securedAmount = cs.amount,
                    };
                }
                else
                {
                    return new ContractMoreDetailResponse
                    {
                        securedAmount = 0,
                    };
                }
            }
        }

        public async Task<ContractMoreDetailResponse> GetContractUnbilled(string userId, int ContractId)
        {
            using (var _context = new GoHireNowContext())
            {
                var uh = _context.spGetContractUnbilled.FromSql("spGetContractUnbilled @ContractID = {0}", ContractId).FirstOrDefault();
                if (uh != null)
                {
                    return new ContractMoreDetailResponse
                    {
                        unbilled = uh.unbilledhours,
                    };
                }
                else
                {
                    return new ContractMoreDetailResponse
                    {
                        unbilled = 0,
                    };
                }
            }
        }

        public async Task<ContractMoreDetailResponse> GetContractLastPayment(string userId, int ContractId)
        {
            using (var _context = new GoHireNowContext())
            {
                var lp = _context.spGetContractLastPayment.FromSql("spGetContractLastPayment @ContractID = {0}", ContractId).FirstOrDefault();
                if (lp != null)
                {
                    return new ContractMoreDetailResponse
                    {
                        lastPayment = lp.lastpayment,
                    };
                }
                else
                {
                    return new ContractMoreDetailResponse
                    {
                        lastPayment = 0,
                    };
                }
            }
        }

        public async Task<List<ContractReleaseInvoiceResponse>> GetReleaseInvoices(int ContractId, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == ContractId && c.IsDeleted == 0 && c.CompanyId == userId);
                if (contract == null)
                {
                    return null;
                }

                var invoices = await _context.ContractsInvoices.Where(x => x.ContractId == ContractId && x.StatusId == 1 && x.PayoutStatusId == 0 && x.IsDeleted == 0).ToListAsync();
                var result = invoices.Select(iv => new ContractReleaseInvoiceResponse()
                {
                    Id = iv.Id,
                    ContractId = iv.ContractId,
                    CreatedDate = iv.CreatedDate,
                    PayoutDate = iv.PayoutDate,
                    Hours = iv.Hours,
                    Amount = iv.Amount,
                    PayoutCommission = iv.PayoutCommission,
                    PayoutStatusId = iv.PayoutStatusId,
                    StatusId = iv.StatusId,
                    InvoiceType = iv.InvoiceType,
                    PayoutId = iv.PayoutId,
                    IsDeleted = iv.IsDeleted,
                    SecuredId = iv.SecuredId,
                    Comment = iv.Comment,
                }).ToList();
                return result;
            }
        }

        public async Task<bool> Release(int ContractId, string ip, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == ContractId && c.IsDeleted == 0 && c.CompanyId == userId);
                if (contract == null)
                {
                    return false;
                }

                var keyids = await _context.ContractsInvoices.Where(x => x.ContractId == ContractId && x.StatusId == 1 && x.PayoutStatusId == 0 && x.IsDeleted == 0).Select(j => j.Id).ToListAsync();

                foreach (var id in keyids)
                {
                    var invoice = await _context.ContractsInvoices.FindAsync(id);
                    if (invoice != null)
                    {
                        invoice.PayoutStatusId = 1;
                        await _context.SaveChangesAsync();
                    }
                }

                var agreement = new UserAgreements()
                {
                    UserId = userId,
                    Ip = ip,
                    Type = 2,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.UserAgreements.AddAsync(agreement);
                await _context.SaveChangesAsync();

                return true;
            }
        }

        public async Task<ContractMoreDetailResponse> GetAccountBalance(string userId, int ContractId)
        {
            using (var _context = new GoHireNowContext())
            {
                var cb = _context.spGetAccountBalanced.FromSql("spGetAccountBalanced @ContractID = {0}", ContractId).FirstOrDefault();
                if (cb != null)
                {
                    return new ContractMoreDetailResponse
                    {
                        contractBalance = cb.amount
                    };
                }
                else
                {
                    return new ContractMoreDetailResponse
                    {
                        contractBalance = 0
                    };
                }
            }
        }

        public async Task<ContractMoreDetailResponse> GetContractBalance(string userId, int ContractId)
        {
            using (var _context = new GoHireNowContext())
            {
                var cb = _context.spGetContractBalanced.FromSql("spGetContractBalanced @ContractID = {0}", ContractId).FirstOrDefault();
                if (cb != null)
                {
                    return new ContractMoreDetailResponse
                    {
                        contractBalance = cb.contractbalance
                    };
                }
                else
                {
                    return new ContractMoreDetailResponse
                    {
                        contractBalance = 0
                    };
                }
            }
        }

        public async Task<ContractMoreDetailResponse> GetContractStatus(string userId, int ContractId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var securityStatus = await _context.spGetIsSecured.FromSql("spGetIsSecured @ContractID = {0}", ContractId).FirstOrDefaultAsync();
                    if (securityStatus != null)
                    {
                        return new ContractMoreDetailResponse
                        {
                            securityStatusId = securityStatus.r,
                            securityAmount = securityStatus.amount
                        };
                    }
                    else
                    {
                        return new ContractMoreDetailResponse
                        {
                            securityStatusId = 0,
                            securityAmount = 0
                        };
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        public async Task<ContractMoreDetailResponse> GetContractCommission(string userId, int ContractId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var commission = await _context.spGetContractCommission.FromSql("spGetContractCommission @ContractID = {0}", ContractId).FirstOrDefaultAsync();

                    return new ContractMoreDetailResponse
                    {
                        current = commission.curr,
                        next = commission.next,
                        amount = commission.amount
                    };
                }
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        public async Task<ContractMoreDetailResponse> GetContractTotalRevenue(string userId, int ContractId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var totalRevenue = await _context.spGetContractBilledTotal.FromSql("spGetContractBilledTotal @ContractID = {0}", ContractId).FirstOrDefaultAsync();
                    if (totalRevenue != null)
                    {
                        return new ContractMoreDetailResponse
                        {
                            totalRevenue = totalRevenue.billedTotal
                        };
                    }
                    else
                    {
                        return new ContractMoreDetailResponse
                        {
                            totalRevenue = 0
                        };

                    }

                }
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        public async Task<ContractDetailResponse> GetContractDetails(string userId, int ContractId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var contract = await _context.Contracts.Include(o => o.User).Where(x => ContractId == x.Id && ((userId.Contains(x.UserId) && x.IsDeleted != 1) || (userId.Contains(x.CompanyId)) && x.IsDeleted != 1)).FirstOrDefaultAsync();

                    if (contract != null)
                    {
                        var disputes = await _context.ContractsDisputes.Where(d => d.ContractId == contract.Id && d.Status == 0 && d.IsCompleted == 0 && d.IsDeleted == 0).ToListAsync();

                        var res = new ContractDetailResponse();
                        res.Id = contract.Id;
                        res.Name = contract.Name;
                        res.WorkerName = contract.User.FullName;
                        res.CompanyId = contract.CompanyId;
                        res.UserId = contract.UserId;
                        res.Hours = contract.Hours;
                        res.Rate = contract.Rate;
                        res.CreatedDate = contract.CreatedDate;
                        res.UpdatedDate = contract.UpdatedDate;
                        res.isAccepted = contract.isAccepted;
                        res.IsDeleted = contract.IsDeleted;
                        res.AutomaticBilling = contract.AutomaticBilling;
                        res.Method = contract.Method;
                        res.AutomaticDeposit = contract.AutomaticDeposit;
                        res.AutomaticRelease = contract.AutomaticRelease;
                        res.IsArchived = contract.IsArchived;
                        res.IsDisputed = disputes.Count() > 0 ? true : false;
                        return res;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<ContractDetailResponse>> ListContractsCompany(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var list = new List<ContractDetailResponse>();
                    var contracts = await _context.Contracts.Include(o => o.User).Where(x => x.IsDeleted != 1 && userId.Contains(x.CompanyId)).OrderByDescending(x => x.Id).ToListAsync();

                    foreach (var item in contracts)
                    {
                        var securityStatus = await _context.spGetIsSecured.FromSql("spGetIsSecured @ContractID = {0}", item.Id).FirstOrDefaultAsync();
                        var res = new ContractDetailResponse();
                        res.Id = item.Id;
                        res.Name = item.Name;
                        res.CompanyId = item.CompanyId;
                        res.UserId = item.UserId;
                        res.Hours = item.Hours;
                        res.Rate = item.Rate;
                        res.CreatedDate = item.CreatedDate;
                        res.UpdatedDate = item.UpdatedDate;
                        res.isAccepted = item.isAccepted;
                        res.IsDeleted = item.IsDeleted;
                        res.AutomaticBilling = item.AutomaticBilling;
                        res.Method = item.Method;
                        res.AutomaticDeposit = item.AutomaticDeposit;
                        res.AutomaticRelease = item.AutomaticRelease;
                        res.IsArchived = item.IsArchived;
                        res.SecurityStatusId = securityStatus.r;
                        list.Add(res);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<List<ContractDetailResponse>> ListContractsUser(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var list = new List<ContractDetailResponse>();
                    var contract = await _context.Contracts.Where(x => x.IsDeleted != 1 && userId.Contains(x.UserId)).OrderByDescending(x => x.Id).ToListAsync();

                    foreach (var item in contract)
                    {
                        var securityStatus = await _context.spGetIsSecured.FromSql("spGetIsSecured @ContractID = {0}", item.Id).FirstOrDefaultAsync();
                        var res = new ContractDetailResponse();
                        res.Id = item.Id;
                        res.Name = item.Name;
                        res.CompanyId = item.CompanyId;
                        res.UserId = item.UserId;
                        res.Hours = item.Hours;
                        res.Rate = item.Rate;
                        res.CreatedDate = item.CreatedDate;
                        res.UpdatedDate = item.UpdatedDate;
                        res.isAccepted = item.isAccepted;
                        res.IsDeleted = item.IsDeleted;
                        res.AutomaticBilling = item.AutomaticBilling;
                        res.Method = item.Method;
                        res.AutomaticDeposit = item.AutomaticDeposit;
                        res.AutomaticRelease = item.AutomaticRelease;
                        res.IsArchived = item.IsArchived;
                        res.SecurityStatusId = securityStatus.r;
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

        public async Task<List<PayoutTransactions>> GetPayoutTransactions(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var transactions = await _context.PayoutTransactions.Where(x => x.UserId == userId && x.IsDeleted == false).OrderByDescending(x => x.Id).ToListAsync();
                    return transactions;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> ApprovePayoutTransaction(int id, string receiptId)
        {
            using (var _context = new GoHireNowContext())
            {
                var transaction = await _context.PayoutTransactions.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
                if (transaction != null)
                {
                    transaction.StatusId = 0;
                    transaction.DepositedReceipt = receiptId;
                    await _context.SaveChangesAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<int> CreatePayoutTransaction(CreatePayoutTransactionModel model, string userId)
        {
            try
            {
                var transaction = new PayoutTransactions();
                transaction.UserId = userId;
                transaction.CreatedDate = DateTime.UtcNow;
                transaction.AmountUSD = model.amountUSD;
                transaction.ExchangeRate = model.exchangeRate;
                transaction.Amount = model.amount;
                transaction.FFees = model.fee;
                transaction.Currency = model.currency;
                transaction.ArrivingBy = model.arrivingBy;
                transaction.PayoutMethod = model.payoutMethod;
                transaction.IsApproved = 0;
                transaction.IsPaid = 0;
                transaction.IsDeposited = 0;
                transaction.DepositedMethod = 0;
                transaction.IsDeleted = false;
                transaction.StatusId = 5;
                transaction.DepositedReceipt = model.transactionId;

                using (var _context = new GoHireNowContext())
                {
                    await _context.PayoutTransactions.AddAsync(transaction);
                    await _context.SaveChangesAsync();
                    return transaction.Id;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.InnerException.ToString());
                throw;
            }
        }

        public async Task<bool> DeletePayoutTransaction(int id)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var transaction = await _context.PayoutTransactions.FindAsync(id);
                    if (transaction != null)
                    {
                        transaction.IsDeleted = true;
                        transaction.StatusId = 4;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<int> GetUnapprovalCount(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var unapproval = await _context.Contracts
                        .Where(o => o.UserId == userId && o.IsDeleted == 0 && o.isAccepted == 0)
                        .CountAsync();
                    return unapproval;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public async Task<int> GetActiveContractsCount(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var activeContracts = await _context.Contracts
                        .Where(o => o.CompanyId == userId && o.IsDeleted == 0 && o.isAccepted == 1)
                        .CountAsync();
                    return activeContracts;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public async Task<int> GetUndepositCount(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var undeposited = await _context.Contracts.Where(o => o.CompanyId == userId && o.IsDeleted == 0 && o.isAccepted == 1 && o.AutomaticBilling == 0).CountAsync();
                    var release = await _context.ContractsInvoices.Include(x => x.Contract).Where(x => x.Contract.CompanyId == userId && x.StatusId == 1 && x.PayoutStatusId == 0 && x.IsDeleted == 0 && x.Hours > 0).GroupBy(x => x.ContractId).CountAsync();

                    return undeposited + release;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public async Task<List<HRPremiumContracts>> GetHRContract(string workerId)
        {
            using (var _context = new GoHireNowContext())
            {
                var contract = await _context.HRPremiumContracts.Where(c => c.WorkerId == workerId && c.Status == 1 && c.IsDeleted == 0).ToListAsync();
                return contract;
            }
        }

        public async Task<bool> AddHRContract(string companyId, string workerId)
        {
            using (var _context = new GoHireNowContext())
            {
                var company = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == companyId && !u.IsDeleted);
                var hrworker = await _context.UserHRProfile.FirstOrDefaultAsync(h => h.UserId == workerId);

                if (company != null)
                {
                    var contract = new HRPremiumContracts
                    {
                        CompanyId = companyId,
                        ClientName = company.Company,
                        ClientEmail = company.Email,
                        WorkerId = workerId,
                        Hourly = Decimal.Divide(hrworker.HRPrice, 160),
                        Status = 0,
                        ContractId = 0,
                        CreatedDate = DateTime.UtcNow,
                        LastBilledDate = DateTime.UtcNow,
                        IsDeleted = 0
                    };

                    await _context.HRPremiumContracts.AddAsync(contract);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
        }

        public async Task NewMailService(int customId, int customType, string emailTo, string nameTo, string subject, string headtitle, string buttonurl, string buttoncaption, string description, string text, string emailFrom, string nameFrom, int priority, string fileName, string howtotext = "", string howtourl = "", string img = "")
        {
            try
            {
                string htmlContent = new System.Net.WebClient().DownloadString(_configuration.GetSection("FilePaths")["EmailTemplatePath"] + fileName);

                htmlContent = htmlContent.Replace("[headtitle]", headtitle);
                htmlContent = htmlContent.Replace("[text]", text);
                htmlContent = htmlContent.Replace("[buttonurl]", buttonurl);
                htmlContent = htmlContent.Replace("[buttoncaption]", buttoncaption);
                htmlContent = htmlContent.Replace("[description]", description);
                if (!string.IsNullOrEmpty(howtotext))
                {
                    htmlContent = htmlContent.Replace("[howtotext]", howtotext);
                }
                if (!string.IsNullOrEmpty(howtourl))
                {
                    htmlContent = htmlContent.Replace("[howtourl]", howtourl);
                }
                if (!string.IsNullOrEmpty(img))
                {
                    htmlContent = htmlContent.Replace("[img]", img);
                }

                using (var _toolsContext = new GoHireNowToolsContext())
                {
                    var sender = new mailer_sender();
                    sender.ms_custom_id = customId;
                    sender.ms_custom_type = customType;
                    sender.ms_date = DateTime.Now;
                    sender.ms_send_date = DateTime.Now;
                    sender.ms_email = emailTo;
                    sender.ms_name = nameTo;
                    sender.ms_subject = subject;
                    sender.ms_message = htmlContent;
                    sender.ms_from_email = emailFrom;
                    sender.ms_from_name = nameFrom;
                    sender.ms_priority = priority;
                    sender.ms_issent = 0;
                    sender.ms_unsubscribe = Guid.NewGuid();

                    await _toolsContext.mailer_sender.AddAsync(sender);
                    await _toolsContext.SaveChangesAsync();
                }
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        public async Task PersonalEmailService(int customId, int customType, string emailTo, string nameTo, string subject, string clientName, string vaName, string emailFrom, string nameFrom, int priority, string fileName)
        {
            string htmlContent = new System.Net.WebClient().DownloadString(_configuration.GetSection("FilePaths")["EmailTemplatePath"] + fileName);

            htmlContent = htmlContent.Replace("[client_name]", clientName);
            htmlContent = htmlContent.Replace("[va_name]", vaName);

            using (var _toolsContext = new GoHireNowToolsContext())
            {
                var sender = new mailer_sender();
                sender.ms_custom_id = customId;
                sender.ms_custom_type = customType;
                sender.ms_date = DateTime.Now;
                sender.ms_send_date = DateTime.Now;
                sender.ms_email = emailTo;
                sender.ms_name = nameTo;
                sender.ms_subject = subject;
                sender.ms_message = htmlContent;
                sender.ms_from_email = emailFrom;
                sender.ms_from_name = nameFrom;
                sender.ms_priority = priority;
                sender.ms_issent = 0;
                sender.ms_unsubscribe = Guid.NewGuid();

                await _toolsContext.mailer_sender.AddAsync(sender);
                await _toolsContext.SaveChangesAsync();
            }
        }
    }
}
