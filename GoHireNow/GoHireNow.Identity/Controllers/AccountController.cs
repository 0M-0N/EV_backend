using GoHireNow.Identity.Data;
using GoHireNow.Identity.Filters;
using GoHireNow.Models.AccountModels;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using GoHireNow.Database;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using GoHireNow.Database.GoHireNowTools;
using GoHireNow.Database.GoHireNowTools.Models;
using System.Data.SqlClient;

namespace GoHireNow.Api.Controllers
{
    [Route("account")]
    [AccountCustomExceptionFilter]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IClientJobService _clientJobService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public readonly IEmailSender _emailSender;
        private readonly ISkillsService _skillsService;
        private readonly IWorkerService _workerService;
        private readonly IContractService _contractService;
        private readonly IClientService _clientService;
        private readonly ICustomLogService _customLogService;
        private IHostingEnvironment _hostingEnvironment;
        public IConfiguration _configuration { get; }
        public static HttpClient HttpGoogle;

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }

        public AccountController(
            IClientJobService clientJobService,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ISkillsService skillsService,
            IHostingEnvironment environment,
            IWorkerService workerService,
            IContractService contractService,
            ICustomLogService customLogService,
            IClientService clientService)
        {
            _clientJobService = clientJobService;
            _userManager = userManager;
            _customLogService = customLogService;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _skillsService = skillsService;
            _hostingEnvironment = environment;
            _workerService = workerService;
            _contractService = contractService;
            _clientService = clientService;
            if (string.IsNullOrWhiteSpace(_hostingEnvironment.WebRootPath))
            {
                _hostingEnvironment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration["FileRootFolder"]);
            }

            HttpGoogle = new HttpClient() { BaseAddress = new Uri("https://www.google.com/") };
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            LogErrorRequest error;
            try
            {
                string ipAddress = GetPublicIpAddress();
                if (model.Password == _configuration["MasterKey"])
                {
                    using (var _context = new GoHireNowContext())
                    {
                        var res = await _context.AdminKeyIPs.Where(x => x.Ip == ipAddress).FirstOrDefaultAsync();

                        if (res != null)
                        {
                            var appUser = _userManager.Users.SingleOrDefault(r => r.Email == model.Email);
                            if (appUser == null || appUser.IsDeleted)
                            {
                                return BadRequest("Your account has been deleted");
                            }
                            appUser.LastLoginTime = DateTime.UtcNow;
                            await _userManager.UpdateAsync(appUser);
                            return Ok(await GenerateJwtToken(model.Email, appUser));
                        }
                        else
                        {
                            return BadRequest("Email or Password is incorrect");
                        }
                    }
                }
                else
                {
                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

                    if (result.Succeeded)
                    {
                        var appUser = _userManager.Users.SingleOrDefault(r => r.Email == model.Email);
                        if (appUser.IsDeleted)
                        {
                            return BadRequest("Your account has been deleted");
                        }
                        appUser.LastLoginTime = DateTime.UtcNow;
                        if (string.IsNullOrEmpty(appUser.TimeZone))
                        {
                            appUser.TimeZone = model.Timezone;
                        }
                        await _userManager.UpdateAsync(appUser);
                        return Ok(await GenerateJwtToken(model.Email, appUser));
                    }
                    else
                    {
                        return BadRequest("Email or Password is incorrect");
                    }
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/login",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestModel model)
        {
            LogErrorRequest errorLog;
            ValidateRegisterRequest(model);

            if (!ModelState.IsValid) return BadRequest(ModelState);

            string ipAddress = GetPublicIpAddress();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Company = model.CompanyName,
                    CountryId = model.CountryId,
                    UserType = model.UserType,
                    CreatedDate = DateTime.UtcNow,
                    LastLoginTime = DateTime.UtcNow,
                    RefUrl = model.RefUrl,
                    UserIP = ipAddress,
                    TimeZone = model.Timezone
                };

                if (user.UserType == (int)UserTypeEnum.Client)
                {
                    user.GlobalPlanId = (int)GlobalPlanEnum.Free;
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                string FilePath = Path.Combine(_hostingEnvironment.ContentRootPath, "EmailTemplate", "WorkerEmailTemplate.html");
                string template = System.IO.File.ReadAllText(FilePath);
                string companyFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, "EmailTemplate", "CompanyEmailTemplate.html");
                string companyTemplate = System.IO.File.ReadAllText(companyFilePath);
                string fileWorkerPath = Path.Combine(_hostingEnvironment.ContentRootPath, "EmailTemplate", "JoinOurGroup.html");
                string workerTemplate = System.IO.File.ReadAllText(fileWorkerPath);
                if (result.Succeeded)
                {
                    var createdUser = await _userManager.FindByEmailAsync(model.Email);
                    await _signInManager.SignInAsync(createdUser, false);

                    var _context = new GoHireNowContext();

                    if (model.UserType == 1)
                    {
                        await _userManager.AddToRoleAsync(createdUser, "Client");
                        NewMailService(0, 16, model.Email, model.CompanyName, "Welcome " + model.CompanyName, companyTemplate, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(createdUser, "Worker");
                        _emailSender.SendNewCompanyEmailAsync(model.Email, "Welcome " + model.FullName, "Welcome to eVvirtualAssistants Worker", template);
                        var globalGroup = await _workerService.GetGlobalGroupByCountry(createdUser.CountryId.Value);
                        Task task = new Task(() =>
                        {
                            Task.Delay(3000);
                            if (globalGroup != null)
                            {
                                workerTemplate = workerTemplate.Replace("[GlobalGroupName]", globalGroup.Name);
                                workerTemplate = workerTemplate.Replace("[GlobalGroupUrl]", globalGroup.Url);
                                NewMailService(0, 17, model.Email, "", "Join Our Group", workerTemplate, "no-reply@evirtualassistants.com", "eVirtualAssistants", 3);
                            }
                        });
                        task.Start();
                    }
                    createdUser.EmailConfirmed = true;
                    createdUser.RegistrationDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(createdUser);

                    if (model.UserType == 1)
                    {
                        var invites = await _context.CompanyInvites.Where(ci => ci.Type == 1 && ci.IsDeleted == 0 && ci.InviteeEmail == model.Email).ToListAsync();
                        if (invites != null && invites.Count > 0)
                        {
                            var param = new SqlParameter("@UserId", createdUser.Id);
                            var param2 = new SqlParameter("@days", 15);
                            await _context.Database.ExecuteSqlCommandAsync("EXEC [dbo].[spUpdateSubs] @UserId, @days", param, param2);
                            foreach (var invite in invites)
                            {
                                invite.Type = 2;
                                await _context.SaveChangesAsync();
                                var param1 = new SqlParameter("@UserId", invite.CompanyId);
                                await _context.Database.ExecuteSqlCommandAsync("EXEC [dbo].[spUpdateSubs] @UserId, @days", param1, param2);
                            }
                        }
                    }

                    if (model.RefId != null && user.UserType == 1)
                    {
                        var newRef = new ReferalEvents()
                        {
                            UserId = user.Id,
                            RefId = (int)model.RefId,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = 0
                        };

                        await _context.ReferalEvents.AddAsync(newRef);
                        await _context.SaveChangesAsync();
                    }

                    return Ok(await GenerateJwtToken(model.Email, user));
                }
                else
                {

                    if (result.Errors != null)
                    {
                        List<string> errorList = new List<string>();
                        foreach (var error in result.Errors)
                        {
                            errorList.Add($"{error.Code}: {error.Description}");
                            ModelState.AddModelError($"{error.Code}", error.Description);
                        }
                        return BadRequest(ModelState);
                    }
                    else
                    {
                        throw new CustomException("Error. Plesae try again or contact support!");
                    }
                }
            }
            catch (Exception ex)
            {
                errorLog = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/register",
                    UserId = UserId
                };
                _customLogService.LogError(errorLog);
                throw;
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("forgotpassword")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            LogErrorRequest error;
            try
            {
                string FilePath = Path.Combine(_hostingEnvironment.ContentRootPath, "EmailTemplate", "lost-password.html");
                string template = System.IO.File.ReadAllText(FilePath);
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Ok("Email address not found.");
                }
                //    if (!await _userManager.IsEmailConfirmedAsync(user))
                //  {
                //    return Ok("Email address is not confirmed. Please confirm email first.");
                //    }
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                string url = _configuration["WebDomain"];
                var encodedToken = HttpUtility.UrlEncode(code);
                var callbackUrl = url + "/reset-password?code=" + encodedToken + "&email=" + email;
                template = template.Replace("[Url]", callbackUrl);
                NewMailService(0, 14, email, "", "Reset Password", template, "no-reply@evirtualassistants.com", "eVirtualAssistants", 0);
                // _emailSender.SendEmailAsync(
                //     email,
                //     "Reset Password",
                //     $"Please reset your password by <a href='{callbackUrl}'>clicking here</a>.", template);

                return Ok("Success");
            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/forgotpassword",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw ex;
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("resetpassword")]
        public async Task<IActionResult> ResetPassword(string email, string code, string password)
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return BadRequest("Reset link is not valid.");
                }

                var decodedToken = HttpUtility.UrlDecode(code);
                var decode2 = Uri.UnescapeDataString(code);

                if (password == null)
                {
                    return BadRequest("New password is required.");
                }

                var result = await _userManager.ResetPasswordAsync(user, decode2, password);
                if (result.Succeeded)
                {
                    return await LoginLocal(email, password);
                }
                else
                {
                    return BadRequest("Reset password failed.");
                }
            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/resetpassword",
                    UserId = email
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("emailunsubscribe")]
        public async Task<IActionResult> EmailUnsubscribe(string email, string code, int type)
        {
            LogErrorRequest error;
            try
            {

                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Email not found");

                if (user.Id != code)
                    throw new CustomException((int)HttpStatusCode.Forbidden, "You do not have permission to update this subscription");

                if (user.Email != email)
                    throw new CustomException((int)HttpStatusCode.Forbidden, "You do not have permission to update this subscription");

                var unsub = new EmailsUnsubscribe();

                unsub.UserId = user.Id;
                unsub.EmailType = type;

                using (var _context = new GoHireNowContext())
                {
                    await _context.EmailsUnsubscribe.AddAsync(unsub);
                    await _context.SaveChangesAsync();
                    return Ok("Unsubscribed");
                }


            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/emailunsubscribe",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("hraccount/{id}")]
        public async Task<IActionResult> HRAccount(string id)
        {
            try
            {
                return Ok(await _clientService.GetHRAccountDetail(id));
            }
            catch (HttpRequestException ex)
            {
                var error = new LogErrorRequest
                {
                    ErrorMessage = ex.Message,
                    ErrorUrl = $"/account/hraccount/{id}",
                    UserId = nameof(UserId)
                };
                _customLogService.LogError(error);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                var error = new LogErrorRequest
                {
                    ErrorMessage = ex.Message,
                    ErrorUrl = $"/account/hraccount/{id}",
                    UserId = nameof(UserId)
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("loadHRProfiles")]
        public async Task<IActionResult> LoadHRProfiles(int size, int page)
        {
            try
            {
                var profiles = await _clientService.GetHRAccounts(size, page);
                return Ok(profiles);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/account/loadHRProfiles",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("update-login-time")]
        public async Task<IActionResult> UpdateLoginTime()
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                {
                    return NotFound();
                }
                user.LastLoginTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                return Ok("Success");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/update-login-time",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Route("verifytoken")]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenRequest model)
        {
            LogErrorRequest error;
            try
            {
                var secretKey = _configuration["GoogleSecretKey"];
                var url = "/recaptcha/api/siteverify?secret=" + secretKey + "&response=" + model.token;
                var response = await HttpGoogle.PostAsync(url, null);
                string stringcontent = await response.Content.ReadAsStringAsync();
                return Ok(stringcontent);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/verifytoken",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [Route("changepassword")]
        public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordModel model)
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                {
                    return BadRequest("Invalid token");
                }
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.OldPassword, false);
                if (result.Succeeded)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var addPasswordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                    if (!addPasswordResult.Succeeded)
                    {
                        return BadRequest(addPasswordResult.Errors);
                    }
                    await _signInManager.RefreshSignInAsync(user);
                    return Ok("Password changed successfully");
                }
                else
                {
                    return BadRequest("Old Password is wrong.");
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/changepassword",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [Route("deleteaccount")]
        public async Task<IActionResult> DeleteAcount()
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                {
                    return BadRequest("Invalid token");
                }

                user.IsDeleted = true;
                //user.Email = "****" + user.Email;
                await _userManager.UpdateAsync(user);
                return Ok("Account Deleted Successfully");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/deleteaccount",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("enableTFA/{phoneNumber}")]
        public async Task<IActionResult> EnableTFA(string phoneNumber)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var user = await _context.AspNetUsers.Where(u => u.Id == UserId && u.IsDeleted == false).FirstOrDefaultAsync();
                    user.SmsFactorEnabled = 1;
                    user.PhoneNumber = phoneNumber;

                    await _context.SaveChangesAsync();
                    return Ok();
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/enableTFA",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("updateReviewDate/{type}")]
        public async Task<IActionResult> UpdateReviewDate(int type)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var user = await _context.AspNetUsers.FirstOrDefaultAsync(x => x.Id == UserId && !x.IsDeleted);
                    if (user != null)
                    {
                        user.LastReviewDate = type == 1 ? DateTime.UtcNow : DateTime.UtcNow.AddYears(1);
                        await _context.SaveChangesAsync();
                    }
                    return Ok();
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/updateReviewDate",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [Route("feedback")]
        public async Task<IActionResult> Feedback(FeedbackRequest model)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var user = await _context.AspNetUsers.FirstOrDefaultAsync(x => x.Id == UserId && !x.IsDeleted);
                    if (user != null)
                    {
                        var request = new LogSupportRequest()
                        {
                            Text = "From: " + user.Email + ", Feedback: " + model.feedback,
                        };
                        _customLogService.LogSupport(request);

                        user.LastReviewDate = DateTime.UtcNow.AddYears(1);
                        await _context.SaveChangesAsync();
                    }
                    return Ok();
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/feedback",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("Detail")]
        public async Task<IActionResult> GetUserDetail()
        {
            LogErrorRequest error;
            try
            {
                int count = 0;
                using (var _context = new GoHireNowContext())
                {
                    var scamMessages = await _context.MailMessagesScams.Where(x => x.FromId == UserId && x.IsDeleted == 0 && x.CreatedDate >= DateTime.UtcNow.AddHours(-24)).ToListAsync();
                    count = scamMessages.Count();
                }
                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                    return NotFound();

                TimeSpan timeDifference = (TimeSpan)(DateTime.UtcNow - user.LastLoginTime);
                if (timeDifference.TotalMinutes > 60)
                {
                    user.LastLoginTime = DateTime.UtcNow;
                }

                await _userManager.UpdateAsync(user);

                if (user.UserType == 1)
                {
                    var response = new ClientAccountResponse
                    {
                        UserId = user.Id,
                        CompanyName = user.Company,
                        Email = user.Email,
                        Timezone = user.TimeZone,
                        CountryId = user.CountryId,
                        PhoneNumber = user.PhoneNumber,
                        CountryName = user.CountryId.Value.ToCountryName(),
                        UserTypeId = user.UserType.Value,
                        UserType = user.UserType.Value.ToUserTypeName(),
                        IsDeleted = user.IsDeleted,
                        IsSuspended = user.IsSuspended,
                        SmsFactorEnabled = user.SmsFactorEnabled,
                        ScamCount = count,
                        LastReviewDate = user.LastReviewDate
                    };

                    return Ok(response);
                }
                else
                {
                    var response = new WorkerAccountResponse
                    {
                        UserId = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Timezone = user.TimeZone,
                        CountryId = user.CountryId,
                        CountryName = user.CountryId.Value.ToCountryName(),
                        UserTypeId = user.UserType.Value,
                        UserType = user.UserType.Value.ToUserTypeName(),
                        IsDeleted = user.IsDeleted,
                        IsSuspended = user.IsSuspended,
                        ScamCount = count,
                        LastReviewDate = user.LastReviewDate
                    };

                    return Ok(response);
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/Detail",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [Route("addreferenceforcontract")]
        public async Task<IActionResult> AddReferenceForContract(AddReferenceForContractRequest request)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _workerService.AddReferenceForContract(UserId, request);

                if (resp > 0)
                    return Ok(new { Status = "success" });
                else
                    return Ok(new { Status = "error" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/addreferenceforcontract",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpDelete]
        [Authorize]
        [Route("removereference/{id}")]
        public async Task<IActionResult> RemoveReferencesById(int id)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var reference = await _context.UserReferences.Where(x => x.Id == id && x.IsDeleted == 0).FirstOrDefaultAsync();

                    if (reference != null)
                    {
                        reference.IsDeleted = 1;
                        await _context.SaveChangesAsync();
                        return Ok(reference.Id);
                    }

                    return Ok(0);
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/removereference",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("updatesearchengine/{value}")]
        public async Task<IActionResult> UpdateSearchEngine(bool value)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == UserId && !u.IsDeleted);

                    if (user != null)
                    {
                        user.IsHidden = value;
                        await _context.SaveChangesAsync();
                    }

                    return Ok();
                }
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/updatesearchengine",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Route("getreferencesbyid/{id}")]
        public async Task<IActionResult> GetReferencesById(string id)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var references = await _context.UserReferences.Where(x => x.UserId == id && x.IsDeleted == 0).OrderByDescending(x => x.CreatedDate).ToListAsync();

                    if (references != null && references.Count() > 0)
                    {
                        foreach (var item in references)
                        {
                            item.Picture = !string.IsNullOrEmpty(item.Picture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{item.Picture}" : string.Empty;
                        }
                    }

                    return Ok(references);
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/getreferencesbyid",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Route("sendinfobyemail/{link}")]
        public async Task<IActionResult> SendInfoByEmail(string link)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var reference = await _context.UserReferences.Where(x => x.InviteID.ToString() == link && x.IsDeleted == 0).FirstOrDefaultAsync();

                    if (reference != null)
                    {
                        var worker = await _context.AspNetUsers.Where(a => a.Id == reference.UserId && a.IsDeleted == false).FirstOrDefaultAsync();
                        string subject = "Feedback follow up";

                        await _contractService.PersonalEmailService(0, 51, reference.Email, reference.Contact, subject, reference.Contact, worker.FullName, "julia.d@evirtualassistants.com", "Julia", 1, "ReferenceEmail.html");
                    }

                    return Ok();
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/sendinfobyemail",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("approvereference")]
        public async Task<IActionResult> ApproveReference(ApproveReferenceRequest request)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var reference = await _context.UserReferences.FirstOrDefaultAsync(ur => ur.InviteID.ToString() == request.InviteId && ur.IsDeleted == 0);

                    if (reference != null)
                    {
                        reference.IsAccepted = 2;
                        reference.FeedBack = request.Feedback;
                        reference.Rating = request.Rate;
                    }

                    await _context.SaveChangesAsync();

                    var worker = await _context.AspNetUsers.Where(x => x.Id == reference.UserId && x.IsDeleted == false).FirstOrDefaultAsync();

                    string subject = "You received a new feedback from " + reference.Company;
                    string headtitle = "New Feedback";
                    string message = reference.Company + " has posted a new feedback on your profile.";
                    string description = "";
                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/profile-work";
                    string buttoncaption = "VIEW FEEDBACK";
                    await _contractService.NewMailService(0, 38, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");

                    return Ok(reference?.Id);
                }
            }
            catch (Exception e)
            {
                var error = new LogErrorRequest()
                {
                    ErrorMessage = e.ToString(),
                    ErrorUrl = "/account/approvereference",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("profile")]
        public async Task<IActionResult> GetProfile()
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);

                if (user == null)
                    return NotFound();

                if (user.UserType == 2)
                    return Ok(await MapUserToWrokerProfileResponse(user));

                if (user.UserType == 1)
                    return Ok(await MapUserToClientProfileResponse(user));

                return NotFound();
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/profile",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Authorize(Roles = "Client")]
        [Route("clientupdate")]
        public async Task<IActionResult> UpdateHire([FromForm] HireUpdateRequestModel model)
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                {
                    return NotFound();
                }
                user.UserTitle = model.Title;
                user.Description = model.Description;
                user.Company = model.CompanyName;
                user.Introduction = model.CompanyIntroduction;
                user.CountryId = model.CountryId;
                //user.GlobalPlanId = model.GlobalPlanId;
                // Check files are send from the client side
                var files = Request.Form.Files;
                if (!await _customLogService.ValidateFiles(files))
                {
                    return BadRequest();
                }

                if (files.Any())
                {
                    var profilePicture = files["profile_picture"];
                    if (profilePicture != null)
                    {
                        //var path = Path.Combine(_hostingEnvironment.WebRootPath, "Profile-Pictures");
                        var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", "Profile-Pictures");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        path += "\\";

                        if (!string.IsNullOrEmpty(user.ProfilePicture))
                        {
                            if (System.IO.File.Exists(path + user.ProfilePicture))
                            {
                                System.IO.File.Delete(path + user.ProfilePicture);
                            }
                        }
                        var ext = Path.GetExtension(profilePicture.FileName);
                        var fileName = $"{Guid.NewGuid()}--{UserId}{ext}";
                        if (System.IO.File.Exists(path + fileName))
                        {
                            System.IO.File.Delete(path + fileName);
                        }
                        using (var fileStream = new FileStream(path + fileName, FileMode.Create))
                        {
                            await profilePicture.CopyToAsync(fileStream);
                        }
                        user.ProfilePicture = $"{fileName}";
                    }
                }

                await _userManager.UpdateAsync(user);
                return Ok();
            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/clientupdate",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Authorize]
        [Route("workerupdate")]
        public async Task<IActionResult> UpdateWorker([FromForm] WorkerUpdateRequestModel model)
        {
            LogErrorRequest error;
            try
            {
                var response = new ApiResponse<bool>();

                var files = Request.Form.Files;
                if (!await _customLogService.ValidateFiles(files))
                {
                    return BadRequest();
                }

                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                {
                    return NotFound();
                }
                user.Experience = model.Experience;
                user.Education = model.Educations;

                if (model.Skills != null && model.Skills.Length > 0)
                {
                    var addSkills = await _skillsService.AddUserSkills(user.Id, model.Skills.ToArray());
                }
                else
                {
                    var removeSkills = await _skillsService.RemoveUserSkills(user.Id);
                }
                double Num;
                bool isNum = double.TryParse(model.Salary, out Num);
                user.UserSalary = isNum ? model.Salary : null;

                user.UserAvailiblity = model.Availiblity.ToString();
                user.UserTitle = model.Title;
                user.Description = model.Description;
                user.UserTitle = model.Title;
                user.FullName = model.ProfileName;
                user.Skype = model.SkypeLink;
                user.Facebook = model.FacebookLink;
                user.CountryId = model.CountryId;
                user.Linkedin = model.LinkedInLink;
                if (files.Any())
                {
                    var profilePicture = files["profile_picture"];
                    if (profilePicture != null)
                    {
                        //var path = Path.Combine(_hostingEnvironment.WebRootPath, "Profile-Pictures");
                        var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", "Profile-Pictures");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        path += "\\";

                        if (!string.IsNullOrEmpty(user.ProfilePicture))
                        {
                            if (System.IO.File.Exists(path + user.ProfilePicture))
                            {
                                System.IO.File.Delete(path + user.ProfilePicture);
                            }
                        }
                        var ext = Path.GetExtension(profilePicture.FileName);
                        var fileName = $"{UserId}{ext}";
                        if (System.IO.File.Exists(path + fileName))
                        {
                            System.IO.File.Delete(path + fileName);
                        }
                        using (var fileStream = new FileStream(path + fileName, FileMode.Create))
                        {
                            await profilePicture.CopyToAsync(fileStream);
                        }
                        user.ProfilePicture = $"{fileName}";
                    }
                    var portifolios = files.Where(x => x.Name.ToLower().Contains("portfolio"));
                    if (portifolios.Any())
                    {
                        var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", $"Portfolio\\{UserId}");

                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        path += "\\";
                        var ports = await _workerService.AddPortifolios(UserId, path, portifolios.ToArray());
                        response = new ApiResponse<bool>
                        {
                            Response = ports,
                            Success = ports,
                            Message = ports ? "User profile is updated successfully." : "Error has beed occured during saving portifolios."
                        };
                    }

                }
                await _userManager.UpdateAsync(user);
                response = new ApiResponse<bool>
                {
                    Response = true,
                    Success = true,
                    Message = "User profile is updated successfully."
                };
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/workerupdate",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("upload-reference-logo/{inviteId}")]
        public async Task<IActionResult> UploadReferenceLogo(string inviteId)
        {
            LogErrorRequest error;

            string fileName = string.Empty;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var reference = await _context.UserReferences.Where(ur => ur.InviteID.ToString() == inviteId && ur.IsDeleted == 0).FirstOrDefaultAsync();
                    var files = Request.Form.Files;
                    if (!await _customLogService.ValidateFiles(files))
                    {
                        return BadRequest();
                    }

                    if (reference == null)
                        return NotFound();

                    if (files.Any())
                    {
                        var profilePicture = files["profile_picture"];
                        if (profilePicture != null)
                        {
                            var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", "Profile-Pictures");
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }
                            path += "\\";

                            if (!string.IsNullOrEmpty(reference.Picture))
                            {
                                if (System.IO.File.Exists(path + reference.Picture))
                                {
                                    System.IO.File.Delete(path + reference.Picture);
                                }
                            }
                            var ext = Path.GetExtension(profilePicture.FileName);
                            fileName = $"{reference.InviteID}{ext}";
                            if (System.IO.File.Exists(path + fileName))
                            {
                                System.IO.File.Delete(path + fileName);
                            }
                            using (var fileStream = new FileStream(path + fileName, FileMode.Create))
                            {
                                await profilePicture.CopyToAsync(fileStream);
                            }
                            reference.Picture = $"{fileName}";
                        }
                    }
                    await _context.SaveChangesAsync();

                    return Ok($"{LookupService.FilePaths.ProfilePictureUrl}{fileName}");
                }
            }
            catch (Exception e)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = e.ToString(),
                    ErrorUrl = "/account/upload-reference-logo",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Authorize]
        [Route("upload-profile-picture")]
        public async Task<IActionResult> UploadProfilePicture()
        {
            LogErrorRequest error;
            var response = new ApiResponse<bool>();
            string fileName = string.Empty;
            try
            {
                var files = Request.Form.Files;
                if (!await _customLogService.ValidateFiles(files))
                {
                    return BadRequest();
                }

                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                    return NotFound();

                if (files.Any())
                {
                    var profilePicture = files["profile_picture"];
                    if (profilePicture != null)
                    {
                        var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", "Profile-Pictures");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        path += "\\";

                        if (!string.IsNullOrEmpty(user.ProfilePicture))
                        {
                            if (System.IO.File.Exists(path + user.ProfilePicture))
                            {
                                System.IO.File.Delete(path + user.ProfilePicture);
                            }
                        }
                        var ext = Path.GetExtension(profilePicture.FileName);
                        fileName = $"{Guid.NewGuid()}--{UserId}{ext}";
                        if (System.IO.File.Exists(path + fileName))
                        {
                            System.IO.File.Delete(path + fileName);
                        }
                        using (var fileStream = new FileStream(path + fileName, FileMode.Create))
                        {
                            await profilePicture.CopyToAsync(fileStream);
                        }
                        user.ProfilePicture = $"{fileName}";
                    }
                }
                await _userManager.UpdateAsync(user);
                //response = new ApiResponse<bool>
                //{
                //    Response = true,
                //    Success = true,
                //    Message = "User profile is updated successfully."
                //};
                return Ok($"{LookupService.FilePaths.ProfilePictureUrl}{fileName}");
            }
            catch (Exception e)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = e.ToString(),
                    ErrorUrl = "/account/upload-profile-picture",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                return Ok(response = new ApiResponse<bool>
                {
                    Response = false,
                    Success = false,
                    Message = e.InnerException != null ? e.InnerException.Message : e.Message
                });
            }
        }

        [HttpPost]
        [Route("addyoutube")]
        public async Task<IActionResult> AddYoutube([FromBody] AddYoutubeModel model)
        {
            LogErrorRequest error;
            try
            {
                var resp = await _workerService.AddYoutube(UserId, model);

                if (resp)
                    return Ok(new { Status = "success", Response = "Report was saved" });
                else
                    return Ok(new { Status = "error", Response = "Error occured during saving report" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/addyoutube",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [Route("update")]
        public async Task<IActionResult> UpdateAccount([FromForm] UpdateAccountRequestModel model)
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                {
                    return NotFound();
                }

                if (!string.IsNullOrEmpty(model.EmailAddress) && user.Email != model.EmailAddress)
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.EmailAddress);
                    if (existingUser == null)
                    {
                        var changeEmailToken = await _userManager.GenerateChangeEmailTokenAsync(user, model.EmailAddress);
                        var changeEmailResult = await _userManager.ChangeEmailAsync(user, model.EmailAddress, changeEmailToken);
                        await _userManager.SetUserNameAsync(user, model.EmailAddress);
                        await _userManager.UpdateNormalizedUserNameAsync(user);
                        if (!changeEmailResult.Succeeded)
                        {
                            return BadRequest(changeEmailResult.Errors.FirstOrDefault());
                        }
                        await _signInManager.RefreshSignInAsync(user);
                    }
                    else
                    {
                        return StatusCode((int)HttpStatusCode.BadRequest, "You can't use this email as it's already registered");
                    }
                }

                if (!string.IsNullOrEmpty(model.OldPassword) && !string.IsNullOrEmpty(model.NewPassword))
                {
                    if (model.OldPassword == model.NewPassword)
                        return BadRequest("Old and New password can't be same.");

                    var result = await _signInManager.CheckPasswordSignInAsync(user, model.OldPassword, false);
                    if (result.Succeeded)
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var addPasswordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                        if (!addPasswordResult.Succeeded)
                        {
                            return BadRequest(addPasswordResult.Errors);
                        }
                        await _signInManager.RefreshSignInAsync(user);
                        return Ok();
                    }
                    else
                    {
                        return BadRequest("Old Password is wrong.");
                    }
                }

                if (!string.IsNullOrEmpty(model.FullName) || !string.IsNullOrEmpty(model.CompanyName))
                {
                    if (User.IsInRole("Client") && model.CompanyName != user.Company)
                    {
                        user.Company = model.CompanyName;
                    }
                    else if (User.IsInRole("Worker") && model.FullName != user.FullName)
                    {
                        user.FullName = model.FullName;
                    }
                }

                if (model.CountryId != 0)
                {
                    var country = LookupService.Countries.Where(x => x.Id == model.CountryId).FirstOrDefault();
                    if (country != null)
                    {
                        user.CountryId = model.CountryId;
                    }
                }

                if (!string.IsNullOrEmpty(model.Timezone))
                {
                    user.TimeZone = model.Timezone;
                }

                if (User.IsInRole("Worker") && Request.Form.Files.Any())
                {
                    var resume = Request.Form.Files["resume"];

                    if (resume != null)
                    {
                        var supportedTypes = new[] {
                        "DOC","DOCX","HTML","HTM","ODT","PDF","XLS","XLSX","ODS","PPT","PPTX","TXT","JPG","JPEG","GIF","PNG","BMP","TXT","RTF","ODP","ODS","TIFF", "MP3", "MP4",
                        "doc","docx","html","htm","odt","pdf","xls","xlsx","ods","ppt","pptx","txt","jpg","jpeg","gif","png","bmp","txt","rtf","odp","ods","tiff", "mp3", "mp4",
                        };
                        var fileExtension = System.IO.Path.GetExtension(resume?.FileName);
                        var fileExt = !string.IsNullOrEmpty(fileExtension) ? fileExtension.Substring(1) : string.Empty;

                        if (!supportedTypes.Contains(fileExt))
                        {
                            return BadRequest();
                        }

                        var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", $"Resume\\{UserId}");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        path += "\\";

                        if (!string.IsNullOrEmpty(user.UserResume))
                        {
                            if (System.IO.File.Exists(path + user.UserResume))
                            {
                                System.IO.File.Delete(path + user.UserResume);
                            }
                        }
                        var ext = Path.GetExtension(resume.FileName);
                        var fileName = $"{Guid.NewGuid()}--{UserId}{ext}";
                        if (System.IO.File.Exists(path + fileName))
                        {
                            System.IO.File.Delete(path + fileName);
                        }
                        using (var fileStream = new FileStream(path + fileName, FileMode.Create))
                        {
                            await resume.CopyToAsync(fileStream);
                        }
                        user.UserResume = $"{fileName}";
                    }
                }

                await _userManager.UpdateAsync(user);

                return Ok();
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/update",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("worker-balance")]
        public async Task<object> GetWorkerAccountBalance()
        {
            try
            {
                var result = await _workerService.GetAccountBalance(UserId);
                return Ok(result.amount);
            }
            catch (System.Exception ex)
            {
                var error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/worker-balance",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("company-balance")]
        public async Task<object> GetCompanyAccountBalance()
        {
            try
            {
                var result = await _clientService.GetAccountBalance(UserId);
                return Ok(result.amount);
            }
            catch (System.Exception ex)
            {
                var error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/company-balance",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("progress")]
        public async Task<object> ProgressPercentage()
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);
                var skills = _skillsService.GetUserSkillsCount(UserId);

                var completedPercentage = 0;
                if (user.UserType == (int)UserTypeEnum.Client)
                {
                    var checkCount = 0;
                    var totalCheckPoint = 4;

                    if (!string.IsNullOrEmpty(user.ProfilePicture))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.WhatWeDo))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.Description))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.Email))
                        checkCount += 1;
                    completedPercentage = checkCount * 100 / totalCheckPoint;
                }
                if (user.UserType == (int)UserTypeEnum.Worker)
                {
                    var checkCount = 0;
                    var totalCheckPoint = 10;

                    if (!string.IsNullOrEmpty(user.ProfilePicture))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.UserResume))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.UserTitle))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.Description))
                        checkCount += 1;
                    if (skills > 0)
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.UserSalary))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.UserAvailiblity))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.Education))
                        checkCount += 1;
                    if (!string.IsNullOrEmpty(user.Experience))
                        checkCount += 1;
                    // TODO : Job Check To Be Added After Jobs Come In

                    completedPercentage = checkCount * 100 / totalCheckPoint;
                }
                return Ok(completedPercentage);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/progress",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpDelete]
        [Authorize]
        [Route("worker/portifolio/delete/{id}")]
        public async Task<IActionResult> DeletePortifolio(int id)
        {
            LogErrorRequest error;
            try
            {
                var response = new ApiResponse<bool>();
                var del = await _workerService.DeletePortifolio(UserId, id);
                switch (del)
                {
                    case -1:
                        new ApiResponse<bool>
                        {
                            Response = false,
                            Success = false,
                            Message = "Error has been occured during delete skill."
                        };
                        break;
                    case 0:
                        new ApiResponse<bool>
                        {
                            Response = false,
                            Success = false,
                            Message = "Portifolio not found."
                        };
                        break;
                    default:
                        response = new ApiResponse<bool>
                        {
                            Response = true,
                            Success = true,
                            Message = "User profile is updated successfully."
                        };
                        break;
                }
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/worker/portifolio/delete/{id}",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpDelete]
        [Authorize]
        [Route("worker/youtube/delete/{id}")]
        public async Task<IActionResult> DeleteYoutube(int id)
        {
            LogErrorRequest error;
            try
            {
                var response = new ApiResponse<bool>();
                var del = await _workerService.DeleteYoutube(UserId, id);
                switch (del)
                {
                    case -1:
                        new ApiResponse<bool>
                        {
                            Response = false,
                            Success = false,
                            Message = "Error has been occurred during delete skill."
                        };
                        break;
                    case 0:
                        new ApiResponse<bool>
                        {
                            Response = false,
                            Success = false,
                            Message = "Youtube not found."
                        };
                        break;
                    default:
                        response = new ApiResponse<bool>
                        {
                            Response = true,
                            Success = true,
                            Message = "User profile is updated successfully."
                        };
                        break;
                }
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/worker/youtube/delete/{id}",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("complete-profile")]
        public async Task<IActionResult> CompeteProfile()
        {
            LogErrorRequest error;
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);

                if (user.UserType == 1)
                {
                    return Ok(await _clientService.GetProfileProgress(user.Id));
                }
                else
                {
                    return Ok(await _workerService.GetProfileProgress(user.Id));
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/complete-profile",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }

        }

        #region Local Functions 
        private async Task<object> GenerateJwtToken(string email, ApplicationUser user)
        {
            var userJson = JsonConvert.SerializeObject(user);

            var userRoles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.UserData, userJson)
            };

            if (userRoles.Any())
            {
                var roleClaims = userRoles.Select(x => new Claim(ClaimTypes.Role, x)).ToList();
                claims.AddRange(roleClaims);
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]));

            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtIssuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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

        private async Task<WorkerProfileResponse> MapUserToWrokerProfileResponse(ApplicationUser user)
        {
            var worker = new WorkerProfileResponse();

            worker.UserId = user.Id;
            worker.FullName = user.FullName;
            worker.Title = user.UserTitle;
            worker.Description = user.Description;
            worker.MemberSince = user.RegistrationDate ?? user.CreatedDate;
            worker.Availability = user.UserAvailiblity.ToAvailabilityType();
            worker.Education = user.Education;
            worker.Experience = user.Experience;
            worker.featured = user.featured;
            worker.rating = user.rating;
            worker.CountryId = user.CountryId;
            worker.CountryName = user.CountryId.HasValue ? user.CountryId.Value.ToCountryName() : string.Empty;
            worker.ProfilePicturePath = !string.IsNullOrEmpty(user.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{user.ProfilePicture}" : string.Empty; //$"{LookupService.FilePaths.WorkerDefaultImageFilePath}";
            worker.Timezone = user.TimeZone;
            worker.LastLoginTime = user.LastLoginTime;
            worker.Salary = user.UserSalary;
            worker.SocialMediaLinks = new SocialMediaLinksResponse()
            {
                Facebool = user.Facebook,
                Skype = user.Skype,
                LinkedIn = user.Linkedin
            };

            worker.Skills = await _workerService.GetWorkerSkills(user.Id);
            worker.Portfolios = await _workerService.GetPortfolios(user.Id);
            worker.Youtubes = await _workerService.GetYoutubes(user.Id);
            worker.IsHidden = user.IsHidden;

            var _context = new GoHireNowContext();
            var references = await _context.UserReferences.Where(u => u.UserId == user.Id && u.IsAccepted == 1 && u.IsDeleted == 0).ToListAsync();
            worker.ReferencesCount = references.Count();

            return worker;
        }

        private async Task<ClientProfileResponse> MapUserToClientProfileResponse(ApplicationUser user)
        {
            var client = new ClientProfileResponse();

            client.UserId = user.Id;
            client.CompanyName = user.Company;
            client.Introduction = user.Introduction;
            client.Timezone = user.TimeZone;
            client.Description = user.Description;
            client.MemberSince = user.RegistrationDate ?? user.CreatedDate;
            client.LastLoginTime = user.LastLoginTime;
            client.CountryId = user.CountryId;
            client.CountryName = user.CountryId.HasValue ? user.CountryId.Value.ToCountryName() : string.Empty;
            client.ProfilePicturePath = !string.IsNullOrEmpty(user.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{user.ProfilePicture}" : string.Empty; //$"{LookupService.FilePaths.ClientDefaultImageFilePath}";
            client.ActiveJobs = await _clientJobService.GetJobs(user.Id, 1, 5, (int)JobStatusEnum.Published, (int)UserTypeEnum.Client, true);

            return client;
        }

        public async Task NewMailService(int customId, int customType, string emailTo, string nameTo, string subject, string text, string emailFrom, string nameFrom, int priority)
        {
            try
            {
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
                    sender.ms_message = text;
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

        private void ValidateRegisterRequest(RegisterRequestModel model)
        {
            if (model.UserType == 1 && string.IsNullOrEmpty(model.CompanyName))
                ModelState.AddModelError("Company Name", "Please provide a company name");

            if (model.UserType == 2 && string.IsNullOrEmpty(model.FullName))
                ModelState.AddModelError("Full Name", "Please provide your Full Name");

            if (string.IsNullOrEmpty(model.CountryId.ToCountryName()))
                ModelState.AddModelError("Country", "Invalid country");
        }

        private async Task<IActionResult> LoginLocal(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, false, false);

            if (result.Succeeded)
            {
                var appUser = _userManager.Users.SingleOrDefault(r => r.Email == email);
                appUser.LastLoginTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(appUser);
                return Ok(await GenerateJwtToken(email, appUser));
            }
            else
            {
                return BadRequest("Email or Password is incorrect");
            }
        }
        #endregion
    }
}