using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ConfigurationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace GoHireNow.Service.CommonServices
{
    public static class LookupService
    {
        public static void Load(FilePaths filepaths)
        {

            PageSize = new PageSizeSettings()
            {
                Applicants = 6,
                ClientJobs = 6,
                FavoriteJobs = 6,
                FavoriteWorkers = 6,
                RelatedJobs = 6,
                RelatedWorkers = 8,
                SearchJobs = 10,
                SearchWorkers = 10,
                Transactions = 10,
                FreeJobPosts = 5
            };

            FilePaths = new FilePathSettings()
            {
                MessageUrl = "https://www.evirtualassistants.com/messages",
                EmailTemplatePath = "https://devapiv1.evirtualassistants.com/EmailTemplate/",
                ProfilePictureUrl = "https://devapiv1.evirtualassistants.com/resources/profile-pictures/",
                PortfolioFileUrl = "https://devapiv1.evirtualassistants.com/resources/portfolio/",
                JobAttachmentUrl = "https://devapiv1.evirtualassistants.com/resources/Jobs/",
                WorkerResumeUrl = "https://devapiv1.evirtualassistants.com/resources/resume/",
                AcademyUrl = "https://devapiv1.evirtualassistants.com/resources/academy/",
                ClientDefaultImageFilePath = "https://devapiv1.evirtualassistants.com/resources/profile-pictures/client-default-icon.png",
                WorkerDefaultImageFilePath = "https://devapiv1.evirtualassistants.com/resources/profile-pictures/worker-default-icon.png"
            };

            using (var _context = new GoHireNowContext())
            {
                Countries = _context.Countries.Select(x => new CountryResponse() { Id = x.Id, Name = x.Name })
                    .OrderBy(x => x.Name).ToList();

                GlobalPlans = _context.GlobalPlans.Select(x => new GlobalPlanResponse() { Id = x.Id, Name = x.Name }).ToList();

                GlobalSkills = _context.GlobalSkills
                    .Where(s => s.IsActive == true && s.IsDeleted == false)
                    .Select(x => new SkillResponse()
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).ToList();

                GlobalJobTitlesSkills = _context.GlobalJobTitlesSkills
                    .Select(x => new SkillResponse()
                    {
                        Id = x.GlobalSkillId,
                        JobTitleId = x.JobTitleId,
                        GlobalSkillId = x.GlobalSkillId,
                        Name = x.Name,
                        FriendlyUrl = x.FriendlyUrl
                    }).Distinct().ToList();

                JobStatuses = _context.JobStatuses.Select(x => new JobStatusResponse() { Id = x.Id, Name = x.Name }).ToList();

                JobTypes = _context.JobTypes.Select(x => new JobTypeResponse() { Id = x.Id, Name = x.Name }).ToList();

                SalaryTypes = _context.SalaryTypes.Select(x => new SalaryTypeResponse() { Id = x.Id, Name = x.Name }).ToList();

                UserTypes = new List<UserTypeResponse>() {
                new UserTypeResponse() { Id = 1 , Name = "Client"},
                new UserTypeResponse() { Id = 2 , Name = "Worker"}
            };

                GlobalPlanDetails = _context.GlobalPlans.Select(x => new GlobalPlanDetailResponse()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Price,
                    JobPosts = x.JobPosts,
                    ViewApplicants = x.ViewApplicants,
                    AddFavorites = x.AddFavorites,
                    ContactApplicants = x.ContactApplicants,
                    Hire = x.Hire,
                    MaxApplicants = x.MaxApplicants,
                    MaxDays = x.MaxDays,
                    AccessId = x.AccessId,
                    IsActive = x.IsActive,
                    CreateDate = x.CreateDate,
                    ModifiedDate = x.ModifiedDate,
                    MaxContacts = x.MaxContacts,
                    Dedicated = x.Dedicated,


                }).ToList();
            }
        }
        public static IEnumerable<CountryResponse> Countries { get; set; }
        public static IEnumerable<GlobalPlanResponse> GlobalPlans { get; set; }
        public static IEnumerable<SkillResponse> GlobalSkills { get; set; }
        public static IEnumerable<SkillResponse> GlobalJobTitlesSkills { get; set; }
        public static IEnumerable<JobStatusResponse> JobStatuses { get; set; }
        public static IEnumerable<JobTypeResponse> JobTypes { get; set; }
        public static IEnumerable<SalaryTypeResponse> SalaryTypes { get; set; }
        public static IEnumerable<UserTypeResponse> UserTypes { get; set; }
        public static IEnumerable<GlobalPlanDetailResponse> GlobalPlanDetails { get; set; }


        public static List<SkillResponse> GetSkillsById(List<int> skillIds)
        {
            return LookupService.GlobalJobTitlesSkills
                .Where(x => skillIds.Contains(x.GlobalSkillId))
                .Select(s => new SkillResponse()
                {
                    Id = s.GlobalSkillId,
                    JobTitleId = s.JobTitleId,
                    GlobalSkillId = s.GlobalSkillId,
                    Name = s.Name,
                    FriendlyUrl = s.FriendlyUrl
                }).ToList();

        }
        //public static List<SkillResponse> GetSkillsById2(List<int> skillIds)
        //{
        //    return LookupService.GlobalSkills
        //        .Where(x => skillIds.Contains(x.Id))
        //        .Select(s => new SkillResponse()
        //        {
        //            Id = s.Id,
        //            Name = s.Name,
        //            JobTitleId = s.JobTitleId,
        //            GlobalSkillId = s.GlobalSkillId,

        //        }).ToList();
        //}
        public static PageSizeSettings PageSize { get; set; }
        public static FilePathSettings FilePaths { get; set; }
        public static string GetFileImage(string ext, string rootPath)
        {
            ext = ext.Replace(".", "");
            var path = "";
            var images = new List<string> { "jpg", "jpeg", "png", "gif" };
            //var ext = Path.GetExtension(name);
            var img = images.FirstOrDefault(x => x == ext);
            if (img != null)
            {
                path = "img";
            }
            else
            {
                switch (ext)
                {
                    case "doc":
                        path = $"{rootPath}/images/word.png";
                        break;
                    case "docx":
                        path = $"{rootPath}/images/word.png";
                        break;
                    case "ppt":
                        path = $"{rootPath}/images/ppt.png";
                        break;
                    case "pptx":
                        path = $"{rootPath}/images/ppt.png";
                        break;
                    case "xls":
                        path = $"{rootPath}/images/excel.png";
                        break;
                    case "xlsx":
                        path = $"{rootPath}/images/excel.png";
                        break;
                    case "csv":
                        path = $"{rootPath}/images/excel.png";
                        break;
                    case "txt":
                        path = $"{rootPath}/images/txt.png";
                        break;
                    case "pdf":
                        path = $"{rootPath}/images/pdf.png";
                        break;
                    default:
                        break;
                }
            }

            return path;
        }

        internal static string GetFileImage(string v, object rootPath)
        {
            throw new NotImplementedException();
        }
    }
}