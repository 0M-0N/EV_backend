using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.HireModels;
using GoHireNow.Models.JobsModels;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Service.HireServices
{
    public class GlobalJobsService : IGlobalJobsService
    {
        private readonly IUserRoleService _userRoleService;
        public GlobalJobsService(IUserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }
        public async Task<List<GlobalJobCategoriesListModel>> GetGlobalJobTitles()
        {
            using (var _context = new GoHireNowContext())
            {
                var titlesWithCategories = await _context.SpGetGlobalJobTitlesWithCategories.FromSql("SpGetGlobalJobTitlesWithCategories").ToListAsync();
                var jobCategories = titlesWithCategories.GroupBy(o => o.JobCategoryId).ToList();
                List<GlobalJobCategoriesListModel> jobCatergoriesList = new List<GlobalJobCategoriesListModel>();
                foreach (var i in jobCategories)
                {
                    GlobalJobCategoriesListModel jobCatergory = new GlobalJobCategoriesListModel();
                    var categories = titlesWithCategories.Where(o => o.JobCategoryId == i.Key).ToList();
                    jobCatergory.Id = categories.Select(x => x.JobCategoryId).FirstOrDefault();
                    jobCatergory.Name = categories.Select(x => x.JobCategoryName).FirstOrDefault();
                    jobCatergory.JobTitles = categories.Select(x => new GlobalJobTitleListModel()
                    {
                        Id = x.JobId,
                        FriendlyUrl = x.FriendlyUrl,
                        Name = x.JobName,
                    }).ToList();
                    jobCatergoriesList.Add(jobCatergory);
                }
                return jobCatergoriesList;
            }
        }
        public async Task<JobTitleRelatedGlobalJobsModel> GetJobTitleRelatedJobs(int jobTitleId, string userId, int roleId)
        {
            
            using (var _context = new GoHireNowContext())
            {
                var jobs = await _context.spGetJobTitleRelatedJobs.FromSql("spGetJobTitleRelatedJobs @JobTitleId = {0}", jobTitleId).ToListAsync();
                
                JobTitleRelatedGlobalJobsModel jobsModel = new JobTitleRelatedGlobalJobsModel();
                if (jobs.Count() > 0)
                {
                    var jobTitle = jobs.FirstOrDefault();
                    jobsModel.JobBigTitle = jobTitle.JobBigTitle;
                    jobsModel.JobDescripton = jobTitle.JobDescripton;
                    jobsModel.JobTitle = jobTitle.JobTitle;
                    jobsModel.Jobs = jobs.OrderByDescending(o => o.LastLoginTime)
                           //.Skip(skip).Take(model.size)
                           .Select(j => new JobTitleRelatedGlobalJobsListModel()
                           {

                               Id = j.Id,
                               Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(j.Title) ? j.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, j.ClientId).Result) : null : j.Title.ReplaceGlobalJobTitleInformation(),
                               StatusId = j.JobStatusId,
                               Status = (j.JobStatusId > 0 && j.JobStatusId <= 3) ? j.JobStatusId.ToJobStatuseName() : "",
                               TypeId = j.JobTypeId,
                               Type = j.JobTypeId.ToJobTypeName(),
                               SalaryTypeId = j.SalaryTypeId,
                               SalaryType = j.SalaryTypeId.ToSalaryTypeName(),
                               Salary = j.Salary,
                               ActiveDate = j.ActiveDate,
                               ProfilePicturePath = !string.IsNullOrEmpty(j.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{j.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}",
                               Skills = LookupService.GetSkillsById(j.JobSkills.Split(',').Select(int.Parse).ToList()),
                               Client = new JobClientSummaryResponse()
                               {
                                   Id = j.ClientId,
                                   CountryName = j.CountryId.HasValue ? j.CountryId.Value.ToCountryName() : string.Empty,
                                   UserUniqueId = j.UserUniqueId,
                                   CompanyName = j.CompanyName,
                                   Logo = j.ProfilePicture,
                                   MemberSince = j.RegistrationDate,
                                   LastLoginDate = j.LastLoginTime,
                                   ProfilePicturePath = !string.IsNullOrEmpty(j.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{j.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
                               }
                           }).ToList();

                }
                else
                {
                    var globalJobTitle = await _context.GlobalJobTitles.Where(o => o.id == jobTitleId).FirstOrDefaultAsync();
                    jobsModel.JobBigTitle = globalJobTitle.BigTitle;
                    jobsModel.JobDescripton = globalJobTitle.Description;
                    jobsModel.JobTitle = globalJobTitle.Title;
                }
                return jobsModel;
            }
        }
        public async Task<JobDetailForWorkerResponse> GetJob(int jobId, string rootPath, string userId, int roleId)
        {
            Jobs job;
            using (var _context = new GoHireNowContext())
            {
                job = await _context.Jobs.Include(o => o.User)
                    .Include(x => x.JobSkills)
                    .Include(x => x.JobAttachments)
                    .FirstOrDefaultAsync(x => x.Id == jobId && x.JobStatusId == (int)JobStatusEnum.Published);
            }
            var res = new JobDetailForWorkerResponse();
            if (job != null)
            {

                List<int> skillIds = job.JobSkills.Select(x => x.SkillId).ToList();
                int i = 1;
                res.Id = job.Id;
                res.UserId = job.UserId; //job.UserId; // Simple user id to use on the front end message send. Client id is also in the client section
                if (userId != null)
                {
                    res.Title = !string.IsNullOrEmpty(job.Title) ? job.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, job.UserId).Result) : null;
                    res.Description = !string.IsNullOrEmpty(job.Description) ? job.Description.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, job.UserId).Result) : null;
                }
                else
                {
                    res.Title = job.Title.ReplaceGlobalJobTitleInformation();
                    res.Description = job.Description.ReplaceGlobalJobTitleInformation();
                }
                res.CreateDate = job.CreateDate;
                res.Type = job.JobTypeId.ToJobTypeName();
                res.JobTypeId = job.JobTypeId;
                res.SalaryTypeId = job.SalaryTypeId;
                res.SalaryType = job.SalaryTypeId.ToSalaryTypeName();
                res.Salary = Convert.ToString(job.Salary);
                res.Status = (job.JobStatusId > 0 && job.JobStatusId <= 3) ? job.JobStatusId.ToJobStatuseName() : "";
                res.JobSkills = job.JobSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList();
                res.Client = new JobClientSummaryResponse()
                {
                    Id = job.User.Id,
                    UserUniqueId = job.User.UserUniqueId,
                    CompanyName = job.User.Company,
                    Logo = job.User.ProfilePicture,
                    MemberSince = job.User.RegistrationDate,
                    LastLoginDate = job.User.LastLoginTime,
                    ProfilePicturePath = !string.IsNullOrEmpty(job.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{job.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
                };
                res.Attachments = job.JobAttachments.Any()
                                                ?
                                                job.JobAttachments.Where(x => x.IsDeleted == false && x.IsActive == true)
                                                .Select(x => new AttachmentResponse()
                                                {
                                                    Id = x.Id,
                                                    FileName = x.Title,
                                                    Counter = i++,
                                                    FilePath = $"{LookupService.FilePaths.JobAttachmentUrl}{job.Id}/{x.AttachedFile}",
                                                    Icon = LookupService.GetFileImage(Path.GetExtension(x.AttachedFile), rootPath) == "img"
                                                                        ? $"{LookupService.FilePaths.JobAttachmentUrl}{job.Id}/{x.AttachedFile}"
                                                                        : LookupService.GetFileImage(Path.GetExtension(x.AttachedFile), rootPath),
                                                    FileExtension = Path.GetExtension(x.AttachedFile).Replace(".", "")
                                                })
                                                .ToList()
                                                :
                                                new List<AttachmentResponse>();
                res.ProfilePicturePath = !string.IsNullOrEmpty(job.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{job.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}";
                res.IsDeleted = job.IsDeleted;

                return res;
            }
            return null;
        }
    }
}