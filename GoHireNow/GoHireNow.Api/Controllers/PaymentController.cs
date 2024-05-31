using GoHireNow.Api.Filters;
using GoHireNow.Database;
using GoHireNow.Identity.Data;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.ContractModels;
using GoHireNow.Models.MailModels;
using GoHireNow.Models.StripeModels;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GoHireNow.Service.CommonServices;
using PusherServer;
using GoHireNow.Models.ConfigurationModels;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Newtonsoft.Json;
using GoHireNow.Models.PayoutTransactionModels;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using GoHireNow.Database.GoHireNowTools;
using GoHireNow.Database.GoHireNowTools.Models;
using System.Security.Cryptography;
using GoHireNow.Api.ServicesConfiguration;
using System.Reflection;

namespace GoHireNow.Api.Controllers
{
    [Route("payment")]
    [ApiController]
    [CustomExceptionFilter]
    public class PaymentController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PusherSettings _pusherSettings;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IContractsSecuredService _contractsSecuredService;
        private readonly IUserSecurityCheckService _userSecurityCheckService;
        private readonly IContractService _contractService;
        private readonly IPlanService _planService;
        private readonly ICustomLogService _customLogService;
        private readonly IClientService _clientService;
        private readonly IWorkerService _workerService;
        private readonly IClientJobService _clientJobService;
        private readonly IMailService _mailService;
        private IConfiguration _configuration { get; }
        private Pusher pusher;
        public PaymentController(UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IStripePaymentService stripePaymentService,
            IOptions<PusherSettings> pusherSettings,
            IContractsSecuredService contractsSecuredService,
            IClientService clientService,
            IWorkerService workerService,
            IClientJobService clientJobService,
            IUserSecurityCheckService userSecurityCheckService,
            IPlanService planService,
            IMailService mailService,
            IContractService contractService,
            ICustomLogService customLogService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _stripePaymentService = stripePaymentService;
            _pusherSettings = pusherSettings.Value;
            _contractsSecuredService = contractsSecuredService;
            _userSecurityCheckService = userSecurityCheckService;
            _planService = planService;
            _mailService = mailService;
            _contractService = contractService;
            _customLogService = customLogService;
            _clientService = clientService;
            _workerService = workerService;
            _clientJobService = clientJobService;
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

        [HttpGet]
        [Route("getCards")]
        public async Task<StripeApiResponse> GetCards()
        {
            LogErrorRequest error;
            var response = new StripeApiResponse();
            var user = await _userManager.FindByIdAsync(UserId);
            try
            {
                if (user != null)
                {
                    var customerId = user.CustomerStripeId;

                    response.StatusCode = HttpStatusCode.OK;

                    if (string.IsNullOrEmpty(customerId))
                    {
                        return response;
                    }

                    CustomerService customerService = new CustomerService();
                    Customer cus = customerService.Get(user.CustomerStripeId);
                    DateTime newAccountCreated = new DateTime(2023, 5, 23);

                    if (cus.Created.Date < newAccountCreated.Date)
                    {
                        var newCustomer = customerService.Create(new CustomerCreateOptions
                        {
                            Name = user.Company,
                            Email = user.Email,
                        });
                        customerId = newCustomer.Id;
                        user.CustomerStripeId = newCustomer.Id;
                        await _userManager.UpdateAsync(user);

                        return response;
                    }

                    var service = new CardService();
                    var options = new CardListOptions
                    {
                        Limit = 5,
                    };

                    var cards = service.List(customerId, options);

                    var result = new List<object>();
                    if (cards != null && cards.Count() != 0)
                    {
                        foreach (var item in cards)
                        {
                            result.Add(item);
                        }
                        return new StripeApiResponse
                        {
                            StatusCode = HttpStatusCode.OK,
                            Result = result
                        };
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessage = ex.Message;

                if (user != null && ex.Message.Substring(0, 16) == "No such customer")
                {
                    CustomerService customerService = new CustomerService();

                    var newCustomer = customerService.Create(new CustomerCreateOptions
                    {
                        Name = user.Company,
                        Email = user.Email,
                    });

                    user.CustomerStripeId = newCustomer.Id;
                    await _userManager.UpdateAsync(user);

                    response.StatusCode = HttpStatusCode.OK;

                    return response;

                }

                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/getCards"
                };
                _customLogService.LogError(error);
                return response;
            }
        }

        [HttpPost]
        [Route("createCard")]
        public async Task<StripeApiResponse> CreateCard([FromForm] CreateCardRequest model)
        {
            LogErrorRequest error;
            var response = new StripeApiResponse();
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);
                var customerId = user.CustomerStripeId;

                if (string.IsNullOrEmpty(customerId))
                {
                    var customerOptions = new CustomerCreateOptions
                    {
                        Email = user.Email,
                        Name = user.Company
                    };
                    var customerService = new CustomerService();
                    var customer = customerService.Create(customerOptions);
                    user.CustomerStripeId = customer.Id;
                    await _userManager.UpdateAsync(user);
                    customerId = customer.Id;
                }

                var options = new CardCreateOptions
                {
                    Source = model.stripeToken
                };
                var service = new CardService();
                var card = service.Create(customerId, options);

                var paymentMethodOptions = new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions
                    {
                        Number = model.cardNumber,
                        ExpMonth = model.cardExpMonth,
                        ExpYear = model.cardExpYear,
                        Cvc = model.cardCVC
                    },
                };

                var paymentMethodService = new PaymentMethodService();
                var paymentMethod = paymentMethodService.Create(paymentMethodOptions);
                var attachOptions = new PaymentMethodAttachOptions
                {
                    Customer = customerId
                };
                paymentMethodService.Attach(paymentMethod.Id, attachOptions);

                await _stripePaymentService.CreateStripePayment(UserId, customerId, card.Id, paymentMethod.Id);

                var result = new List<object>();
                response.StatusCode = HttpStatusCode.OK;
                if (card != null)
                {
                    result.Add(card);
                    return new StripeApiResponse
                    {
                        StatusCode = HttpStatusCode.OK,
                        Result = result
                    };
                }

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessage = ex.Message;

                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/createCard"
                };
                _customLogService.LogError(error);
                return response;
            }
        }

        [HttpPost]
        [Route("deleteCard")]
        public async Task<StripeApiResponse> DeleteCard([FromForm] DeleteCardRequest model)
        {
            LogErrorRequest error;
            var response = new StripeApiResponse();
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);
                var customerId = user.CustomerStripeId;

                var stripePayment = await _stripePaymentService.GetStripePayment(customerId, model.cardId);
                var paymentMethodService = new PaymentMethodService();

                var detachOptions = new PaymentMethodDetachOptions { };
                paymentMethodService.Detach(stripePayment.PaymentMethodId, detachOptions);

                var isDeleted = await _stripePaymentService.DeleteStripePayment(customerId, model.cardId);

                response.StatusCode = HttpStatusCode.OK;
                var result = new List<object>();

                if (isDeleted)
                {
                    var service = new CardService();
                    var card = service.Delete(customerId, model.cardId);

                    if (card != null)
                    {
                        result.Add(card);
                        return new StripeApiResponse
                        {
                            StatusCode = HttpStatusCode.OK,
                            Result = result
                        };
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessage = ex.Message;

                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/deleteCard"
                };
                _customLogService.LogError(error);
                return response;
            }
        }

        [HttpPost]
        [Route("pockyt")]
        [Authorize]
        public async Task<IActionResult> PayByPockyt([FromBody] PockytPaymentRequest model)
        {
            LogErrorRequest error;
            try
            {
                var _context = new GoHireNowContext();
                var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == UserId);
                if (user == null)
                {
                    return Ok();
                }

                var merchantNo = _configuration.GetSection("Pockyt")["MerchantNo"];
                var storeNo = _configuration.GetSection("Pockyt")["StoreNo"];
                var hashAPIToken = _configuration.GetSection("Pockyt")["ApiToken"].CalculateMD5Hash();

                var httpClient = new HttpClient();
                var customerNo = "";

                var countryCode = user.CountryId.Value.ToCountryName().ToCountryCode();
                var email = user.Email;
                var cont = "countryCode=" + countryCode + "&email=" + email + "&firstName=" + model.FirstName + "&lastName=" + model.LastName + "&merchantNo=" + merchantNo + "&storeNo=" + storeNo + "&" + hashAPIToken;
                var verify = cont.CalculateMD5Hash();

                var createCustomerRequest = new PockytCreateCustomerRequest
                {
                    countryCode = countryCode,
                    email = email,
                    firstName = model.FirstName,
                    lastName = model.LastName,
                    merchantNo = merchantNo,
                    storeNo = storeNo,
                    verifySign = verify
                };
                var resp = await httpClient.PostAsJsonAsync(_configuration.GetSection("Pockyt")["BaseUrl"] + "/v1/customers/create", createCustomerRequest);
                var stringCont = await resp.Content.ReadAsStringAsync();
                PockytCreateCustomerResponse resu = JsonConvert.DeserializeObject<PockytCreateCustomerResponse>(stringCont);
                if (resu.ret_code == "000100")
                {
                    customerNo = resu.customer.customerNo;
                }
                else
                {
                    error = new LogErrorRequest()
                    {
                        ErrorMessage = stringCont,
                        UserId = UserId,
                        ErrorUrl = "/payment/pockyt-custom"
                    };
                    _customLogService.LogError(error);

                    return BadRequest("Failed to create customer");
                }

                var reference = GenerateUniqueString();
                var content = "amount=" + model.Amount + "&callbackUrl=" + _configuration.GetValue<string>("WebDomain") + "/balance?status={status}"
                    + "&creditType=vault" + "&currency=USD" + "&customerNo=" + customerNo + "&ipnUrl=" + _configuration.GetValue<string>("APIDomain") + "/payment/pockyt-webhook&merchantNo="
                    + merchantNo + "&reference=" + reference + "&settleCurrency=USD&storeNo=" + storeNo + "&terminal=ONLINE&vendor=" + model.PaymentMethod + "&" + hashAPIToken;
                var verifySign = content.CalculateMD5Hash();

                var securePayRequest = new PockytSecurePayRequest
                {
                    amount = model.Amount.ToString(),
                    creditType = "vault",
                    currency = "USD",
                    customerNo = customerNo,
                    merchantNo = merchantNo,
                    callbackUrl = _configuration.GetValue<string>("WebDomain") + "/balance?status={status}",
                    ipnUrl = _configuration.GetValue<string>("APIDomain") + "/payment/pockyt-webhook",
                    reference = reference,
                    settleCurrency = "USD",
                    storeNo = storeNo,
                    terminal = "ONLINE",
                    vendor = model.PaymentMethod,
                    verifySign = verifySign
                };
                var response = await httpClient.PostAsJsonAsync(_configuration.GetSection("Pockyt")["BaseUrl"] + "/online/v3/secure-pay", securePayRequest);
                var stringContent = await response.Content.ReadAsStringAsync();
                PockytSecurePayResponse result = JsonConvert.DeserializeObject<PockytSecurePayResponse>(stringContent);

                if (result.ret_code == "000100")
                {
                    return Ok(result.result);
                }
                else
                {
                    error = new LogErrorRequest()
                    {
                        ErrorMessage = stringContent,
                        UserId = UserId,
                        ErrorUrl = "/payment/pockyt-secure"
                    };
                    _customLogService.LogError(error);

                    return BadRequest("Failed to proceed Secure pay");
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/pockyt"
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Route("charge")]
        public async Task<StripeApiResponse> Charge([FromForm] PaymentChargeRequest model)
        {
            LogErrorRequest error;
            var response = new StripeApiResponse();
            try
            {
                var user = await _userManager.FindByIdAsync(UserId);

                if (user == null)
                {
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }

                var planId = model.planId?.Split(',');

                // Cancel Subscription
                var freeClientPlan = await _stripePaymentService.GetGlobalPlanDetail((int)GlobalPlanEnum.Free);
                var freeWorkerPlan = await _stripePaymentService.GetGlobalPlanDetail((int)GlobalPlanEnum.FreeForWorker);
                if (planId?.Count() == 1 && (freeClientPlan.AccessId == planId[0] || freeWorkerPlan.AccessId == planId[0]))
                {
                    if (!string.IsNullOrEmpty(user.CustomerStripeId))
                    {
                        CancelSubscription(user.CustomerStripeId);
                    }
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                // end

                var customers = new CustomerService();
                var subscriptions = new SubscriptionService();
                var customerId = user.CustomerStripeId;
                var cardId = "";

                if (model.byCard)
                {
                    if (string.IsNullOrEmpty(user.CustomerStripeId))        // If user has not his own customerId, create new customerId for the user with default payment method of selected card
                    {
                        var paymentMethodId = "";
                        var service = new PaymentMethodService();

                        var customer = customers.Create(new CustomerCreateOptions
                        {
                            Name = user.Company,
                            Email = user.Email,
                            Source = model.stripeToken
                        });
                        customerId = customer.Id;
                        user.CustomerStripeId = customer.Id;
                        await _userManager.UpdateAsync(user);

                        var paymentMethodOptions = new PaymentMethodCreateOptions
                        {
                            Type = "card",
                            Card = new PaymentMethodCardOptions
                            {
                                Number = model.cardNumber,
                                ExpMonth = model.cardExpMonth,
                                ExpYear = model.cardExpYear,
                                Cvc = model.cardCVC
                            },
                        };

                        var paymentMethodService = new PaymentMethodService();
                        var paymentMethod = paymentMethodService.Create(paymentMethodOptions);

                        await _stripePaymentService.CreateStripePayment(UserId, customerId, model.cardId, paymentMethod.Id);
                        cardId = model.cardId;

                        paymentMethodId = paymentMethod.Id;

                        var attachOptions = new PaymentMethodAttachOptions
                        {
                            Customer = customerId
                        };
                        service.Attach(paymentMethodId, attachOptions);

                        customers.Update(customerId, new CustomerUpdateOptions
                        {
                            InvoiceSettings = new CustomerInvoiceSettingsOptions
                            {
                                DefaultPaymentMethod = paymentMethodId
                            }
                        });
                    }
                    else                                                    // Attach card information as payment method of the customer
                    {
                        var paymentMethodId = "";
                        var service = new PaymentMethodService();

                        if (!model.isNewCard)
                        {
                            var stripePayment = await _stripePaymentService.GetStripePayment(customerId, model.cardId);
                            paymentMethodId = stripePayment.PaymentMethodId;
                            cardId = stripePayment.CardId;
                        }
                        else
                        {
                            var options = new CardCreateOptions
                            {
                                Source = model.stripeToken
                            };
                            var cardService = new CardService();
                            var card = cardService.Create(customerId, options);
                            cardId = card.Id;

                            var paymentMethodOptions = new PaymentMethodCreateOptions
                            {
                                Type = "card",
                                Card = new PaymentMethodCardOptions
                                {
                                    Number = model.cardNumber,
                                    ExpMonth = model.cardExpMonth,
                                    ExpYear = model.cardExpYear,
                                    Cvc = model.cardCVC
                                },
                            };

                            var paymentMethodService = new PaymentMethodService();
                            var paymentMethod = paymentMethodService.Create(paymentMethodOptions);

                            await _stripePaymentService.CreateStripePayment(UserId, customerId, card.Id, paymentMethod.Id);

                            paymentMethodId = paymentMethod.Id;
                        }

                        var attachOptions = new PaymentMethodAttachOptions
                        {
                            Customer = customerId
                        };
                        service.Attach(paymentMethodId, attachOptions);

                        var customer = customers.Update(customerId, new CustomerUpdateOptions
                        {
                            InvoiceSettings = new CustomerInvoiceSettingsOptions
                            {
                                DefaultPaymentMethod = paymentMethodId
                            }
                        });
                    }
                }

                var isRenewable = false;

                // Charge for Security Deposit plan
                if (model.customType == "3" || model.customType == "4" || model.customType == "5")
                {
                    if (!model.byCard)
                    {
                        if (model.customType == "3")
                        {
                            await SecurityDepositPayProcess(model.amount, UserId, Int32.Parse(model.customData), true, true);
                        }
                        else if (model.customType == "4")
                        {
                            await HRPremiumPayProcess(model.amount, UserId, model.customData, true);
                        }
                        else if (model.customType == "5")
                        {
                            await BonusPayProcess(model.amount, UserId, Int32.Parse(model.customData), true);
                        }
                    }
                    else
                    {
                        var _context = new GoHireNowContext();
                        var contractPlan = model.customType == "3" ?
                            _context.GlobalPlans.Where(item => item.BillingName == "Security Deposit For Your Contract").FirstOrDefault() : model.customType == "4" ?
                            _context.GlobalPlans.Where(item => item.BillingName == "Salary HR").FirstOrDefault() : _context.GlobalPlans.Where(item => item.BillingName == "Contract Bonus").FirstOrDefault();

                        var chargeOptions = new ChargeCreateOptions
                        {
                            Amount = model.amount > model.balance ? (int)(Decimal.Divide((model.amount - model.balance) * (100 + contractPlan.ProcessingFeeRate), 100)) : (int)(Decimal.Divide(model.amount * (100 + contractPlan.ProcessingFeeRate), 100)),
                            Currency = "usd",
                            Customer = customerId,
                            StatementDescriptor = contractPlan.Name,
                            Source = cardId,
                            Metadata = new Dictionary<string, string>
                            {
                                {"customType", model.customType},
                                {"customData", model.customData},
                                {"chargeType", "deposit"},
                                {"chargeAmount", (model.amount > model.balance ? model.amount - model.balance : model.amount).ToString()},
                                {"userId", UserId},
                                {"amountByBalance", (model.amount > model.balance ? model.balance : 0).ToString()}
                            }
                        };
                        var chargeService = new ChargeService();
                        var rs = chargeService.Create(chargeOptions);
                    }

                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                // end

                foreach (var item in planId)
                {
                    GlobalPlans plan = _planService.GetAllPlans().Where(p => p.AccessId == item).FirstOrDefault();
                    if (plan.Renewable)
                    {
                        isRenewable = true;
                        break;
                    }
                }

                if (isRenewable)                                        // Charge for Subscription
                {
                    var plan = new PlanService().Get(planId[0]);
                    var rs = CreateCustomerSubscription(subscriptions, customerId, plan);
                    if (rs.Status == "active")
                    {
                        response.StatusCode = HttpStatusCode.OK;
                        return response;
                    }
                }
                else                                                    // Charge for other product plans
                {
                    GlobalPlans globalPlan = _planService.GetAllPlans().Where(p => p.AccessId == planId[0]).FirstOrDefault();

                    // Create an Invoice
                    var invoiceOptions = new InvoiceCreateOptions
                    {
                        Customer = customerId,
                        StatementDescriptor = globalPlan.BillingName.Length > 21 ? globalPlan.BillingName.Substring(0, 21) : globalPlan.BillingName,
                        Metadata = new Dictionary<string, string>
                        {
                            {"customType", model.customType},
                            {"customData", model.customData},
                            {"amount", model.amount.ToString()},
                            {"byCard", model.byCard.ToString()},
                            {"paymentType", model.paymentType.ToString()}
                        }
                    };
                    var invoiceService = new InvoiceService();
                    var invoice = invoiceService.Create(invoiceOptions);

                    // Create an Invoice Item with the Price, and Customer you want to charge
                    foreach (var item in planId)
                    {
                        GlobalPlans plan = _planService.GetAllPlans().FirstOrDefault(p => p.AccessId == item);

                        var invoiceItemOptions = new InvoiceItemCreateOptions
                        {
                            Customer = customerId,
                            Price = item,
                            Invoice = invoice.Id,
                            Description = plan?.BillingName,
                        };

                        if (!model.byCard)
                        {
                            invoiceItemOptions.Discounts = new List<InvoiceItemDiscountOptions>
                            {
                                new InvoiceItemDiscountOptions
                                {
                                    Coupon = _configuration.GetValue<string>("CouponKey")
                                }
                            };
                        }

                        var invoiceItemService = new InvoiceItemService();
                        var invoiceItem = invoiceItemService.Create(invoiceItemOptions);
                    }

                    invoiceService.Pay(invoice.Id);

                    if (invoice.Status == "draft")
                    {
                        await _userManager.UpdateAsync(user);
                        response.StatusCode = HttpStatusCode.OK;
                        return response;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessage = ex.Message;

                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/charge"
                };
                _customLogService.LogError(error);
                return response;
            }
        }

        [HttpGet]
        [Route("endcontract/{contractid}")]
        [Authorize]
        public async Task<IActionResult> EndContract(int contractid)
        {
            LogErrorRequest error;
            var response = new StripeApiResponse();
            try
            {
                var ip = GetPublicIpAddress();
                var res = await _contractService.EndContract(UserId, contractid, ip);

                using (var _context = new GoHireNowContext())
                {
                    var contract = await _context.Contracts.Where(x => x.Id == contractid).FirstOrDefaultAsync();
                    if (contract != null)
                    {
                        ContractNotificationSend(contract.UserId, contract.CompanyId);
                    }
                }

                return Ok(res);
            }
            catch (System.Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessage = ex.Message;

                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/endcontract/" + contractid
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("cancelsubscription")]
        public void CancelSubscription(string customerId)
        {
            LogErrorRequest error;
            try
            {
                var service = new SubscriptionService();
                var previousList = service.List(new SubscriptionListOptions
                {
                    Customer = customerId
                });
                if (previousList.Count() > 0)
                {
                    var options = new SubscriptionUpdateOptions
                    {
                        CancelAtPeriodEnd = true
                    };
                    service.Update(previousList.FirstOrDefault().Id, options);
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/cancelsubscription"
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("pockyt-webhook")]
        public async Task<IActionResult> PockytWebhook()
        {
            try
            {
                var json = new StreamReader(HttpContext.Request.Body).ReadToEnd();
                PockytWebhookResponse result = JsonConvert.DeserializeObject<PockytWebhookResponse>(json);
                var resultString = GetQueryString<PockytWebhookResponse>(result) + _configuration.GetSection("Pockyt")["ApiToken"].CalculateMD5Hash();

                if (resultString.CalculateMD5Hash() == result.verifySign)
                {
                    var newInformation = new PockytPaymentInformation()
                    {
                        UserId =
                    };
                    // var timestamp = DateTime.UtcNow;
                    // var content = "merchantNo=" + merchantNo + "&storeNo=" + storeNo + "&timestamp=" + timestamp.ToString() + "&transactionNo=" + result.transactionNo + "&" + hashAPIToken;
                    // var verifySign = content.CalculateMD5Hash();

                    // var processRequest = new PockytProcessRequest
                    // {
                    //     merchantNo = merchantNo,
                    //     storeNo = storeNo,
                    //     timestamp = timestamp.ToString(),
                    //     transactionNo = result.transactionNo,
                    //     verifySign = verifySign
                    // };
                    // var response = await httpClient.PostAsJsonAsync(_configuration.GetSection("Pockyt")["BaseUrl"] + "/creditpay/v3/process", processRequest);
                    // var stringContent = await response.Content.ReadAsStringAsync();
                    // PockytSecurePayResponse result = JsonConvert.DeserializeObject<PockytSecurePayResponse>(stringContent);

                    // if (result.ret_code == "000100")
                    // {
                    //     return Ok(result.result);
                    // }
                    // else
                    // {
                    //     error = new LogErrorRequest()
                    //     {
                    //         ErrorMessage = stringContent,
                    //         UserId = UserId,
                    //         ErrorUrl = "/payment/pockyt-secure"
                    //     };
                    //     _customLogService.LogError(error);

                    //     return BadRequest("Failed to proceed Secure pay");
                    // }

                    LogErrorRequest error;
                    error = new LogErrorRequest()
                    {
                        ErrorMessage = json.ToString(),
                        ErrorUrl = "/payment/pockyt-webhook"
                    };
                    _customLogService.LogError(error);


                    return Content("success", "text/plain");
                }

                return Ok();
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/pockyt-webhook"
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Route("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            LogErrorRequest error;
            try
            {
                var json = new StreamReader(HttpContext.Request.Body).ReadToEnd();

                // validate webhook called by stripe only
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], _configuration.GetValue<string>("WebHookTrialSignature"));

                if (stripeEvent.Type == "invoice.payment_failed")
                {
                    var subscriptions = new SubscriptionService();
                    var invoice = stripeEvent.Data.Object as Invoice;
                    if (invoice.Lines.FirstOrDefault().Plan != null)
                    {
                        var previousList = subscriptions.List(new SubscriptionListOptions
                        {
                            Customer = invoice.CustomerId
                        });
                        if (previousList.Count() > 0)
                        {
                            var options = new SubscriptionUpdateOptions
                            {
                                CancelAtPeriodEnd = false
                            };
                            subscriptions.Update(previousList.FirstOrDefault().Id, options);
                        }
                        foreach (var item in previousList)
                        {
                            var cancelOptions = new SubscriptionCancelOptions
                            {
                                InvoiceNow = false,
                                Prorate = false,
                            };
                            subscriptions.Cancel(item.Id, cancelOptions);
                        }
                        var userId = _userManager.Users.Where(u => u.CustomerStripeId == invoice.CustomerId).FirstOrDefault()?.Id;
                        if (userId != null)
                        {
                            await _clientService.UpdateToFreePlan(userId);
                            await _clientService.ProcessCurrentPricingPlan(userId);
                        }
                    }
                }
                if (stripeEvent.Type == "invoice.payment_succeeded")
                {
                    var subService = new SubscriptionService();
                    var invoice = stripeEvent.Data.Object as Invoice;

                    // check if subscription or not
                    if (invoice.Lines.FirstOrDefault().Plan == null)    // Other product
                    {
                        if (!string.IsNullOrEmpty(invoice.ChargeId))
                        {
                            var charge = new ChargeService().Get(invoice.ChargeId);
                            await CreateOtherTransactionInDatabase(invoice, charge);
                        }
                        else
                        {
                            await CreateOtherTransactionInDatabase(invoice);
                        }
                    }
                    else                                                // Subscription
                    {
                        var plan = invoice.Lines.FirstOrDefault().Plan;
                        var charge = new ChargeService().Get(invoice.ChargeId);
                        await CreatePlanTransactionInDatabase(invoice, charge, plan);
                    }
                }
                if (stripeEvent.Type == "customer.subscription.deleted")
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    var userId = _userManager.Users.Where(u => u.CustomerStripeId == subscription.CustomerId).FirstOrDefault()?.Id;
                    if (subscription.CancelAtPeriodEnd == true)
                    {
                        await _clientService.UpdateToFreePlan(userId);
                        await _clientService.ProcessCurrentPricingPlan(userId);
                    }
                }
                if (stripeEvent.Type == "customer.subscription.updated")
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    var userId = _userManager.Users.Where(u => u.CustomerStripeId == subscription.CustomerId).FirstOrDefault()?.Id;
                    if (userId != null)
                    {
                        if (subscription.CancelAtPeriodEnd == true)
                        {
                            await _clientService.UpdateToFreePlan(userId);
                            await _clientService.ProcessCurrentPricingPlan(userId);
                        }
                        return Ok("Canceled!");
                    }
                    else
                    {
                        return Ok("ID not found!");
                    }
                }
                if (stripeEvent.Type == "charge.succeeded")
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    if (!string.IsNullOrEmpty(charge.Id) && !string.IsNullOrEmpty(charge.InvoiceId))
                    {
                        return Ok("Invoice payment");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(charge.Id) &&
                            charge.Metadata.ContainsKey("customType") && charge.Metadata["customType"] == "3" &&
                            charge.Metadata.ContainsKey("chargeType") && charge.Metadata["chargeType"] == "deposit")
                        {
                            using (var _context = new GoHireNowContext())
                            {
                                if (charge.Metadata.ContainsKey("actionType") && charge.Metadata["actionType"] == "SundayDeposit" && charge.Metadata.ContainsKey("actionId"))
                                {
                                    var action = await _context.Actions.Where(x => x.Id == Int32.Parse(charge.Metadata["actionId"])).FirstOrDefaultAsync();
                                    if (action != null)
                                    {
                                        action.Processed = 1;
                                        await _context.SaveChangesAsync();
                                    }
                                }

                                await SecurityDepositPayProcess((int)charge.Amount, charge.Metadata["userId"], Int32.Parse(charge.Metadata["customData"]), false, true, charge);
                            }

                            return Ok("Contract payment");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(charge.Id) &&
                                charge.Metadata.ContainsKey("customType") && charge.Metadata["customType"] == "4" &&
                                charge.Metadata.ContainsKey("chargeType"))
                            {
                                await HRPremiumPayProcess((int)charge.Amount, charge.Metadata["userId"], charge.Metadata["customData"], false, charge);

                                return Ok("Contract payment");
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(charge.Id) &&
                                    charge.Metadata.ContainsKey("customType") && charge.Metadata["customType"] == "5" &&
                                    charge.Metadata.ContainsKey("chargeType"))
                                {
                                    await BonusPayProcess((int)charge.Amount, charge.Metadata["userId"], Int32.Parse(charge.Metadata["customData"]), false, charge);

                                    return Ok("Contract payment");
                                }
                            }
                        }
                    }
                }
                if (stripeEvent.Type == "charge.failed")
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    if (!string.IsNullOrEmpty(charge.Id) && !string.IsNullOrEmpty(charge.InvoiceId))
                    {
                        return Ok("Invoice payment failed");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(charge.Id) &&
                            charge.Metadata.ContainsKey("customType") && charge.Metadata["customType"] == "3" &&
                            charge.Metadata.ContainsKey("chargeType") && charge.Metadata["chargeType"] == "deposit" &&
                            charge.Metadata.ContainsKey("actionType") && charge.Metadata["actionType"] == "SundayDeposit")
                        {
                            await SecurityDepositPayProcess(0, charge.Metadata["userId"], Int32.Parse(charge.Metadata["customData"]), true, false, charge);
                            return Ok("Deposit Failed");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(charge.Id) &&
                                charge.Metadata.ContainsKey("customType") && charge.Metadata["customType"] == "4" &&
                                charge.Metadata.ContainsKey("chargeType") && charge.Metadata["chargeType"] == "hrautodeposit" &&
                                charge.Metadata.ContainsKey("userId"))
                            {
                                var request = new LogSupportRequest()
                                {
                                    Text = charge.Metadata["userId"] + " - failed - $" + Decimal.Divide(charge.Amount, 100)
                                };
                                _customLogService.LogHRSupport(request);

                            }
                        }
                    }
                }
                return Ok();
            }
            catch (StripeException ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/webhook"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/webhook"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("wise-webhook")]
        public async Task<IActionResult> WiseWebhook()
        {
            LogErrorRequest error;
            try
            {
                string requestBody = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                WiseWebhookResponse response = JsonConvert.DeserializeObject<WiseWebhookResponse>(requestBody);

                using (var _context = new GoHireNowContext())
                {
                    var log = new PayoutTransactionsLog();
                    if (response.event_type == "transfers#payout-failure")
                    {
                        log.event_id = response.data.transfer_id;
                        log.profile_id = response.data.profile_id;
                        log.account_id = 0;
                        log.type = "Event_Type: " + response.event_type;
                        log.state = response.data.failure_reason_code + "  " + response.data.failure_description;
                        log.eventDate = response.occured_at;
                        log.createddate = DateTime.UtcNow;
                    }
                    else
                    {
                        log.event_id = response.data.resource.id;
                        log.profile_id = response.data.resource.profile_id;
                        log.account_id = response.data.resource.account_id;
                        log.type = "Event_Type: " + response.event_type + " Resource_type: " + response.data.resource.type;
                        log.state = response.data.current_state;
                        log.eventDate = response.sent_at;
                        log.createddate = DateTime.UtcNow;
                    }

                    await _context.PayoutTransactionsLog.AddAsync(log);
                    await _context.SaveChangesAsync();
                }

                return Ok("WISE-WEBHOOK");
            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message,
                    ErrorUrl = "/payment/wise-webhook"
                };

                if (ex.InnerException != null)
                {
                    error.ErrorMessage += " InnerException: " + ex.InnerException.Message;
                }

                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("transactions")]
        public async Task<IActionResult> Transactions()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _stripePaymentService.GetAllTransactions(UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/transactions"
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("transactions/{jobid}")]
        public async Task<IActionResult> TransactionDetails(int jobid)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _stripePaymentService.GetTransactionDetails(jobid));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/transactions/" + jobid
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("payoutRecipient/{userId}")]
        public async Task<IActionResult> GetPayoutRecipient(string userId)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.GetPayoutRecipient(userId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/payoutRecipient/" + userId,
                    UserId = UserId,
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("deletePayoutTransaction/{transactionId}")]
        public async Task<IActionResult> DeletePayoutTransaction(int transactionId)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _contractService.DeletePayoutTransaction(transactionId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/deletePayoutTransaction/" + transactionId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("addRecipient")]
        public async Task<IActionResult> AddPayoutRecipient([FromBody] AddRecipientModel model)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var recipientsToUpdate = _context.PayoutRecipients.Where(x => x.userId == UserId && x.isdeleted == 0 && x.statusId > 0).ToList();
                    foreach (var rec in recipientsToUpdate)
                    {
                        rec.statusId = 0;
                        rec.isdeleted = 1;
                    }

                    var recipient = new PayoutRecipients();
                    recipient.userId = UserId;
                    recipient.autodeposit = 1;
                    recipient.CreatedDate = DateTime.UtcNow;
                    recipient.statusId = model.statusId;
                    recipient.currency = model.currency;
                    recipient.ispersonal = model.ispersonal;
                    recipient.email = model.email;
                    recipient.name = model.accountHolderName;
                    recipient.country = model.country;
                    recipient.WiseCustomerId = model.WiseCustomerId;
                    recipient.isdeleted = 0;

                    await _context.PayoutRecipients.AddAsync(recipient);
                    await _context.SaveChangesAsync();

                    var worker = await _context.AspNetUsers.Where(x => x.Id == UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (worker != null)
                    {
                        string headtitle = "";
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/balance";
                        string buttoncaption = "View Balance";
                        string description = "";
                        var subject = "You added a new bank account";
                        var message = "Your bank account has been added successfully.";
                        await _contractService.NewMailService(0, 28, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }

                    return Ok(true);
                }

                return Ok(false);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    UserId = UserId,
                    ErrorUrl = "/payment/addRecipient"
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("newquote")]
        public async Task<IActionResult> NewQuote([FromBody] QuoteModel model)
        {
            try
            {
                var httpClient = new HttpClient();
                var quoteResponse = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newquote", model);
                string quoteString = await quoteResponse.Content.ReadAsStringAsync();
                Quote quote = JsonConvert.DeserializeObject<Quote>(quoteString);
                return Ok(quote);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/NewQuote"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("refreshAccountRequirements")]
        public async Task<IActionResult> RefreshAccountRequirements([FromBody] RefreshRecipientData model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/refreshAccountRequirements", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                List<AccountRequirements> result = JsonConvert.DeserializeObject<List<AccountRequirements>>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/RefreshAccountRequirements"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newrecipient/usd")]
        public async Task<IActionResult> CreateNewRecipientUSDAsync([FromBody] NewRecipientDataUSD model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newrecipient/usd", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                RecipientData result = JsonConvert.DeserializeObject<RecipientData>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/CreateNewRecipientUSDAsync"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newrecipient/php")]
        public async Task<IActionResult> CreateNewRecipientPHPAsync([FromBody] NewRecipientDataPHP model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newrecipient/php", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                RecipientData result = JsonConvert.DeserializeObject<RecipientData>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/CreateNewRecipientPHPAsync"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newrecipient/eur")]
        public async Task<IActionResult> CreateNewRecipientEURAsync([FromBody] NewRecipientDataEUR model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newrecipient/eur", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                RecipientData result = JsonConvert.DeserializeObject<RecipientData>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/CreateNewRecipientEURAsync"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newrecipient/cad")]
        public async Task<IActionResult> CreateNewRecipientCADAsync([FromBody] NewRecipientDataCAD model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newrecipient/cad", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                RecipientData result = JsonConvert.DeserializeObject<RecipientData>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/CreateNewRecipientCADAsync"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newrecipient/gbp")]
        public async Task<IActionResult> CreateNewRecipientGBPAsync([FromBody] NewRecipientDataGBP model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newrecipient/gbp", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                RecipientData result = JsonConvert.DeserializeObject<RecipientData>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/CreateNewRecipientGBPAsync"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newrecipient/aud")]
        public async Task<IActionResult> CreateNewRecipientAUDAsync([FromBody] NewRecipientDataAUD model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newrecipient/aud", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                RecipientData result = JsonConvert.DeserializeObject<RecipientData>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/CreateNewRecipientAUDAsync"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newrecipient/inr")]
        public async Task<IActionResult> CreateNewRecipientINRAsync([FromBody] NewRecipientDataINR model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newrecipient/inr", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                RecipientData result = JsonConvert.DeserializeObject<RecipientData>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/CreateNewRecipientINRAsync"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newrecipient/kes")]
        public async Task<IActionResult> CreateNewRecipientKESAsync([FromBody] NewRecipientDataKES model)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newrecipient/kes", model);
                string stringContent = await response.Content.ReadAsStringAsync();
                RecipientData result = JsonConvert.DeserializeObject<RecipientData>(stringContent);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/CreateNewRecipientKESAsync"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("newpayout/{amount}")]
        [Authorize]
        public async Task<IActionResult> CreateNewPayoutAsync([FromBody] CreatePayoutTransactionModel model, decimal amount)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == UserId && !u.IsDeleted);
                    if (user != null)
                    {
                        if (user.UserType == 1)
                        {
                            var balance = await _clientService.GetAccountBalance(UserId);
                            if (balance.amount < amount)
                            {
                                return BadRequest();
                            }
                        }
                        else
                        {
                            var balance = await _workerService.GetAccountBalance(UserId);
                            if (balance.amount < amount)
                            {
                                return BadRequest();
                            }
                        }
                    }

                    var ptId = await _contractService.CreatePayoutTransaction(model, UserId);

                    var newAction = new Actions()
                    {
                        UserId = UserId,
                        Type = 3,
                        Processed = 0,
                        IsApproved = 0,
                        CustomAmount = amount,
                        CustomContractId = 0,
                        CustomPayoutId = ptId,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _context.Actions.AddAsync(newAction);
                    await _context.SaveChangesAsync();

                    return Ok(newAction.Id);
                }
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/newpayout",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("complete/{id}")]
        public async Task<IActionResult> CreateNewCompleteAsync([FromBody] CompleteInfo model, string id)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/complete/" + id, model);
                string stringContent = await response.Content.ReadAsStringAsync();
                CompleteResponse result = JsonConvert.DeserializeObject<CompleteResponse>(stringContent);

                using (var _context = new GoHireNowContext())
                {
                    var worker = await _context.AspNetUsers.Where(x => x.Id == UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (result != null && result.status == "COMPLETED" && worker != null)
                    {
                        string subject = "We sent you a payment";
                        string headtitle = "Payment Sent";
                        string text = "A payment is on its way to your bank account, it should be deposited within the next 48 hours.";
                        string description = "";
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/balance";
                        string buttoncaption = "View Balance";
                        await _contractService.NewMailService(0, 31, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, text, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }
                }

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/complete/" + id,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("account-requirements/{quoteId}")]
        public async Task<IActionResult> GetAccountRequirements(string quoteId)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/account-requirements/" + quoteId);
                string stringContent = await response.Content.ReadAsStringAsync();
                List<AccountRequirements> result = JsonConvert.DeserializeObject<List<AccountRequirements>>(stringContent);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/account-requirements/" + quoteId
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("sendCode")]
        public async Task<IActionResult> SendVerificationCode(SMSRequest model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var user = await _context.AspNetUsers.Where(x => x.PhoneNumber == model.phoneNumber && x.Id != UserId && x.SmsFactorEnabled == 1).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        return Ok("This number is already taken.");
                    }
                }

                TwilioClient.Init(_configuration.GetSection("TwilioSettings")["AccountSid"], _configuration.GetSection("TwilioSettings")["AuthToken"]);

                var to = new PhoneNumber(model.phoneNumber);
                var from = new PhoneNumber(_configuration.GetSection("TwilioSettings")["PhoneNumber"]);
                int code = new Random().Next(1000, 10000);
                string message = code.ToString() + " is your EVA verification code. Do not share this code with anyone.";
                MessageResource sentMessage = MessageResource.Create(to: to, from: from, body: message);

                if (sentMessage.Status == MessageResource.StatusEnum.Failed)
                {
                    return Ok(sentMessage.ErrorMessage);
                }
                else
                {
                    using (var _context = new GoHireNowContext())
                    {
                        var sms = new UserSMS();
                        sms.UserId = UserId;
                        sms.Area = model.country;
                        sms.Number = model.phoneNumber;
                        sms.TempCode = code;
                        sms.IsDeleted = 0;
                        sms.CreatedDate = DateTime.UtcNow;

                        await _context.UserSMS.AddAsync(sms);
                        await _context.SaveChangesAsync();
                    }

                    return Ok("Success");
                }
            }
            catch (ApiException ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message,
                    ErrorUrl = "/payment/sendCode/" + model.phoneNumber,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                return Ok(ex.Message);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/sendCode",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("verifyCode/{code}/{phoneNumber}")]
        public async Task<IActionResult> VerifyCode(int code, string phoneNumber)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var sms = await _context.UserSMS.Where(x => x.UserId == UserId && x.Number == phoneNumber && x.LastVerifiedDate == null).LastAsync();
                    if (sms.TempCode == code)
                    {
                        sms.LastVerifiedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        return Ok("Success");
                    }
                    else
                    {
                        return Ok("Failed");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/verifyCode/" + code,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet]
        [Route("deleteRecipient/{accountId}")]
        public async Task<IActionResult> DeleteRecipient(string accountId)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/deleteRecipient/" + accountId);
                return Ok();
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/deleteRecipient" + accountId
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        #region Local Functions
        public async Task<IActionResult> PaymentAction()
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var actions = await _context.Actions.Where(x => x.Type == 3 ? x.Processed == 0 && x.IsApproved == 1 : x.Processed == 0).ToListAsync();
                    var contractPlan = await _context.GlobalPlans.Where(p => p.IsActive == true && p.BillingName == "Security Deposit For Your Contract").FirstOrDefaultAsync();
                    var httpClient = new HttpClient();

                    foreach (var action in actions)
                    {
                        action.Processed = 2;
                        action.RunnedDate = DateTime.Now;
                        await _context.SaveChangesAsync();

                        if (action.Type == 1)
                        {
                            var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == action.UserId & !u.IsDeleted);
                            var contract = await _context.Contracts.Where(x => x.Id == action.CustomContractId && x.IsDeleted == 0).FirstOrDefaultAsync();
                            var balance = await _clientService.GetAccountBalance(user.Id);
                            if (contract != null && user != null && balance != null)
                            {
                                var customerId = user.CustomerStripeId;
                                var stripePayment = await _context.StripePayments
                                    .Where(s => s.CustomerId == customerId && !s.IsDeleted)
                                    .OrderByDescending(s => s.Id)
                                    .FirstOrDefaultAsync();

                                if (!string.IsNullOrEmpty(customerId))
                                {
                                    if (balance.amount >= contract.Hours * contract.Rate)
                                    {
                                        await SecurityDepositPayProcess((int)(contract.Hours * contract.Rate * 100), action.UserId, action.CustomContractId, true, true);

                                        action.Processed = 1;
                                        await _context.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        if (stripePayment == null)
                                        {
                                            var chargeOptions = new ChargeCreateOptions
                                            {
                                                Amount = (int)((contract.Hours * contract.Rate - balance.amount) * (100 + contractPlan.ProcessingFeeRate)),
                                                Currency = "usd",
                                                Customer = customerId,
                                                Metadata = new Dictionary<string, string>
                                                {
                                                    {"customType", "3"},
                                                    {"customData", action.CustomContractId.ToString()},
                                                    {"chargeType", "deposit"},
                                                    {"chargeAmount", ((int)((contract.Hours * contract.Rate - balance.amount) * 100)).ToString()},
                                                    {"userId", action.UserId},
                                                    {"actionType", "SundayDeposit"},
                                                    {"actionId", action.Id.ToString()},
                                                    {"amountByBalance", ((int)(balance.amount * 100)).ToString()}
                                                }
                                            };
                                            var chargeService = new ChargeService();
                                            var rs = chargeService.Create(chargeOptions);
                                        }
                                        else
                                        {
                                            var chargeOptions = new ChargeCreateOptions
                                            {
                                                Amount = (int)((contract.Hours * contract.Rate - balance.amount) * (100 + contractPlan.ProcessingFeeRate)),
                                                Currency = "usd",
                                                Customer = customerId,
                                                Source = stripePayment.CardId,
                                                Metadata = new Dictionary<string, string>
                                                {
                                                    {"customType", "3"},
                                                    {"customData", action.CustomContractId.ToString()},
                                                    {"chargeType", "deposit"},
                                                    {"chargeAmount", ((int)((contract.Hours * contract.Rate - balance.amount) * 100)).ToString()},
                                                    {"userId", action.UserId},
                                                    {"actionType", "SundayDeposit"},
                                                    {"actionId", action.Id.ToString()},
                                                    {"amountByBalance", ((int)(balance.amount * 100)).ToString()}
                                                }
                                            };
                                            var chargeService = new ChargeService();
                                            var rs = chargeService.Create(chargeOptions);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (action.Type == 2)
                            {
                                if (action.CustomAmount > 0)
                                {
                                    var companyBalanceId = await _clientService.PayByBalance(action.CustomAmount, action.UserId, 1);
                                    CompanyBalanceSend(action.UserId);
                                    await CreateContractSecuredForRefundInDatabase(action);

                                    action.Processed = 1;
                                    await _context.SaveChangesAsync();
                                }
                            }
                            else
                            {
                                if (action.Type == 3 && action.IsApproved == 1)
                                {
                                    // Create the quote for transfer
                                    var recipient = await _context.PayoutRecipients.Where(x => x.userId == action.UserId && x.isdeleted == 0 && x.statusId > 0).FirstOrDefaultAsync();
                                    if (recipient != null)
                                    {
                                        var quoteRequest = new QuoteModel();
                                        quoteRequest.sourceCurrency = "USD";
                                        quoteRequest.targetCurrency = recipient.currency;
                                        quoteRequest.sourceAmount = action.CustomAmount;
                                        quoteRequest.payOut = "BANK_TRANSFER";
                                        quoteRequest.preferredPayIn = "BALANCE";

                                        var quoteResponse = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newquote", quoteRequest);
                                        string quoteString = await quoteResponse.Content.ReadAsStringAsync();
                                        Quote quote = JsonConvert.DeserializeObject<Quote>(quoteString);

                                        if (string.IsNullOrEmpty(quote.id))
                                        {
                                            continue;
                                        }

                                        // Create the transfer
                                        Guid uuid = Guid.NewGuid();
                                        var transferRequest = new TransferModel();
                                        transferRequest.targetAccount = int.Parse(recipient.WiseCustomerId);
                                        transferRequest.quoteUuid = quote.id;
                                        transferRequest.customerTransactionId = uuid.ToString();
                                        transferRequest.details = new TransferDetails();
                                        transferRequest.details.reference = "TF";

                                        var transferResponse = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newpayout", transferRequest);
                                        string transferString = await transferResponse.Content.ReadAsStringAsync();
                                        Transfer transfer = JsonConvert.DeserializeObject<Transfer>(transferString);

                                        if (transfer.id > 0)
                                        {
                                            // Create Fund
                                            var fundRequest = new FundModel();
                                            fundRequest.type = "BALANCE";

                                            var fundResponse = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/complete/" + transfer.id, fundRequest);
                                            string fundString = await fundResponse.Content.ReadAsStringAsync();
                                            FundResponse fund = JsonConvert.DeserializeObject<FundResponse>(fundString);

                                            if (string.IsNullOrWhiteSpace(fund.errorCode))
                                            {
                                                action.Processed = 1;
                                                await _context.SaveChangesAsync();

                                                // Add transaction
                                                if (action.CustomPayoutId != null)
                                                {
                                                    await _contractService.ApprovePayoutTransaction((int)action.CustomPayoutId, transfer.id.ToString());
                                                }

                                                var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == action.UserId && !u.IsDeleted);

                                                if (user != null && user.UserType == 1)
                                                {
                                                    await _clientService.PayByBalance(-action.CustomAmount, action.UserId, 3);
                                                    CompanyBalanceSend(action.UserId);
                                                }

                                                var worker = await _context.AspNetUsers.Where(x => x.Id == action.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                                                if (worker != null)
                                                {
                                                    var name = worker.UserType == 1 ? worker.Company : worker.FullName;
                                                    var request = new LogSupportRequest()
                                                    {
                                                        Text = "Payout to: " + name + " - " + transfer.id + "\nAmount: $" + action.CustomAmount
                                                    };
                                                    _customLogService.LogPayout(request);

                                                    string subject = "We sent you a payment";
                                                    string headtitle = "Payment Sent";
                                                    string text = "A payment is on its way to your bank account, it should be deposited within the next 48 hours.";
                                                    string description = "";
                                                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/balance";
                                                    string buttoncaption = "View Balance";
                                                    await _contractService.NewMailService(0, 31, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, text, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/payment/paymentAction"
                };
                _customLogService.LogError(error);
                return BadRequest();
            }
        }

        public async Task<IActionResult> AutoChargeForHRAction()
        {
            var _context = new GoHireNowContext();
            var list = await _context.sp_hr_charge.FromSql("sp_hr_charge").ToListAsync();
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    var company = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == item.companyId & !u.IsDeleted);
                    if (company != null && item.available < 32)
                    {
                        var customerId = company.CustomerStripeId;
                        if (!string.IsNullOrEmpty(customerId))
                        {
                            var stripePayment = await _context.StripePayments
                                .Where(s => s.CustomerId == customerId && !s.IsDeleted)
                                .OrderByDescending(s => s.Id)
                                .FirstOrDefaultAsync();

                            if (stripePayment == null)
                            {
                                var chargeOptions = new ChargeCreateOptions
                                {
                                    Amount = (int)(item.amount * 100),
                                    Currency = "usd",
                                    Customer = customerId,
                                    Metadata = new Dictionary<string, string>
                                    {
                                        {"customType", "4"},
                                        {"customData", item.contractId.ToString()},
                                        {"chargeType", "hrautodeposit"},
                                        {"chargeAmount", ((int)(item.amount * 100)).ToString()},
                                        {"userId", item.companyId},
                                        {"actionType", "SundayDeposit"},
                                    }
                                };
                                var chargeService = new ChargeService();
                                var rs = chargeService.Create(chargeOptions);
                            }
                            else
                            {
                                var chargeOptions = new ChargeCreateOptions
                                {
                                    Amount = (int)(item.amount * 100),
                                    Currency = "usd",
                                    Customer = customerId,
                                    Source = stripePayment.CardId,
                                    Metadata = new Dictionary<string, string>
                                    {
                                        {"customType", "4"},
                                        {"customData", item.contractId.ToString()},
                                        {"chargeType", "hrautodeposit"},
                                        {"chargeAmount", ((int)(item.amount * 100)).ToString()},
                                        {"userId", item.companyId},
                                        {"actionType", "SundayDeposit"},
                                    }
                                };
                                var chargeService = new ChargeService();
                                var rs = chargeService.Create(chargeOptions);
                            }
                        }
                    }
                }
            }

            return Ok();
        }

        public async Task<IActionResult> AutoWithdrawAction()
        {
            var _context = new GoHireNowContext();
            var httpClient = new HttpClient();
            var list = await _context.sp_actionPayouts.FromSql("sp_actionPayouts").ToListAsync();
            if (list != null && list.Count > 0)
            {
                foreach (var ticket in list)
                {
                    var recipient = await _context.PayoutRecipients.Where(x => x.userId == ticket.userId && x.isdeleted == 0 && x.statusId > 0).FirstOrDefaultAsync();
                    if (recipient != null)
                    {
                        var quoteRequest = new QuoteModel();
                        quoteRequest.sourceCurrency = "USD";
                        quoteRequest.targetCurrency = recipient.currency;
                        quoteRequest.sourceAmount = ticket.amount;
                        quoteRequest.payOut = "BANK_TRANSFER";
                        quoteRequest.preferredPayIn = "BALANCE";

                        var quoteResponse = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("PAYOUTAPIDomain") + "/newquote", quoteRequest);
                        string quoteString = await quoteResponse.Content.ReadAsStringAsync();
                        Quote quote = JsonConvert.DeserializeObject<Quote>(quoteString);

                        var paymentOption = quote.paymentOptions.FirstOrDefault(po => po.payIn == "BALANCE" && !po.disabled);

                        var model = new CreatePayoutTransactionModel()
                        {
                            amountUSD = ticket.amount,
                            exchangeRate = (decimal)quote.rate,
                            amount = (decimal)paymentOption.targetAmount,
                            fee = (decimal)paymentOption.fee.total + (ticket.amount * 99.0m / 100.0m),
                            currency = recipient.currency,
                            arrivingBy = paymentOption.estimatedDelivery,
                            transactionId = "",
                            payoutMethod = 0
                        };

                        var ptId = await _contractService.CreatePayoutTransaction(model, ticket.userId);

                        var newAction = new Actions()
                        {
                            UserId = ticket.userId,
                            Type = 3,
                            Processed = 0,
                            IsApproved = 0,
                            CustomAmount = ticket.amount,
                            CustomContractId = 0,
                            CustomPayoutId = ptId,
                            CreatedDate = DateTime.UtcNow
                        };
                        await _context.Actions.AddAsync(newAction);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok();
        }

        public async Task<IActionResult> ReleaseAction()
        {
            var _context = new GoHireNowContext();
            var onGoingContracts = _context.Contracts.Where(c => c.isAccepted == 1 && c.IsDeleted == 0 && c.AutomaticBilling == 1).ToList();
            foreach (var item in onGoingContracts)
            {
                var user = await _context.AspNetUsers.Where(x => x.Id == item.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();
                if (user != null)
                {
                    var subject = "Review your contract hours and release payment - " + item.Name;
                    string headtitle = "";
                    string message = item.Name;
                    var description = "Your last week's contract has ended, please review your employee hours and release the payment if you are satisfied with the work done.";
                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/user-payment-detail/" + item.Id;
                    string buttoncaption = "View Contract";
                    await _contractService.NewMailService(0, 29, user.Email, user.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                }
            }

            return Ok();
        }

        public async Task<IActionResult> HRWorkerHoursAction()
        {
            var _context = new GoHireNowContext();
            var _toolsContext = new GoHireNowToolsContext();
            DateTime startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday - 7);
            DateTime endOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
            var hrContracts = await _context.HRPremiumContracts.Where(c => c.Status == 1 && c.IsDeleted == 0).ToListAsync();
            string htmlContent = new System.Net.WebClient().DownloadString(_configuration.GetSection("FilePaths")["EmailTemplatePath"] + "HRWorkingHours.html");

            foreach (var contract in hrContracts)
            {
                var usualContract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contract.ContractId && c.IsDeleted == 0);
                var hours = await _context.ContractsHours.Where(ch => ch.IsDeleted == 0 && ch.WorkedDate >= startOfWeek && ch.WorkedDate <= endOfWeek && ch.ContractId == contract.ContractId && ch.Hours > 0).ToListAsync();
                var worker = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == contract.WorkerId && !u.IsDeleted);
                var data = new StringBuilder();
                decimal total = 0.0m;
                if (usualContract != null && hours != null && worker != null && hours.Count > 0)
                {
                    foreach (var hour in hours)
                    {
                        if (hour.Hours > 0)
                        {
                            total += hour.Hours;

                            data.AppendLine("<tr>");
                            data.AppendLine("<td style=\"color: #333333; text-align:center; font-family: 'Myriad Pro', sans-serif; font-size: 19px; font-weight: 400; line-height: 38px; padding-top: 1rem;\">");
                            data.AppendLine(hour.WorkedDate.ToString().Substring(0, 10));
                            data.AppendLine("</td>");
                            data.AppendLine("<td style=\"color: #333333; text-align:center; font-family: 'Myriad Pro', sans-serif; font-size: 19px; font-weight: 400; line-height: 38px; padding-top: 1rem;\">");
                            data.AppendLine(hour.Description);
                            data.AppendLine("</td>");
                            data.AppendLine("<td style=\"color: #333333; text-align:center; font-family: 'Myriad Pro', sans-serif; font-size: 19px; font-weight: 400; line-height: 38px; padding-top: 1rem; padding-right: 2rem;\">");
                            data.AppendLine(hour.Hours.ToString("0.00") + "h");
                            data.AppendLine("</td>");
                        }
                    }

                    htmlContent = htmlContent.Replace("[headtitle]", worker.FullName + " Weekly Hours");
                    htmlContent = htmlContent.Replace("[startdate]", startOfWeek.ToString().Substring(0, 10));
                    htmlContent = htmlContent.Replace("[enddate]", endOfWeek.ToString().Substring(0, 10));
                    htmlContent = htmlContent.Replace("[maincontent]", data.ToString());
                    htmlContent = htmlContent.Replace("[totalhours]", total.ToString("0.00"));

                    var sender = new mailer_sender();
                    sender.ms_custom_id = 0;
                    sender.ms_custom_type = 50;
                    sender.ms_date = DateTime.Now;
                    sender.ms_send_date = DateTime.Now;
                    sender.ms_email = contract.ClientEmail;
                    sender.ms_name = contract.ClientName;
                    sender.ms_subject = "Weekly worked hours by " + worker.FullName;
                    sender.ms_message = htmlContent;
                    sender.ms_from_email = "no-reply@evirtualassistants.com";
                    sender.ms_from_name = "evirtualassistants";
                    sender.ms_priority = 1;
                    sender.ms_issent = 0;
                    sender.ms_unsubscribe = Guid.NewGuid();

                    await _toolsContext.mailer_sender.AddAsync(sender);
                    await _toolsContext.SaveChangesAsync();
                }
            }
            return Ok();
        }

        private Subscription CreateCustomerSubscription(SubscriptionService subscriptions, string customerId, Plan plan)
        {
            var previousList = subscriptions.List(new SubscriptionListOptions
            {
                Customer = customerId
            });
            if (previousList.Count() > 0)
            {
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = false
                };
                subscriptions.Update(previousList.FirstOrDefault().Id, options);
            }

            foreach (var item in previousList)
            {
                var cancelOptions = new SubscriptionCancelOptions
                {
                    InvoiceNow = false,
                    Prorate = false,
                };
                subscriptions.Cancel(item.Id, cancelOptions);
            }

            GlobalPlans globalPlan = _planService.GetAllPlans().Where(p => p.AccessId == plan.Id).FirstOrDefault();

            var subscription = subscriptions.Create(new SubscriptionCreateOptions
            {
                Customer = customerId,
                Description = globalPlan.BillingName,
                Items = new List<SubscriptionItemOptions>() { new SubscriptionItemOptions {
                    Plan = plan.Id,
                    Quantity = 1,
                }},
                Metadata = new Dictionary<string, string>
                {
                    {"customType", "0"}
                }
            });

            return subscription;
        }

        private async Task CreateContractSecuredForRefundInDatabase(Actions action)
        {
            using (var _context = new GoHireNowContext())
            {
                DateTime today = DateTime.Today;
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek) % 7;
                if ((int)today.DayOfWeek == 0)
                {
                    daysUntilMonday -= 7;
                }

                if (action.CustomType == 1)
                {
                    daysUntilMonday += 7;
                }
                DateTime periodDate = today.AddDays(daysUntilMonday - 7);

                var newSecured = new ContractsSecured
                {
                    Amount = -action.CustomAmount,
                    CreatedDate = DateTime.UtcNow,
                    PeriodDate = periodDate,
                    IsDeleted = 0,
                    Method = 3,
                    Type = 6,
                    ContractId = action.CustomContractId
                };

                _contractsSecuredService.PostContractsSecured(newSecured);
            }
        }

        // private async Task CreateContractSecuredForRefundInDatabase(Refund refund, int refundCustomType, string chargeId)
        // {
        //     using (var _context = new GoHireNowContext())
        //     {
        //         var item = await _context.ContractsSecured.Where(x => x.RefundId == refund.Id && x.Type == 3 && x.IsDeleted == 0).FirstOrDefaultAsync();
        //         if (item == null)
        //         {
        //             var metaData = refund.Metadata;

        //             DateTime today = DateTime.Today;
        //             int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek) % 7;
        //             if ((int)today.DayOfWeek == 0)
        //             {
        //                 daysUntilMonday -= 7;
        //             }

        //             if (refundCustomType == 1)
        //             {
        //                 daysUntilMonday += 7;
        //             }
        //             DateTime periodDate = today.AddDays(daysUntilMonday - 7);

        //             decimal amountSecured = -refund.Amount / 100.0m;
        //             int method = 3;
        //             int type = 3;

        //             var newSecured = new ContractsSecured
        //             {
        //                 Amount = amountSecured,
        //                 CreatedDate = DateTime.UtcNow,
        //                 PeriodDate = periodDate,
        //                 StripeChargeId = chargeId,
        //                 RefundId = refund.Id,
        //                 IsDeleted = 0,
        //                 Method = method,
        //                 Type = type,
        //                 ContractId = Int32.Parse(metaData["ContractId"])
        //             };

        //             _contractsSecuredService.PostContractsSecured(newSecured);
        //         }
        //     }
        // }

        private async Task CreateContractTransactionRefundInDatabase(Actions action, int companyBalanceId)
        {
            using (var _context = new GoHireNowContext())
            {
                _stripePaymentService.PostTransaction(new Transactions
                {
                    Amount = -action.CustomAmount,
                    AmountBalance = -action.CustomAmount,
                    AmountBalanceId = companyBalanceId,
                    CreateDate = DateTime.UtcNow,
                    GlobalPlanId = _planService.GetAllPlans().Where(x => x.BillingName == "Security Deposit For Your Contract").FirstOrDefault().Id,
                    IsDeleted = false,
                    UserId = action.UserId,
                    Status = "Success",
                    CustomType = (Int32)action.CustomType,
                    CustomId = (Int32)action.CustomContractId
                });
            }
        }

        // private async Task CreateContractTransactionRefundInDatabase(Charge charge, Refund refund)
        // {
        //     var user = _userManager.Users.Where(u => u.CustomerStripeId == charge.CustomerId).FirstOrDefault();
        //     var metaData = charge.Metadata;

        //     if (user != null && metaData != null && metaData.ContainsKey("customType") && metaData.ContainsKey("customData"))
        //     {
        //         using (var _context = new GoHireNowContext())
        //         {
        //             var transaction = await _context.Transactions.Where(x => x.RefundId == refund.Id && x.IsDeleted == false).FirstOrDefaultAsync();
        //             if (transaction == null)
        //             {
        //                 _stripePaymentService.PostTransaction(new Transactions
        //                 {
        //                     Amount = -Decimal.Divide(refund.Amount, 100),
        //                     CreateDate = DateTime.UtcNow,
        //                     GlobalPlanId = _planService.GetAllPlans().Where(x => x.BillingName == "Security Deposit For Your Contract").FirstOrDefault().Id,
        //                     IsDeleted = false,
        //                     Receipt = charge.ReceiptUrl ?? string.Empty,
        //                     ReceiptId = charge.ReceiptNumber ?? string.Empty,
        //                     Status = charge.Status,
        //                     RefundId = refund.Id,
        //                     UserId = user.Id,
        //                     CustomType = Int32.Parse(metaData["customType"]),
        //                     CustomId = Int32.Parse(metaData["customData"])
        //                 });
        //             }
        //         }
        //     }
        // }

        private async Task CreatePlanTransactionInDatabase(Invoice invoice, Charge charge, Plan plan)
        {
            var user = _userManager.Users.Where(u => u.CustomerStripeId == invoice.CustomerId).FirstOrDefault();
            _stripePaymentService.PostTransaction(new Transactions
            {
                Amount = Decimal.Divide(invoice.AmountPaid, 100),
                AmountBalance = 0,
                AmountBalanceId = 0,
                // CardName = card.Name != null ? card.Brand : card.Name,
                CreateDate = invoice.WebhooksDeliveredAt ?? DateTime.UtcNow,
                GlobalPlanId = _planService.GetAllPlans().Where(x => x.AccessId == plan.Id).FirstOrDefault().Id,
                IsDeleted = false,
                Receipt = charge.ReceiptUrl ?? string.Empty,
                ReceiptId = charge.ReceiptNumber ?? string.Empty,
                Status = invoice.Status,
                UserId = user.Id,
                // CustomType = _transactionsTypeService.GetAllTransactionsTypes().Where(x => x.Id == Int32.Parse(metaData["customType"])).FirstOrDefault().Id,
                CustomType = 0,
                CustomId = 0,
            });
            await _clientService.ProcessCurrentPricingPlan(user.Id);
            var planName = _planService.GetAllPlans().Where(x => x.AccessId == plan.Id).FirstOrDefault().Name;
            var result = _stripePaymentService.SendInvoiceToClient(planName, user.Company, invoice.Id, Decimal.Divide(invoice.AmountPaid, 100).ToString(), user.Email);
        }

        private async Task CreateOtherTransactionInDatabase(Invoice invoice, Charge charge = null)
        {
            var user = _userManager.Users.Where(u => u.CustomerStripeId == invoice.CustomerId).FirstOrDefault();
            if (user != null)
            {
                var metaData = invoice.Metadata;
                var customId = 0;
                if (metaData.ContainsKey("customType") && metaData.ContainsKey("customData") && metaData.ContainsKey("paymentType"))
                {
                    if (metaData["paymentType"] != "2")
                    {
                        if (metaData["customType"] == "2")
                        {
                            var _context = new GoHireNowContext();
                            var profile = _context.AspNetUsers.Where(u => u.Id == metaData["customData"]).FirstOrDefault();
                            customId = profile.UserUniqueId;
                        }
                        else
                        {
                            customId = Int32.Parse(metaData["customData"]);
                        }
                    }
                    else
                    {
                        user.GlobalPlanId = null;
                        await _userManager.UpdateAsync(user);
                    }
                    var planName = "";
                    foreach (var item in invoice.Lines)
                    {
                        var id = _planService.GetAllPlans().Where(x => x.AccessId == item.Price.Id).FirstOrDefault().Id;
                        planName += _planService.GetAllPlans().Where(x => x.AccessId == item.Price.Id).FirstOrDefault().Name;
                        if (item != invoice.Lines.LastOrDefault())
                        {
                            planName += ", ";
                        }
                        if (id == 17)
                        {
                            await _clientJobService.UpdateJobStatus(customId, user.Id, 2);
                        }
                        if (id == 10 || id == 11 || id == 17)
                        {
                            var _context = new GoHireNowContext();
                            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == customId && !j.IsDeleted);
                            if (job != null)
                            {
                                job.IsEmail = 1;
                            }
                            await _context.SaveChangesAsync();
                        }
                        if (metaData["customType"] == "2")
                        {
                            _userSecurityCheckService.PostUserSecurityCheck(new UserSecurityCheck
                            {
                                CompanyId = user.Id,
                                UserId = metaData["customData"],
                                isDeleted = false,
                                CreatedDate = invoice.WebhooksDeliveredAt ?? DateTime.UtcNow,
                            });
                        }

                        var companyBalanceId = 0;
                        if (metaData.ContainsKey("byCard") && metaData.ContainsKey("paymentType") && metaData["byCard"] == "False")
                        {
                            companyBalanceId = await _clientService.PayByBalance(-Decimal.Divide(item.Amount, 100), user.Id, 2);
                            CompanyBalanceSend(user.Id);

                            using (var _context = new GoHireNowContext())
                            {
                                var request = new LogSupportRequest()
                                {
                                    Text = "Charge succeeded (" + item.Description + "): " + user.Company + " - " + companyBalanceId + "\nAmount: $" + Decimal.Divide(item.Amount, 100)
                                };
                                _customLogService.LogPayout(request);
                            }
                        }

                        _stripePaymentService.PostTransaction(new Transactions
                        {
                            Amount = Decimal.Divide(item.Amount, 100),
                            AmountBalance = metaData["byCard"] == "False" ? Decimal.Divide(item.Amount, 100) : 0,
                            AmountBalanceId = companyBalanceId,
                            // CardName = card.Name != null ? card.Brand : card.Name,
                            CreateDate = invoice.WebhooksDeliveredAt ?? DateTime.UtcNow,
                            GlobalPlanId = id,
                            IsDeleted = false,
                            Receipt = charge?.ReceiptUrl ?? string.Empty,
                            ReceiptId = charge?.ReceiptNumber ?? string.Empty,
                            Status = invoice.Status,
                            UserId = user.Id,
                            CustomType = Int32.Parse(metaData["customType"]),
                            // CustomType = _transactionsTypeService.GetAllTransactionsTypes().Where(x => x.Id == Int32.Parse(metaData["CustomType"])).FirstOrDefault().Id,
                            CustomId = customId
                        });
                    }

                    if (invoice.AmountPaid > 0)
                    {
                        var result = _stripePaymentService.SendInvoiceToClient(planName, user.Company, invoice.Id, Decimal.Divide(invoice.AmountPaid, 100).ToString(), user.Email);
                    }
                }
            }
        }

        private async Task BonusPayProcess(int amount, string userId, int contractId, bool byBalance, Charge charge = null)
        {
            using (var _context = new GoHireNowContext())
            {
                DateTime today = DateTime.Today;
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek) % 7;
                if ((int)today.DayOfWeek == 0)
                {
                    daysUntilMonday -= 7;
                }
                DateTime periodDate = today.AddDays(daysUntilMonday);
                var newSecured = new ContractsSecured
                {
                    Amount = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(Int32.Parse(charge.Metadata["chargeAmount"]) + Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                    CreatedDate = DateTime.UtcNow,
                    PeriodDate = periodDate,
                    StripeChargeId = charge?.Id ?? string.Empty,
                    IsDeleted = 0,
                    Method = 1,
                    Type = 7,
                    ContractId = contractId
                };
                await _context.ContractsSecured.AddAsync(newSecured);
                await _context.SaveChangesAsync();

                var newInvoice = new ContractsInvoices
                {
                    ContractId = contractId,
                    CreatedDate = DateTime.UtcNow,
                    PaidDate = DateTime.UtcNow,
                    Hours = 0,
                    InvoiceType = 2,
                    Amount = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(Int32.Parse(charge.Metadata["chargeAmount"]) + Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                    PayoutStatusId = 1,
                    StatusId = 1,
                    IsDeleted = 0,
                    SecuredId = newSecured.Id,
                    PayoutCommission = 10
                };

                _context.ContractsInvoices.Add(newInvoice);
                _context.SaveChanges();
            }

            var companyBalanceId = 0;
            if (byBalance)
            {
                companyBalanceId = await _clientService.PayByBalance(-Decimal.Divide(amount, 100), userId, 2);
                CompanyBalanceSend(userId);

                using (var _context = new GoHireNowContext())
                {
                    var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
                    var request = new LogSupportRequest()
                    {
                        Text = "Charge succeeded (Bonus): " + user.Company + " - " + companyBalanceId + "\nAmount: $" + Decimal.Divide(amount, 100)
                    };
                    _customLogService.LogPayout(request);
                }
            }
            else
            {
                if (charge != null && charge.Metadata.ContainsKey("amountByBalance") && Int32.Parse(charge.Metadata["amountByBalance"]) > 0)
                {
                    companyBalanceId = await _clientService.PayByBalance(-Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100), userId, 2);
                    CompanyBalanceSend(userId);

                    using (var _context = new GoHireNowContext())
                    {
                        var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
                        var request = new LogSupportRequest()
                        {
                            Text = "Charge succeeded (Bonus): " + user.Company + " - " + companyBalanceId + "\nAmount: $" + Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100)
                        };
                        _customLogService.LogPayout(request);
                    }
                }
            }

            _stripePaymentService.PostTransaction(new Transactions
            {
                Amount = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(Int32.Parse(charge.Metadata["chargeAmount"]) + Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                AmountBalance = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                AmountBalanceId = companyBalanceId,
                CreateDate = DateTime.UtcNow,
                GlobalPlanId = _planService.GetAllPlans().Where(x => x.BillingName == "Security Deposit For Your Contract").FirstOrDefault().Id,
                IsDeleted = false,
                Receipt = charge?.ReceiptUrl ?? string.Empty,
                ReceiptId = charge?.ReceiptNumber ?? string.Empty,
                ChargeId = charge?.Id ?? string.Empty,
                Status = byBalance ? "Success" : charge?.Status,
                UserId = userId,
                CustomType = 3,
                CustomId = contractId
            });
        }

        private async Task HRPremiumPayProcess(int amount, string userId, string hrId, bool byBalance, Charge charge = null)
        {
            if (byBalance || charge.Metadata["chargeType"] == "deposit")
            {
                _contractService.AddHRContract(userId, hrId);
            }

            var companyBalanceId = 0;
            if (byBalance)
            {
                companyBalanceId = await _clientService.PayByBalance(-Decimal.Divide(amount, 100), userId, 2);
                CompanyBalanceSend(userId);

                using (var _context = new GoHireNowContext())
                {
                    var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
                    var request = new LogSupportRequest()
                    {
                        Text = "Charge succeeded (HRPremium): " + user.Company + " - " + companyBalanceId + "\nAmount: $" + Decimal.Divide(amount, 100)
                    };
                    _customLogService.LogPayout(request);
                }
            }
            else
            {
                if (charge != null && charge.Metadata.ContainsKey("amountByBalance") && Int32.Parse(charge.Metadata["amountByBalance"]) > 0)
                {
                    companyBalanceId = await _clientService.PayByBalance(-Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100), userId, 2);
                    CompanyBalanceSend(userId);

                    using (var _context = new GoHireNowContext())
                    {
                        var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
                        var request = new LogSupportRequest()
                        {
                            Text = "Charge succeeded (HRPremium): " + user.Company + " - " + companyBalanceId + "\nAmount: $" + Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100)
                        };
                        _customLogService.LogPayout(request);
                    }
                }
            }

            _stripePaymentService.PostTransaction(new Transactions
            {
                Amount = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(amount + Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                AmountBalance = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                AmountBalanceId = companyBalanceId,
                CreateDate = DateTime.UtcNow,
                GlobalPlanId = _planService.GetAllPlans().Where(x => x.BillingName == "Salary HR").FirstOrDefault().Id,
                IsDeleted = false,
                Receipt = charge?.ReceiptUrl ?? string.Empty,
                ReceiptId = charge?.ReceiptNumber ?? string.Empty,
                ChargeId = charge?.Id ?? string.Empty,
                Status = byBalance ? "Success" : charge?.Status,
                UserId = userId,
                CustomType = 4,
            });

            if (!byBalance)
            {
                var _context = new GoHireNowContext();
                var company = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

                if (company != null && charge != null)
                {
                    _stripePaymentService.SendInvoiceToClient("Premium HR", company.Company, charge.Id, Decimal.Divide(amount, 100).ToString(), company.Email);
                }
            }
        }

        private async Task SecurityDepositPayProcess(int amount, string userId, int contractId, bool byBalance, bool isSuccess, Charge charge = null)
        {
            DateTime today = DateTime.Today;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek) % 7;
            if ((int)today.DayOfWeek == 0)
            {
                daysUntilMonday -= 7;
            }
            DateTime periodDate = today.AddDays(daysUntilMonday);
            var newSecured = new ContractsSecured
            {
                Amount = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(Int32.Parse(charge.Metadata["chargeAmount"]) + Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                CreatedDate = DateTime.UtcNow,
                PeriodDate = periodDate,
                StripeChargeId = charge?.Id ?? string.Empty,
                IsDeleted = 0,
                Method = isSuccess ? 1 : 4,
                Type = isSuccess ? 1 : 4,
                ContractId = contractId
            };
            _contractsSecuredService.PostContractsSecured(newSecured);

            _contractService.isautomaticbilling(userId, new UpdateAutomaticBillingModel
            {
                ContractId = contractId,
                AutomaticBilling = isSuccess ? 1 : 0
            });

            if (isSuccess)
            {
                var companyBalanceId = 0;
                if (byBalance)
                {
                    companyBalanceId = await _clientService.PayByBalance(-Decimal.Divide(amount, 100), userId, 2);
                    CompanyBalanceSend(userId);

                    using (var _context = new GoHireNowContext())
                    {
                        var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
                        var request = new LogSupportRequest()
                        {
                            Text = "Charge succeeded (SecurityDeposit): " + user.Company + " - " + companyBalanceId + "\nAmount: $" + Decimal.Divide(amount, 100)
                        };
                        _customLogService.LogPayout(request);
                    }
                }
                else
                {
                    if (charge != null && charge.Metadata.ContainsKey("amountByBalance") && Int32.Parse(charge.Metadata["amountByBalance"]) > 0)
                    {
                        companyBalanceId = await _clientService.PayByBalance(-Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100), userId, 2);
                        CompanyBalanceSend(userId);

                        using (var _context = new GoHireNowContext())
                        {
                            var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
                            var request = new LogSupportRequest()
                            {
                                Text = "Charge succeeded (SecurityDeposit): " + user.Company + " - " + companyBalanceId + "\nAmount: $" + Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100)
                            };
                            _customLogService.LogPayout(request);
                        }
                    }
                }

                _stripePaymentService.PostTransaction(new Transactions
                {
                    Amount = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(amount + Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                    AmountBalance = byBalance ? Decimal.Divide(amount, 100) : Decimal.Divide(Int32.Parse(charge.Metadata["amountByBalance"]), 100),
                    AmountBalanceId = companyBalanceId,
                    CreateDate = DateTime.UtcNow,
                    GlobalPlanId = _planService.GetAllPlans().Where(x => x.BillingName == "Security Deposit For Your Contract").FirstOrDefault().Id,
                    IsDeleted = false,
                    Receipt = charge?.ReceiptUrl ?? string.Empty,
                    ReceiptId = charge?.ReceiptNumber ?? string.Empty,
                    ChargeId = charge?.Id ?? string.Empty,
                    Status = byBalance ? "Success" : charge?.Status,
                    UserId = userId,
                    CustomType = 3,
                    CustomId = contractId
                });
            }

            string message = isSuccess ?
                "SECURITY DEPOSIT\nI have activated the\nsecurity deposit and\nsecured this contract." :
                "SECURITY DEPOSIT NEEDED\nPlease wait for a new\nsecurity deposit for this\nweek to continue working.";
            SendDepositMessage(contractId, message);

            using (var _context = new GoHireNowContext())
            {
                var contract = await _context.Contracts.Where(x => x.Id == contractId && x.IsDeleted == 0).FirstOrDefaultAsync();
                var worker = await _context.AspNetUsers.Where(x => x.Id == contract.UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                var company = await _context.AspNetUsers.Where(x => x.Id == contract.CompanyId && x.IsDeleted == false).FirstOrDefaultAsync();

                if (worker != null && isSuccess)
                {
                    string subject = "Job Security Deposit Received - " + contract.Name;
                    string headtitle = "Security Deposit Received";
                    string text = contract.Name;
                    string description = "We have secured your week's pay from " + company.Company + ".<br/>You can start to work. Don't forget to add your hours to the job offer to get paid.";
                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/billing-detail/" + contractId;
                    string buttoncaption = "View Job";
                    await _contractService.NewMailService(0, 18, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, text, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                }
                else
                {
                    if (company != null && !isSuccess)
                    {
                        string subject = "Action Needed: The Security Deposit Failed - " + contract.Name;
                        string headtitle = "Security Deposit Failed";
                        string text = contract.Name;
                        string description = "Your weekly security deposit encounters a problem, go to your contract page and reactivate your contract by creating a security deposit.";
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/user-payment-detail/" + contractId;
                        string buttoncaption = "View Contract";
                        await _contractService.NewMailService(0, 32, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }
                }

                if (contract != null)
                {
                    ContractNotificationSend(contract.UserId, contract.CompanyId);
                }
            }
        }

        private async Task ContractNotificationSend(string workerId, string companyId)
        {
            var unapproved = await _contractService.GetUnapprovalCount(workerId);
            var undeposited = await _contractService.GetUndepositCount(companyId);

            if (undeposited >= 0)
            {
                var result = await pusher.TriggerAsync(
                    $"contractDeposited-{companyId}",
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
                    $"contractApproved-{workerId}",
                    "contractApproved",
                    new
                    {
                        unapproved = unapproved
                    }
                );
            }
        }

        private async Task CompanyBalanceSend(string companyId)
        {
            var balance = await _clientService.GetAccountBalance(companyId);

            if (balance != null)
            {
                var result = await pusher.TriggerAsync(
                    $"companyBalance-{companyId}",
                    "companyBalance",
                    new
                    {
                        balance = balance.amount
                    }
                );
            }
        }

        private async Task SendDepositMessage(int contractId, string message)
        {
            try
            {
                if (contractId > 0)
                {
                    var _context = new GoHireNowContext();
                    var contract = _context.Contracts.Where(x => x.Id == contractId && x.IsDeleted == 0).FirstOrDefault();

                    var request = new SendMessageRequest
                    {
                        fromUserId = contract.CompanyId,
                        toUserId = contract.UserId,
                        message = message,
                        customId = contractId,
                        customIdType = 1,
                        isDirectMessage = false
                    };

                    var send = await _mailService.InitialSendMessage(request);
                    if (send > 0)
                    {
                        await SendMessageToUser(send);
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        private async Task<bool> SendMessageToUser(int id)
        {
            var message = await _mailService.GetMessageById(id);
            var rand = new Random();
            var result = await pusher.TriggerAsync(
                $"private-{message.ToUserId}",
                "message",
                new
                {
                    message = message.Message,
                    from = message.FromUserId,
                    name = message.FromUser.UserType == 1 ? message.FromUser.Company : message.FromUser.FullName,
                    date = message.CreateDate.ToShortDateString(),
                    mailId = message.MailId,
                    picture = !string.IsNullOrEmpty(message.FromUser.ProfilePicture)
                                    ? $"{FilePathRoot}/Profile-Pictures/" + message.FromUser.ProfilePicture
                                    : "",
                    sent = false,
                    id = message.MailId, // This is for if we don't have chat then we have to append a chat
                    title = message.FromUser.UserTitle, // This is for if we don't have chat then we have to append a chat
                    lastLogin = message.FromUser.LastLoginTime, // This is for if we don't have chat then we have to append a chat,
                    mailDate = message.Mail.CreateDate.ToShortDateString(), // This is used to sort mails on the client side.
                    fromUserId = message.ToUserId,
                    toUserId = message.FromUserId,
                    isRead = false,
                    myPicture = !string.IsNullOrEmpty(message.ToUser.ProfilePicture)
                                    ? $"{FilePathRoot}/Profile-Pictures/" + message.ToUser.ProfilePicture
                                    : "",
                    userType = (int)message.FromUser.UserType,
                    fileName = message.FileName,
                    filePath = !string.IsNullOrEmpty(message.FilePath) ? FilePathRoot.Replace("Resources", "") + "Home/Download/MessageAttachment?id=" + message.Id : "",
                    fileExtension = !string.IsNullOrEmpty(message.FilePath) && !string.IsNullOrEmpty(message.FileName)
                                        ? (
                                            LookupService.GetFileImage(Path.GetExtension(message.FileName), "") != "img"
                                                ? Path.GetExtension(message.FileName).Replace(".", "") : ""
                                        ) : "",
                    fileImage = !string.IsNullOrEmpty(message.FilePath) && !string.IsNullOrEmpty(message.FileName)
                                        ? (
                                            LookupService.GetFileImage(Path.GetExtension(message.FileName), "") == "img"
                                                ? $"{FilePathRoot}/MessageAttachments/{message.FilePath}" : ""
                                        ) : "",
                    messageId = message.Id,
                    customId = message.CustomId,
                    customIdType = message.CustomIdType,
                    customLink = message.CustomLink
                });
            return true;
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

        private static string GenerateUniqueString()
        {
            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Random random = new Random();
            int randomNumber = random.Next(1000, 9999);

            string uniqueString = $"{timestamp}_{randomNumber}";

            return uniqueString;
        }

        static string GetQueryString<T>(T obj)
        {
            StringBuilder queryStringBuilder = new StringBuilder();

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (property.Name != "verifySign")
                {
                    string value = property.GetValue(obj)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        queryStringBuilder.Append($"{property.Name}={value}&");
                    }
                }
            }

            return queryStringBuilder.ToString();
        }
        #endregion

        #region Request Models
        public class AddRecipientModel
        {
            public int statusId { get; set; }
            public string currency { get; set; }
            public string ispersonal { get; set; }
            public string email { get; set; }
            public string accountHolderName { get; set; }
            public string country { get; set; }
            public string WiseCustomerId { get; set; }
        }

        public class QuoteModel
        {
            public string sourceCurrency { get; set; }
            public string targetCurrency { get; set; }
            public decimal sourceAmount { get; set; }
            public string payOut { get; set; }
            public string preferredPayIn { get; set; }
        }

        public class TransferModel
        {
            public int targetAccount { get; set; }
            public string quoteUuid { get; set; }
            public string customerTransactionId { get; set; }
            public TransferDetails details { get; set; }
        }

        public class FundModel
        {
            public string type { get; set; }
        }

        public class TransferDetails
        {
            public string reference { get; set; }
            public string transferPurpose { get; set; }
            public string sourceOfFunds { get; set; }
        }

        public class RecipientDataDetails
        {
            public object address { get; set; }
            public object email { get; set; }
            public string legalType { get; set; }
            public object accountNumber { get; set; }
            public object sortCode { get; set; }
            public object abartn { get; set; }
            public object accountType { get; set; }
            public object bankgiroNumber { get; set; }
            public object ifscCode { get; set; }
            public object bsbCode { get; set; }
            public object institutionNumber { get; set; }
            public object transitNumber { get; set; }
            public object phoneNumber { get; set; }
            public object bankCode { get; set; }
            public object russiaRegion { get; set; }
            public object routingNumber { get; set; }
            public object branchCode { get; set; }
            public object cpf { get; set; }
            public object cardNumber { get; set; }
            public object idType { get; set; }
            public object idNumber { get; set; }
            public object idCountryIso3 { get; set; }
            public object idValidFrom { get; set; }
            public object idValidTo { get; set; }
            public object clabe { get; set; }
            public object swiftCode { get; set; }
            public object dateOfBirth { get; set; }
            public object clearingNumber { get; set; }
            public object bankName { get; set; }
            public object branchName { get; set; }
            public object businessNumber { get; set; }
            public object province { get; set; }
            public object city { get; set; }
            public object rut { get; set; }
            public object token { get; set; }
            public object cnpj { get; set; }
            public object payinReference { get; set; }
            public object pspReference { get; set; }
            public object orderId { get; set; }
            public object idDocumentType { get; set; }
            public object idDocumentNumber { get; set; }
            public object targetProfile { get; set; }
            public object targetUserId { get; set; }
            public object taxId { get; set; }
            public object job { get; set; }
            public object nationality { get; set; }
            public object interacAccount { get; set; }
            public object bban { get; set; }
            public object town { get; set; }
            public object postCode { get; set; }
            public object language { get; set; }
            public string IBAN { get; set; }
            // public string iban { get; set; }
            public object BIC { get; set; }
            // public object bic { get; set; }
        }

        public class CustomErrorObject
        {
            public string Code { get; set; }
            public string Message { get; set; }
            public string Path { get; set; }
            public string[] Arguments { get; set; }
        }

        public class RecipientData
        {
            public int id { get; set; }
            public int business { get; set; }
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string country { get; set; }
            public string type { get; set; }
            public RecipientDataDetails details { get; set; }
            public int user { get; set; }
            public bool active { get; set; }
            public bool ownedByCustomer { get; set; }
            public bool responseStatus { get; set; }
            public List<CustomErrorObject> errors { get; set; }
        }

        public class AccountRequirements
        {
            public string type { get; set; }
            public string title { get; set; }
            public string usageInfo { get; set; }
            public List<object> fields { get; set; }
        }

        public class RefreshRecipientData
        {
            public string quoteId { get; set; }
            public RecipientDataDetails details { get; set; }
        }

        public class Address
        {
            public string country { get; set; }
            public string city { get; set; }
            public string postCode { get; set; }
            public string firstLine { get; set; }
            public string state { get; set; }
        }

        public class DetailsUSD
        {
            public string legalType { get; set; }
            public string abartn { get; set; }
            public string swiftCode { get; set; }
            public string accountNumber { get; set; }
            public string accountType { get; set; }
            public Address address { get; set; }
        }

        public class NewRecipientDataUSD
        {
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string type { get; set; }
            public DetailsUSD details { get; set; }
        }

        public class DetailsPHP
        {
            public string legalType { get; set; }
            public string bankCode { get; set; }
            public string accountNumber { get; set; }
            public Address address { get; set; }
        }

        public class NewRecipientDataPHP
        {
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string type { get; set; }
            public DetailsPHP details { get; set; }
        }

        public class DetailsEUR
        {
            public string legalType { get; set; }
            public string BIC { get; set; }
            public string IBAN { get; set; }
            public string swiftCode { get; set; }
            public string accountNumber { get; set; }
            public Address address { get; set; }
        }

        public class NewRecipientDataEUR
        {
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string type { get; set; }
            public DetailsEUR details { get; set; }
        }

        public class DetailsCAD
        {
            public string legalType { get; set; }
            public string institutionNumber { get; set; }
            public string transitNumber { get; set; }
            public string accountNumber { get; set; }
            public string accountType { get; set; }
            public string interacAccount { get; set; }
            public Address address { get; set; }
        }

        public class NewRecipientDataCAD
        {
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string type { get; set; }
            public DetailsCAD details { get; set; }
        }

        public class DetailsGBP
        {
            public string legalType { get; set; }
            public string sortCode { get; set; }
            public string accountNumber { get; set; }
            public string BIC { get; set; }
            public string IBAN { get; set; }
            public Address address { get; set; }
        }

        public class NewRecipientDataGBP
        {
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string type { get; set; }
            public DetailsGBP details { get; set; }
        }

        public class DetailsAUD
        {
            public string legalType { get; set; }
            public string bsbCode { get; set; }
            public string accountNumber { get; set; }
            public string billerCode { get; set; }
            public string customerReferenceNumber { get; set; }
            public Address address { get; set; }
        }

        public class NewRecipientDataAUD
        {
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string type { get; set; }
            public DetailsAUD details { get; set; }
        }

        public class DetailsINR
        {
            public string legalType { get; set; }
            public string ifscCode { get; set; }
            public string accountNumber { get; set; }
            public Address address { get; set; }
        }

        public class NewRecipientDataINR
        {
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string type { get; set; }
            public DetailsINR details { get; set; }
        }

        public class DetailsKES
        {
            public string legalType { get; set; }
            public string accountNumber { get; set; }
            public string bankCode { get; set; }
            public Address address { get; set; }
        }

        public class NewRecipientDataKES
        {
            public int profile { get; set; }
            public string accountHolderName { get; set; }
            public string currency { get; set; }
            public string type { get; set; }
            public DetailsKES details { get; set; }
        }

        public class TransferDataDetails
        {
            public string reference { get; set; }
            public string transferPurpose { get; set; }
            public string sourceOfFunds { get; set; }
        }

        public class TransferData
        {
            public int targetAccount { get; set; }
            public string quoteUuid { get; set; }
            public string customerTransactionId { get; set; }
            public TransferDataDetails details { get; set; }
        }

        public class CompleteInfo
        {
            public string type { get; set; }
        }

        public class CompleteResponse
        {
            public string type { get; set; }
            public string status { get; set; }
            public string errorCode { get; set; } = "";
            public List<CustomErrorObject> errors { get; set; }
        }

        public class Quote
        {
            public string id { get; set; }
            public string sourceCurrency { get; set; }
            public string targetCurrency { get; set; }
            public double sourceAmount { get; set; }
            public string payOut { get; set; }
            public string preferredPayIn { get; set; }
            public double rate { get; set; }
            public DateTime createdTime { get; set; }
            public int user { get; set; }
            public int profile { get; set; }
            public string rateType { get; set; }
            public DateTime rateExpirationTime { get; set; }
            public bool guaranteedTargetAmountAllowed { get; set; }
            public bool targetAmountAllowed { get; set; }
            public bool guaranteedTargetAmount { get; set; }
            public string providedAmountType { get; set; }
            public List<PaymentOption> paymentOptions { get; set; }
            public string status { get; set; }
            public DateTime expirationTime { get; set; }
            public List<Notice> notices { get; set; }
        }

        public class Notice
        {
            public string text { get; set; }
            public string link { get; set; }
            public string type { get; set; }
        }

        public class PaymentOption
        {
            public bool disabled { get; set; }
            public DateTime? estimatedDelivery { get; set; }
            public string formattedEstimatedDelivery { get; set; }
            public List<object> estimatedDeliveryDelays { get; set; }
            public Fee fee { get; set; }
            public Price price { get; set; }
            public double sourceAmount { get; set; }
            public double targetAmount { get; set; }
            public string sourceCurrency { get; set; }
            public string targetCurrency { get; set; }
            public string payIn { get; set; }
            public string payOut { get; set; }
            public List<string> allowedProfileTypes { get; set; }
            public string payInProduct { get; set; }
            public double feePercentage { get; set; }
            public DisabledReason disabledReason { get; set; }
        }

        public class DisabledReason
        {
            public string code { get; set; }
            public string message { get; set; }
        }

        public class Price
        {
            public int priceSetId { get; set; }
            public Total total { get; set; }
            public List<Item> items { get; set; }
        }

        public class Fee
        {
            public double transferwise { get; set; }
            public double payIn { get; set; }
            public double discount { get; set; }
            public double partner { get; set; }
            public double total { get; set; }
        }

        public class Item
        {
            public string type { get; set; }
            public string label { get; set; }
            public Value value { get; set; }
            public int id { get; set; }
            public Explanation explanation { get; set; }
        }

        public class Total
        {
            public string type { get; set; }
            public string label { get; set; }
            public Value value { get; set; }
        }

        public class Value
        {
            public double amount { get; set; }
            public string currency { get; set; }
            public string Label { get; set; }
        }

        public class Explanation
        {
            public string plainText { get; set; }
        }

        public class Transfer
        {
            public int id { get; set; }
            public int user { get; set; }
            public int targetAccount { get; set; }
            public object sourceAccount { get; set; }
            public object quote { get; set; }
            public string quoteUuid { get; set; }
            public string status { get; set; }
            public string reference { get; set; }
            public double rate { get; set; }
            public string created { get; set; }
            public int business { get; set; }
            public object transferRequest { get; set; }
            public TransferDetails details { get; set; }
            public bool hasActiveIssues { get; set; }
            public string sourceCurrency { get; set; }
            public double sourceValue { get; set; }
            public string targetCurrency { get; set; }
            public double targetValue { get; set; }
            public string customerTransactionId { get; set; }
        }

        public class FundResponse
        {
            public string type { get; set; }
            public string status { get; set; }
            public string errorCode { get; set; } = "";
            public List<CustomErrorObject> errors { get; set; }
        }

        public class WiseWebhookResponseResource
        {
            public string type { get; set; }
            public int id { get; set; }
            public int profile_id { get; set; }
            public int account_id { get; set; }
        }

        public class WiseWebhookResponseData
        {
            public WiseWebhookResponseResource resource { get; set; } = null;
            public string current_state { get; set; }
            public int? transfer_id { get; set; }
            public int? profile_id { get; set; }
            public string failure_reason_code { get; set; }
            public string failure_description { get; set; }
            public string previous_state { get; set; } = "";
            public List<string> active_cases { get; set; } = null;
            public DateTime? occurred_at { get; set; }
        }

        public class WiseWebhookResponse
        {
            public WiseWebhookResponseData data { get; set; }
            public string subscription_id { get; set; }
            public string event_type { get; set; }
            public string schema_version { get; set; }
            public DateTime? sent_at { get; set; }
            public DateTime? occured_at { get; set; }
        }

        public class PockytCreateCustomerRequest
        {
            public string countryCode { get; set; }
            public string email { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string merchantNo { get; set; }
            public string storeNo { get; set; }
            public string verifySign { get; set; }
        }

        public class PockytCreateCustomerResponse
        {
            public string ret_code { get; set; }
            public string ret_msg { get; set; }
            public PockytCustomer customer { get; set; }
        }

        public class PockytCustomer
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string customerCode { get; set; }
            public string createdTime { get; set; }
            public string customerNo { get; set; }
            public string email { get; set; }
        }

        public class PockytSecurePayRequest
        {
            public string amount { get; set; }
            public string creditType { get; set; }
            public string callbackUrl { get; set; }
            public string currency { get; set; }
            public string customerNo { get; set; }
            public string ipnUrl { get; set; }
            public string merchantNo { get; set; }
            public string reference { get; set; }
            public string settleCurrency { get; set; }
            public string storeNo { get; set; }
            public string terminal { get; set; }
            public string vendor { get; set; }
            public string verifySign { get; set; }
        }

        public class PockytProcessRequest
        {
            public string merchantNo { get; set; }
            public string storeNo { get; set; }
            public string timestamp { get; set; }
            public string transactionNo { get; set; }
            public string verifySign { get; set; }
        }

        public class PockytSecurePayResponse
        {
            public string ret_code { get; set; }
            public string ret_msg { get; set; }
            public PockytSecurePay result { get; set; }
        }

        public class PockytSecurePay
        {
            public string amount { get; set; }
            public string cashierUrl { get; set; }
            public string currency { get; set; }
            public string reference { get; set; }
            public string settleCurrency { get; set; }
            public string transactionNo { get; set; }
        }

        public class PockytWebhookResponse
        {
            public string amount { get; set; }
            public string currency { get; set; }
            public string customerNo { get; set; }
            public string reference { get; set; }
            public string settleCurrency { get; set; }
            public string status { get; set; }
            public string time { get; set; }
            public string transactionNo { get; set; }
            public string vaultId { get; set; }
            public string vendorId { get; set; }
            public string verifySign { get; set; }
        }

        public class PockytProcessResponse
        {
            public string amount { get; set; }
            public string currency { get; set; }
            public string customerNo { get; set; }
            public string reference { get; set; }
            public string status { get; set; }
            public string transactionNo { get; set; }
            public string vaultId { get; set; }
            public string vendorId { get; set; }
            public string supUserid { get; set; }
            public string paymentTime { get; set; }
        }

        #endregion
    }
}