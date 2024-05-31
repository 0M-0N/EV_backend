using GoHireNow.Database;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.HireModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Service.HireServices
{
    public class HireService : IHireService
    {
        private readonly IUserRoleService _userRoleService;
        public HireService(IUserRoleService userRoleService)
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

        public async Task<List<WorkerSummaryForClientResponse>> GetRelatedWorkersOffline(string UserId, int size=1, int page=5)
        {
            int skip = page > 1 ? ((page - 1) * size) : 0;
            using (var _context = new GoHireNowContext())
            {

               // var Uid = await _context.AspNetUsers.Where(x => x.Id == UserId).Select(j => j.UserUniqueId).ToListAsync();

                //var skillIds = await _context.UserSkills.Where(x => x.UserId == UserId).Select(j => j.SkillId).Distinct().ToListAsync();

             //   WorkerSummaryForClientResponse workers = new WorkerSummaryForClientResponse();

                
                var workers = await (from ws in _context.AspNetUsers
                                     join us in _context.UserSkills on ws.Id equals us.UserId
                                     join sk in _context.GlobalSkills on us.SkillId equals sk.Id
                                     select ws).Distinct()
                                     //.Where(ws => ws.Id == UserId)
                                     .Where(o => o.IsDeleted == false)
                                     .Where(o => o.Rating > 3.9m)
                                     .OrderByDescending(x => x.LastLoginTime)
                                     .Skip(skip).Take(size)
                .Select(x => new WorkerSummaryForClientResponse()
                {
                    UserId = x.Id,
                    UserUniqueId = x.UserUniqueId,
                    CountryId = x.CountryId,
                    CountryName = x.CountryId.Value.ToCountryName(),
                    LastLoginDate = x.LastLoginTime,
                    Education = x.Education,
                    Experience = x.Experience,
                    featured = x.Featured,
                    rating = x.Rating,
                    Name = x.FullName,
                    Salary = x.UserSalary,
                    SalaryTypeId = (int)SalaryTypeEnum.Monthly,
                    //Title = !string.IsNullOrEmpty(x.UserTitle) ? x.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(UserId, roleId, x.Id).Result) : null,
                    Title = x.UserTitle,
                    Skills = LookupService.GetSkillsById(x.UserSkills.Select(s => s.SkillId).ToList()),
                    ProfilePicturePath = !string.IsNullOrEmpty(x.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{x.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}"
                }).ToListAsync();

                return workers;
            }
        }

        public async Task<WorkerSummaryForHireResponse> GetJobTitleRelatedWorkers(int jobTitleId, int size, int page, string userId, int roleId)
        {

            using (var _context = new GoHireNowContext())
            {
                size = size > 0 ? size : 10;
                int skip = page < 1 ? 0 : (page * size) - size;

                var workers = await _context.spGetJobTitleRelatedWorkers.FromSql("spGetJobTitleRelatedWorkers @JobTitleId = {0},@take = {1}, @skip = {2}", jobTitleId, size, skip).ToListAsync();
                var totalWorker = workers.Count();

                WorkerSummaryForHireResponse WorkersModel = new WorkerSummaryForHireResponse();
                if (workers.Count() > 0)
                {
                    var jobTitle = workers.FirstOrDefault();
                    WorkersModel.TotalWorkers = jobTitle.TotalWorkers;
                    WorkersModel.JobBigTitle = jobTitle.JobBigTitle;
                    WorkersModel.JobDescripton = jobTitle.JobDescripton;
                    WorkersModel.JobTitle = jobTitle.JobTitle;
                    WorkersModel.SEOText = jobTitle.SEOText;
                    WorkersModel.Video = jobTitle.Video;
                    WorkersModel.Workers = workers.OrderByDescending(o => o.rating)
                           //.Skip(skip).Take(model.size)
                           .Select(x => new WorkerSummaryForClientResponse()
                           {

                               UserId = x.Id,
                               UserUniqueId = x.UserUniqueId,
                               CountryId = x.CountryId,
                               CountryName = x.CountryId.Value.ToCountryName(),
                               LastLoginDate = x.LastLoginTime,
                               Education = x.Education,
                               Experience = x.Experience,
                               featured = x.featured,
                               rating = x.rating,
                               Name = x.FullName,
                               Salary = x.UserSalary,
                               SalaryTypeId = (int)SalaryTypeEnum.Monthly,
                               Availability = x.UserAvailiblity.ToAvailabilityType(),
                               Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(x.UserTitle) ? x.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, x.Id).Result) : null : x.UserTitle.ReplaceGlobalJobTitleInformation(),
                               Skills = LookupService.GetSkillsById(x.UserSkills.Split(',').Select(int.Parse).ToList()),
                               ProfilePicturePath = !string.IsNullOrEmpty(x.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{x.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}"
                           }).ToList();

                }
                else
                {
                    var globalJobTitle = await _context.GlobalJobTitles.Where(o => o.id == jobTitleId).FirstOrDefaultAsync();
                    WorkersModel.TotalWorkers = totalWorker;
                    WorkersModel.JobBigTitle = globalJobTitle.BigTitle;
                    WorkersModel.JobDescripton = globalJobTitle.Description;
                    WorkersModel.JobTitle = globalJobTitle.Title;
                }
                return WorkersModel;
            }
        }

        public async Task<List<JobTitleRelatedWorkersModel>> GetWorkerProfile(string userId,string log_UserId,int roleId)
        {
            using (var _context = new GoHireNowContext())
            {
                var worker = await _context.AspNetUsers.Where(o => o.Id == userId)
                .Select(x => new JobTitleRelatedWorkersModel()
                {
                    UserId = x.Id,
                    CountryId = x.CountryId,
                    CountryName = x.CountryId.Value.ToCountryName(),
                    LastLoginDate = x.LastLoginTime,
                    Name = x.FullName,
                    Salary = x.UserSalary,
                    SalaryTypeId = (int)SalaryTypeEnum.Monthly,
                    Title = !string.IsNullOrEmpty(log_UserId) ? !string.IsNullOrEmpty(x.UserTitle) ? x.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, x.Id).Result) : null : x.UserTitle.ReplaceGlobalJobTitleInformation(),
                    Skills = LookupService.GetSkillsById(x.UserSkills.Select(s => s.SkillId).ToList()),
                    ProfilePicturePath = !string.IsNullOrEmpty(x.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{x.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}"
                }).ToListAsync();
                return worker;
            }
        }


    }
}