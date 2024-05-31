using GoHireNow.Api.Filters;
using GoHireNow.Database;
using GoHireNow.Identity.Data;
using GoHireNow.Models.ContractModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using PusherServer;
using GoHireNow.Models.ConfigurationModels;
using Microsoft.Extensions.Options;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using GoHireNow.Models.CommonModels;
using System;
using Microsoft.Extensions.Configuration;

namespace GoHireNow.Api.Controllers
{
    [Route("contracts")]
    [ApiController]
    [CustomExceptionFilter]
    public class ContractController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContractService _contractService;
        private readonly IContractsInvoicesService _contractsInvoicesService;
        private readonly ICustomLogService _customLogService;
        private readonly IContractsSecuredService _contractsSecuredService;
        private IConfiguration _configuration { get; }
        private readonly PusherSettings _pusherSettings;
        private Pusher pusher;

        public ContractController(IContractService contractService, IConfiguration configuration, ICustomLogService customLogService, IOptions<PusherSettings> pusherSettings, IContractsInvoicesService contractsInvoicesService, IContractsSecuredService contractsSecuredService, UserManager<ApplicationUser> userManager)
        {
            _contractService = contractService;
            _contractsInvoicesService = contractsInvoicesService;
            _contractsSecuredService = contractsSecuredService;
            _configuration = configuration;
            _pusherSettings = pusherSettings.Value;
            _userManager = userManager;
            _customLogService = customLogService;

            var options = new PusherOptions
            {
                Cluster = _pusherSettings.AppCluster,
                Encrypted = true
            };

            pusher = new Pusher(
                _pusherSettings.AppId,
                _pusherSettings.AppKey,
                _pusherSettings.AppSecret,
                options
            );
        }

        [HttpPost]
        [Route("auth")]
        public IActionResult Auth([FromForm] string channel_name, [FromForm] string socket_id)
        {
            LogErrorRequest error;
            try
            {
                var auth = pusher.Authenticate(channel_name, socket_id);
                var json = auth.ToJson();
                return new ContentResult { Content = json, ContentType = "application/json" };
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/auth",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Authorize(Roles = "Client")]
        [Route("add")]
        public async Task<IActionResult> SendContract([FromBody] CreateModel model)
        {
            LogErrorRequest error;
            try
            {
                var res = await _contractService.CreateContract(UserId, model);
                if (res.Status)
                {
                    var unapproved = await _contractService.GetUnapprovalCount(model.ToUserId);

                    if (unapproved >= 0)
                    {
                        var result = await pusher.TriggerAsync(
                            $"contractApproved-{model.ToUserId}",
                            "contractApproved",
                            new
                            {
                                unapproved = unapproved
                            }
                        );
                    }
                }

                return Ok(res);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/add",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("isaccepted")]
        [Authorize]
        public async Task<IActionResult> IsAccepted([FromBody] UpdateStatusModel model)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.UpdateContractStatus(UserId, model);

                using (var _context = new GoHireNowContext())
                {
                    var contract = await _context.Contracts.Where(x => x.Id == model.ContractId).FirstOrDefaultAsync();
                    var unapproved = await _contractService.GetUnapprovalCount(contract.UserId);
                    var undeposited = await _contractService.GetUndepositCount(contract.CompanyId);

                    if (undeposited >= 0)
                    {
                        var result = await pusher.TriggerAsync(
                            $"contractDeposited-{contract.CompanyId}",
                            "contractDeposited",
                            new
                            {
                                undeposited = undeposited
                            }
                        );
                    }

                    if (unapproved >= 0)
                    {
                        var result = await pusher.TriggerAsync(
                            $"contractApproved-{contract.UserId}",
                            "contractApproved",
                            new
                            {
                                unapproved = unapproved
                            }
                        );
                    }
                }

                if (resp)
                    return Ok(new { Status = "success", Response = "Contract is updated" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during updating status" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/isaccepted",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("isautomaticbilling")]
        public async Task<IActionResult> isautomaticbilling([FromBody] UpdateAutomaticBillingModel model)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.isautomaticbilling(UserId, model);

                using (var _context = new GoHireNowContext())
                {
                    var contract = await _context.Contracts.Where(x => x.Id == model.ContractId).FirstOrDefaultAsync();
                    var unapproved = await _contractService.GetUnapprovalCount(contract.UserId);
                    var undeposited = await _contractService.GetUndepositCount(contract.CompanyId);

                    if (undeposited >= 0)
                    {
                        var result = await pusher.TriggerAsync(
                            $"contractDeposited-{contract.CompanyId}",
                            "contractDeposited",
                            new
                            {
                                undeposited = undeposited
                            }
                        );
                    }

                    if (unapproved >= 0)
                    {
                        var result = await pusher.TriggerAsync(
                            $"contractApproved-{contract.UserId}",
                            "contractApproved",
                            new
                            {
                                unapproved = unapproved
                            }
                        );
                    }
                }

                if (resp)
                    return Ok(new { Status = "success", Response = "Contract Automatic Billing is updated" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during updating Automatic Billing" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/isautomaticbilling",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("isautomaticdeposit")]
        public async Task<IActionResult> isautomaticdeposit([FromBody] UpdateAutomaticDepositModel model)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.isautomaticdeposit(UserId, model);

                if (resp)
                    return Ok(new { Status = "success", Response = "Contract Automatic deposit is updated" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during updating Automatic deposit" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/isautomaticdeposit",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("isautomaticrelease")]
        [Authorize]
        public async Task<IActionResult> isautomaticrelease([FromBody] UpdateAutomaticReleaseModel model)
        {
            LogErrorRequest error;
            try
            {
                var ip = GetPublicIpAddress();
                var resp = await _contractService.isautomaticrelease(UserId, model, ip);

                if (resp)
                    return Ok(new { Status = "success", Response = "Contract Automatic release is updated" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during updating Automatic release" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/isautomaticrelease",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("dispute")]
        [Authorize]
        public async Task<IActionResult> dispute([FromBody] DisputeModel model)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.Dispute(model, UserId);
                return Ok(resp);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/dispute",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("sendreport")]
        [Authorize]
        public async Task<IActionResult> sendReport([FromBody] ReportModel model)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.SendReport(UserId, model);

                if (resp)
                    return Ok(new { Status = "success", Response = "Report was saved" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during saving report" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/sendreport",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("updatehours")]
        public async Task<IActionResult> updaterate([FromBody] UpdateHoursModel model)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.UpdateHours(UserId, model);

                if (resp)
                    return Ok(new { Status = "success", Response = "Contract hours is updated" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during updating hours" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/updatehours",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpDelete]
        [Route("delete/{contractid}")]
        public async Task<IActionResult> DeleteAContract(int contractid)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.DeleteContract(UserId, contractid);

                using (var _context = new GoHireNowContext())
                {
                    var contract = await _context.Contracts.Where(x => x.Id == contractid).FirstOrDefaultAsync();
                    var unapproved = await _contractService.GetUnapprovalCount(contract.UserId);
                    var undeposited = await _contractService.GetUndepositCount(contract.CompanyId);

                    if (undeposited >= 0)
                    {
                        var result = await pusher.TriggerAsync(
                            $"contractDeposited-{contract.CompanyId}",
                            "contractDeposited",
                            new
                            {
                                undeposited = undeposited
                            }
                        );
                    }

                    if (unapproved >= 0)
                    {
                        var result = await pusher.TriggerAsync(
                            $"contractApproved-{contract.UserId}",
                            "contractApproved",
                            new
                            {
                                unapproved = unapproved
                            }
                        );
                    }
                }

                if (resp)
                    return Ok(new { Status = "success", Response = "Contract is deleted" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during deleting contract" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/delete/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("isContracted/{toUserId}")]
        public async Task<IActionResult> CheckContracted(string toUserId)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.CheckContracted(UserId, toUserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/isContracted/" + toUserId,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("archive/{contractId}")]
        public async Task<IActionResult> Archive(int contractId)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.Archive(UserId, contractId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/archive/" + contractId,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("get/{contractid}")]
        public async Task<IActionResult> GetContract(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetContractDetails(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/get/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getActiveContractsCount")]
        public async Task<IActionResult> GetActiveContractsCount()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetActiveContractsCount(UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getActiveContractsCount",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getContractSecured/{contractid}")]
        public async Task<IActionResult> GetContractSecured(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetContractSecured(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getContarctSecured/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getContractUnbilled/{contractid}")]
        public async Task<IActionResult> GetContractUnbilled(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetContractUnbilled(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getContractUnbilled/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getContractBalance/{contractid}")]
        public async Task<IActionResult> GetContractBalance(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetContractBalance(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getContractBalance/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getAccountBalance/{contractid}")]
        public async Task<IActionResult> GetAccountBalance(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetAccountBalance(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getAccountBalance/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getReleaseInvoices/{contractid}")]
        [Authorize]
        public async Task<IActionResult> GetReleaseInvoices(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetReleaseInvoices(contractid, UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getReleaseInvoices/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("release/{contractid}")]
        [Authorize]
        public async Task<IActionResult> Release(int contractid)
        {
            LogErrorRequest error;
            try
            {
                var ip = GetPublicIpAddress();
                return Ok(await _contractService.Release(contractid, ip, UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/release/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getContractSecurityStatus/{contractid}")]
        public async Task<IActionResult> GetContractStatus(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetContractStatus(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getContractSecurityStatus/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getContractTotalRevenue/{contractid}")]
        public async Task<IActionResult> GetContractTotalRevenue(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetContractTotalRevenue(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getContractTotalRevenue/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getContractCommission/{contractid}")]
        public async Task<IActionResult> GetContractCommission(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetContractCommission(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getContractCommission/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getContractLastPayment/{contractid}")]
        public async Task<IActionResult> GetContractLastPayment(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetContractLastPayment(UserId, contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getContractLastPayment/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getPaymentsHistory/{contractid}")]
        public async Task<IActionResult> GetPaymentsHistory(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractsInvoicesService.GetContractsInvoicesDetails(contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getPaymentHistory/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getPayoutHistory")]
        public async Task<IActionResult> GetPayoutHistory()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetPayoutTransactions(UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getPayoutHistory",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getInvoiceDetail/{id}")]
        public async Task<IActionResult> GetInvoiceDetail(int id)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractsInvoicesService.GetInvoiceDetail(UserId, id));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getInvoiceDetail/" + id,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getSecuredTransactions/{contractid}")]
        public async Task<IActionResult> GetSecuredTransactions(int contractid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractsSecuredService.GetContractsSecuredDetails(contractid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getSecuredTransactions/{contractid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("list-company")]
        public async Task<IActionResult> ListContractsCompany()
        {
            LogErrorRequest error;
            try
            {
                var list = await _contractService.ListContractsCompany(UserId);
                if (list != null && list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        var user = await _userManager.FindByIdAsync(item.UserId);

                        if (user != null && !user.IsDeleted)
                        {
                            item.LastLoginTime = user.LastLoginTime;
                            item.WorkerName = user.FullName;
                            item.Avatar = !string.IsNullOrEmpty(user.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{user.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}";
                        }
                    }
                }
                return Ok(list);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/list-company",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("list-user")]
        public async Task<IActionResult> ListContractsUser()
        {
            LogErrorRequest error;
            try
            {
                var list = await _contractService.ListContractsUser(UserId);
                if (list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        var user = await _userManager.FindByIdAsync(item.CompanyId);

                        if (user != null && !user.IsDeleted)
                        {
                            item.CompanyName = user.Company;
                            item.Avatar = !string.IsNullOrEmpty(user.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{user.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}";
                        }
                    }
                }
                return Ok(list);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/list-user",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("getWorkingHours")]
        public async Task<IActionResult> GetWorkingHours(GetHoursModel model)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetWorkingHours(model.contractId, model.week));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getWorkingHours",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("addHour")]
        [Authorize]
        public async Task<IActionResult> AddHour([FromBody] AddHourModel model)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.AddHour(UserId, model);

                if (resp)
                    return Ok(new { Status = "success", Response = "Work hours were added successfully" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during adding work hours" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/addHour",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("deleteHour/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteHour(int id)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _contractService.DeleteHour(id, UserId);

                if (resp)
                    return Ok(new { Status = "success", Response = "Work hours were added successfully" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during adding work hours" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/deleteHour/{id}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("unapprovalcontractcount")]
        public async Task<IActionResult> UnapprovalCount()
        {
            LogErrorRequest error;
            try
            {
                var unread = await _contractService.GetUnapprovalCount(UserId);
                if (unread > -1)
                {
                    return Ok(new { Status = "success", count = unread });
                }
                return Ok(new { Status = "error", count = 0, Response = "Some Error occured" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/unapprovalcontractcount",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("undepositcontractcount")]
        public async Task<IActionResult> UndepositCount()
        {
            LogErrorRequest error;
            try
            {
                var unread = await _contractService.GetUndepositCount(UserId);
                if (unread > -1)
                {
                    return Ok(new { Status = "success", count = unread });
                }
                return Ok(new { Status = "error", count = 0, Response = "Some Error occured" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/undepositcontractcount",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getHRContract/{hrId}")]
        public async Task<IActionResult> GetHRContract(string hrId)
        {
            try
            {
                return Ok(await _contractService.GetHRContract(hrId));
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/contracts/getHRContract/" + hrId,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        public async Task<IActionResult> PendingContractAction()
        {
            var _context = new GoHireNowContext();
            var pendingContracts = await _context.Contracts.Where(c => c.isAccepted == 1 && c.IsDeleted == 0 && c.AutomaticBilling == 0).ToListAsync();
            foreach (var contract in pendingContracts)
            {
                var lastSecure = await _context.ContractsSecured.Where(c => c.ContractId == contract.Id && c.Amount > 0 && c.Type == 1 && c.IsDeleted == 0).OrderByDescending(c => c.CreatedDate).FirstOrDefaultAsync();
                if ((lastSecure != null && lastSecure.CreatedDate < DateTime.UtcNow.AddDays(-30)) || (lastSecure == null && contract.CreatedDate < DateTime.UtcNow.AddDays(-30)))
                {
                    contract.isAccepted = 2;
                    await _context.SaveChangesAsync();

                    var company = await _context.AspNetUsers.Where(x => x.Id == contract.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (company != null)
                    {
                        string subject = "Your pending contract has been canceled - " + contract.Name;
                        string headtitle = "Your pending contract has been canceled";
                        string message = contract.Name;
                        string description = "We have canceled your contract pending a security deposit since it has been pending for more than 30 days.\nYou can create a new contract at any time.";
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/user-payment-detail/" + contract.Id;
                        string buttoncaption = "View Contract";
                        await _contractService.NewMailService(0, 42, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }
                }
            }
            return Ok();
        }

        public async Task<IActionResult> WorkerHourEmailAction()
        {
            DateTime currentDate = DateTime.Now;
            DayOfWeek currentDayOfWeek = currentDate.DayOfWeek;
            DateTime startOfWeek = currentDate.AddDays(-(int)currentDayOfWeek + 1).Date;
            DateTime endOfWeek = startOfWeek.AddDays(7);
            var _context = new GoHireNowContext();
            var onGoingContracts = await _context.Contracts.Where(c => c.isAccepted == 1 && c.IsDeleted == 0 && c.AutomaticBilling == 1).ToListAsync();
            foreach (var item in onGoingContracts)
            {
                var hours = await _context.ContractsHours.Where(c => c.IsDeleted == 0 && c.ContractId == item.Id && c.WorkedDate >= startOfWeek && c.WorkedDate < endOfWeek).ToListAsync();
                decimal totalHours = hours.Sum(h => h.Hours);
                if (totalHours < item.Hours * 0.5m)
                {
                    var worker = await _context.AspNetUsers.Where(x => x.Id == item.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (worker != null)
                    {
                        string subject = "Update your worked hours - " + item.Name;
                        string headtitle = "";
                        string message = item.Name;
                        string description = "You only have 24 hours left to enter your worked hours into the contract tracking section to get paid.<br/>If you already entered your hours please ignore this email.";
                        string howtotext = "HOW TO: Track Your Hours";
                        string howtourl = "https://www.youtube.com/watch?v=cKKqLxsTFbo";
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/billing-detail/" + item.Id;
                        string buttoncaption = "View Contract";
                        await _contractService.NewMailService(0, 27, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "HowToEmailTemplate.html", howtotext, howtourl);
                    }

                }
            }
            return Ok();
        }

        public async Task<IActionResult> SendSecondEmail(string userId, int id, int type)
        {
            var _context = new GoHireNowContext();
            var worker = await _context.AspNetUsers.Where(x => x.Id == userId && x.IsDeleted == false).FirstOrDefaultAsync();
            var reference = await _context.UserReferences.Where(x => x.Id == id && x.IsDeleted == 0).FirstOrDefaultAsync();
            if (worker != null && reference != null && reference.IsAccepted == 0 && ((type == 2 && reference.IsInvited == 1) || (type == 3 && reference.IsInvited == 2)))
            {
                string subject = type == 2 ? "RE: " + worker.FullName + " needs your help." : "RE: Final notice - " + worker.FullName + " is waiting for your feedback!";
                string headtitle = type == 2 ? "RE: " + worker.FullName + " needs your help." : "RE: Final notice - " + worker.FullName + " is waiting for your feedback!";
                string message = "Hello " + reference.Contact + ", " + worker.FullName + " is currently looking for a job on our platform.<br/>It would really help if you could leave a feedback from " + reference.Company;
                string description = "Reference: " + reference.JobTitle + ", " + reference.FromDate + " ~ " + reference.ToDate + ", " + reference.Company + ".<br/>It only takes 30 seconds to leave a feedback on " + worker.FullName + "'s profile.";
                string buttonurl = _configuration.GetValue<string>("WebDomain") + "/work-profile/" + userId + "/" + reference.InviteID;
                string buttoncaption = "Leave Feedback";
                string img = !string.IsNullOrEmpty(worker.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{worker.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}";
                await _contractService.NewMailService(0, 50, reference.Email, reference.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ReferenceRequireTemplate.html", "", "", img);

                reference.IsInvited = type;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        private string GetPublicIpAddress()
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress.ToString();

            if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.ToString().Split(',')[0].Trim();
            }

            return ipAddress;
        }
    }
}
