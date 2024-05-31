using GoHireNow.Api.Controllers;
using GoHireNow.Api.ServicesConfiguration;
using GoHireNow.Identity.Data;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ConfigurationModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.EmailServices;
using GoHireNow.Service.Interfaces;
using GoHireNow.Service.WorkerServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Stripe;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Reflection;
using System.Text;

namespace GoHireNow.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection"), opt => opt.EnableRetryOnFailure()));

            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionScopedJobFactory();

                q.AddJob<WorkerHourJob>(j => j.WithIdentity("WorkerHourJob").Build());
                q.AddTrigger(t => t
                    .WithIdentity("WorkerHourJobTrigger")
                    .ForJob("WorkerHourJob")
                    .WithCronSchedule("0 30 22 ? * SAT"));

                q.AddJob<PendingContractJob>(j => j.WithIdentity("PendingContractJob").Build());
                q.AddTrigger(t => t
                    .WithIdentity("PendingContractJobTrigger")
                    .ForJob("PendingContractJob")
                    .WithCronSchedule("0 0 21 ? * *"));

                q.AddJob<ReleaseJob>(j => j.WithIdentity("ReleaseJob").Build());
                q.AddTrigger(t => t
                    .WithIdentity("ReleaseJobTrigger")
                    .ForJob("ReleaseJob")
                    .WithCronSchedule("0 30 0 ? * MON"));

                q.AddJob<HRWorkerHoursJob>(j => j.WithIdentity("HRWorkerHoursJob").Build());
                q.AddTrigger(t => t
                    .WithIdentity("HRWorkerHoursJobTrigger")
                    .ForJob("HRWorkerHoursJob")
                    .WithCronSchedule("0 0 2 ? * MON"));

                q.AddJob<AutoWithdrawJob>(j => j.WithIdentity("AutoWithdrawJob").Build());
                q.AddTrigger(t => t
                    .WithIdentity("AutoWithdrawJobTrigger")
                    .ForJob("AutoWithdrawJob")
                    .WithCronSchedule("0 0 23 ? * WED"));

                q.AddJob<AutoChargeForHRJob>(j => j.WithIdentity("AutoChargeForHRJob").Build());
                q.AddTrigger(t => t
                    .WithIdentity("AutoChargeForHRJobTrigger")
                    .ForJob("AutoChargeForHRJob")
                    .WithCronSchedule("0 0 22 ? * *"));

                q.AddJob<ActionJob>(j => j.WithIdentity("ActionJob").Build());
                q.AddTrigger(t => t
                    .WithIdentity("ActionJobTrigger")
                    .ForJob("ActionJob")
                    .WithCronSchedule("0 * * ? * *"));
            });
            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddRoles<IdentityRole>()
            .AddRoleManager<RoleManager<IdentityRole>>()
            .AddDefaultUI(UIFramework.Bootstrap4)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            });

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromMinutes(2880));

            //services.AddSingleton<GoHireNowContext, GoHireNowContext>();
            services.AddInjectionServices();

            services.Configure<FormOptions>(options =>
            {
                options.ValueCountLimit = int.MaxValue;
            });
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = Configuration["JwtIssuer"],
                        ValidAudience = Configuration["JwtIssuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtKey"])),
                        ClockSkew = TimeSpan.Zero // remove delay of token when expire
                    };
                });
            services.AddAuthorization();

            // Add service and create Policy with options
            services.AddCors();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));
            services.Configure<PusherSettings>(Configuration.GetSection("PusherSettings"));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddMvc().AddApplicationPart(Assembly.Load(new AssemblyName("GoHireNow.Identity")));
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Go Hire Now API", Version = "1" });
            });
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IEmailRecurringJobService, EmailRecurringJobService>();
            services.AddScoped<ContractController>();
            services.AddScoped<PaymentController>();
            services.AddScoped<MessagesController>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ApplicationDbContext dbContext)
        {
            StripeConfiguration.ApiKey = Configuration.GetSection("Stripe")["SecretKey"];

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCors("CorsPolicy");
            app.UseCors(builder => builder.WithOrigins("*")
                .AllowAnyHeader());
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Go Hire Now APIsss");
                c.RoutePrefix = "swagger/ui";
            });
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "Resources")),
                RequestPath = new PathString("/Resources")
            });
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "EmailTemplate")),
                RequestPath = new PathString("/EmailTemplate")
            });
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "EmailTemplateResources")),
                RequestPath = new PathString("/EmailTemplateResources")
            });

            app.UseAuthentication();

            app.UseHttpsRedirection();
            app.UseMvc();


            dbContext.Database.EnsureCreated();
            FilePaths filepaths = new FilePaths();
            filepaths.MessageUrl = Configuration.GetSection("FilePaths")["MessageUrl"];
            filepaths.EmailTemplatePath = Configuration.GetSection("FilePaths")["EmailTemplatePath"];
            filepaths.ProfilePictureUrl = Configuration.GetSection("FilePaths")["ProfilePictureUrl"];
            filepaths.PortfolioFileUrl = Configuration.GetSection("FilePaths")["PortfolioFileUrl"];
            filepaths.JobAttachmentUrl = Configuration.GetSection("FilePaths")["JobAttachmentUrl"];
            filepaths.WorkerResumeUrl = Configuration.GetSection("FilePaths")["WorkerResumeUrl"];
            filepaths.ClientDefaultImageFilePath = Configuration.GetSection("FilePaths")["ClientDefaultImageFilePath"];
            filepaths.WorkerDefaultImageFilePath = Configuration.GetSection("FilePaths")["WorkerDefaultImageFilePath"];
            LookupService.Load(filepaths);

        }
    }
}
