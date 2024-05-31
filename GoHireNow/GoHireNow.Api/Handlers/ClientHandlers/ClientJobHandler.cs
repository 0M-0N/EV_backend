using GoHireNow.Api.Handlers.ClientHandlers.Interfaces;
using GoHireNow.Database;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Api.Handlers.ClientHandlers
{
    public class ClientJobHandler : IClientJobHandler
    {
        private readonly IClientJobService _clientJobService;
        private readonly IUserRoleService _userRoleService;
        public ClientJobHandler(IClientJobService clientJobService, IUserRoleService userRoleService)
        {
            _clientJobService = clientJobService;
            _userRoleService = userRoleService;
        }

        public async Task<int> PostJob(string userId, PostJobRequest model)
        {
            //TODO: Check if client is eligible to post a job
            Jobs job = MapJob(userId, model);
            return await _clientJobService.AddJob(job);
        }

        private Jobs MapJob(string userId, PostJobRequest model)
        {
            var job = new Jobs();
            try
            {
                job.UserId = userId;
                job.JobTypeId = model.JobTypeId;
                job.Duration = 10; //TODO: get from plan logic
                job.Salary = model.Salary;
                job.SalaryTypeId = model.SalaryTypeId;
                job.Title = model.Title;
                job.Description = model.Description;
                job.IsActive = true;
                job.IsDashboard = 1;
                job.IsEmail = model.isEmail ? 1 : 0;
                job.JobStatusId = _userRoleService.GetClientPlan(userId).Result == true ? (int)JobStatusEnum.Published : (int)JobStatusEnum.NotApproved;
                job.CreateDate = DateTime.UtcNow;
                job.ActiveDate = DateTime.UtcNow;
                job.CloseDate = DateTime.UtcNow.AddDays(GetJobCloseDays(userId));
                job.IsDeleted = false;
            }
            catch (Exception)
            {
                throw new CustomException(500, "Error mapping job request with required model");
            }

            job.JobSkills = MapJobSkills(model.JobSkillIds);

            return job;

        }

        private List<JobSkills> MapJobSkills(List<int> skillIds)
        {
            var skills = LookupService.GlobalSkills.Where(x => skillIds.Contains(x.Id))
                .Select(j => new JobSkills()
                {
                    SkillId = j.Id,
                    CreateDate = DateTime.UtcNow
                }).ToList();

            if (skills.Count == 0 || skills == null)
                throw new CustomException(400, "Invalid skillIds");

            return skills;
        }

        //TODO: with client plan logic
        private int GetJobCloseDays(string userId)
        {
            return 10;
        }
    }
}
