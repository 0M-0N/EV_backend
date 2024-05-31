using GoHireNow.Api.Handlers.ClientHandlers;
using GoHireNow.Api.Handlers.ClientHandlers.Interfaces;
using GoHireNow.Service.ClientServices;
using GoHireNow.Service.ContractService;
using GoHireNow.Service.ContractsInvoicesServices;
using GoHireNow.Service.ContractsSecuredServices;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.EmailServices;
using GoHireNow.Service.FavoriteServices;
using GoHireNow.Service.HireServices;
using GoHireNow.Service.HomeServices;
using GoHireNow.Service.Interfaces;
using GoHireNow.Service.MailServices;
using GoHireNow.Service.PlanServices;
using GoHireNow.Service.TransactionsTypeServices;
using GoHireNow.Service.SkillServices;
using GoHireNow.Service.StripePaymentServices;
using GoHireNow.Service.UserSecurityCheckServices;
using GoHireNow.Service.UserPortifolioServices;
using GoHireNow.Service.WorkerServices;
using Microsoft.Extensions.DependencyInjection;

namespace GoHireNow.Api.ServicesConfiguration
{
    public static class ServicesConfigurationExtension
    {
        public static void AddInjectionServices(this IServiceCollection services)
        {
            services.AddScoped<ISkillsService, SkillsService>();
            services.AddScoped<IEmailSender, EmailSenderService>();
            services.AddScoped<IClientJobService, ClientJobsService>();
            services.AddScoped<IClientJobHandler, ClientJobHandler>();
            services.AddScoped<IFavoritesService, FavoritesService>();
            services.AddScoped<IWorkerJobService, WorkerJobService>();
            services.AddScoped<IStripePaymentService, StripePaymentService>();
            services.AddScoped<IUserSecurityCheckService, UserSecurityCheckService>();
            services.AddScoped<IPlanService, PlanService>();
            services.AddScoped<ITransactionsTypeService, TransactionsTypeService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IUserPortifolioService, UserPortifolioService>();
            services.AddScoped<IWorkerService, WorkerService>();
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<IHomeService, HomeService>();
            services.AddScoped<IHireService, HireService>();
            services.AddScoped<IGlobalJobsService, GlobalJobsService>();
            services.AddScoped<IPricingService, PricingService>();
            services.AddScoped<ICustomLogService, CustomLogService>();
            services.AddScoped<IUserRoleService, UserRoleService>();
            services.AddScoped<IContractService, ContractService>();
            services.AddScoped<IContractsInvoicesService, ContractsInvoicesService>();
            services.AddScoped<IContractsSecuredService, ContractsSecuredService>();
        }
    }
}
