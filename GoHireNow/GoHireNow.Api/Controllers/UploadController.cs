using GoHireNow.Api.Filters;
using GoHireNow.Database;
using GoHireNow.Identity.Data;
using GoHireNow.Models.CommonModels;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GoHireNow.Api.Controllers
{
    [Route("upload")]
    [ApiController]
    [CustomExceptionFilter]
    public class UploadController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICustomLogService _customLogService;
        private readonly IClientService _clientCommonService;
        private readonly IUserPortifolioService _userPortifolioService;
        public UploadController(UserManager<ApplicationUser> userManager,
            IClientService clientCommonService,
            ICustomLogService customLogService,
            IUserPortifolioService userPortifolioService)
        {
            _customLogService = customLogService;
            _userManager = userManager;
            _clientCommonService = clientCommonService;
            _userPortifolioService = userPortifolioService;
        }

        [HttpPost, DisableRequestSizeLimit]
        [Authorize]
        [Route("file")]
        public async Task<IActionResult> File([FromForm]IFormFile file, [FromQuery]int countryId, [FromQuery]string user_email, [FromQuery]bool isProfilePicture)
        {
            LogErrorRequest error;
            try
            {
                var supportedTypes = new[] {
                    "DOC","DOCX","HTML","HTM","ODT","PDF","XLS","XLSX","ODS","PPT","PPTX","TXT","JPG","JPEG","GIF","PNG","BMP","TXT","RTF","ODP","ODS","TIFF", "MP3", "MP4",
                    "doc","docx","html","htm","odt","pdf","xls","xlsx","ods","ppt","pptx","txt","jpg","jpeg","gif","png","bmp","txt","rtf","odp","ods","tiff", "mp3", "mp4",
                };
                var fileExtension = System.IO.Path.GetExtension(file?.FileName);
                var fileExt = !string.IsNullOrEmpty(fileExtension) ? fileExtension.Substring(1) : string.Empty;
                
                if (!supportedTypes.Contains(fileExt))
                {
                    return BadRequest();
                }

                // pattern for resource folders /resources/1/44/51d523f0-4f47-487d-b7c8-53924cd0bcc9/7668867_resume.xls
                var user = _userManager.Users.FirstOrDefault(u => u.Email == user_email);
                if (file.Length > 0)
                {
                    CreatePathAndDirectory(file, countryId, user, out string fullPath, out string dbPath);

                    DeleteExistingFiles(isProfilePicture, user);

                    await UploadFileAndSaveIntoDatabase(file, isProfilePicture, user, fullPath, dbPath);

                    return Ok(dbPath);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/upload/file",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Authorize]
        [Route("portfolio")]
        public async Task<IActionResult> Porfolio([FromForm]List<IFormFile> files, [FromQuery]int countryId, [FromQuery]string userId)
        {
            LogErrorRequest error;
            try
            {
                // pattern for resource folders /resources/1/44/51d523f0-4f47-487d-b7c8-53924cd0bcc9/7668867_resume.xls
                var user = await _userManager.FindByIdAsync(userId);
                if (files.Count > 0)
                {
                    var listOfDbPaths = new List<string>();
                    foreach (var file in files)
                    {
                        CreatePathAndDirectory(file, countryId, user, out string fullPath, out string dbPath);

                        //DeleteExistingPorfolios(user);

                        var userPortifolio = new UserPortfolios
                        {
                            Link = dbPath,
                            CreateDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            UserId = user.Id,
                            Title = file.Name,
                            Description = file.FileName
                        };
                        _userPortifolioService.AddUserPortifolio(userPortifolio);
                        //listOfDbPaths.Add(dbPath);
                    }
                    //This is moved to the for loop to add direct into the User Portifolio table
                    //user.UserPortfolios = string.Join(" | ", listOfDbPaths);
                    await _userManager.UpdateAsync(user);
                    return Ok(listOfDbPaths);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/upload/portfolio",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Route("validatefile")]
        public IActionResult ValidateFile(IFormFile file)
        {
            LogErrorRequest error;
            string ErrorMessage = string.Empty;
            try
            {
                var supportedTypes = new[] {
                "DOC","DOCX","HTML","HTM","ODT","PDF","XLS","XLSX","ODS","PPT","PPTX","TXT","JPG","JPEG","GIF","PNG","BMP","TXT","RTF","ODP","ODS","TIFF", "MP3", "MP4",
                "doc","docx","html","htm","odt","pdf","xls","xlsx","ods","ppt","pptx","txt","jpg","jpeg","gif","png","bmp","txt","rtf","odp","ods","tiff", "mp3", "mp4",
                };
                var fileExtension = System.IO.Path.GetExtension(file?.FileName);
                var fileExt = !string.IsNullOrEmpty(fileExtension) ? fileExtension.Substring(1) : string.Empty;
                
                if (!supportedTypes.Contains(fileExt))
                {
                    ErrorMessage = "File Extension Is Invalid - DOC|DOCX|HTML|HTM|ODT|PDF|XLS|XLSX|ODS|PPT|PPTX|TXT|JPG|JPEG|GIF|PNG|BMP|TXT|RTF|ODP|ODS|TIFF|MP3|MP4 Files are supported";
                    return Ok(new { result = false, message = ErrorMessage });
                }
                else
                {
                    ErrorMessage = "File Is Successfully Uploaded";
                    return Ok(new { result = true, message = ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/upload/validatefile",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                ErrorMessage = "Upload Container Should Not Be Empty or Contact Admin";
                return Ok(new { result = false, message = ErrorMessage });
            }
        }

        //[HttpPost, DisableRequestSizeLimit]
        //[Authorize]
        //[Route("jobattachments")]
        //public async Task<IActionResult> JobAttachments([FromForm]List<IFormFile> files, [FromQuery]int countryId, [FromQuery]string userId, [FromQuery]int jobId)
        //{
        //    try
        //    {
        //        // pattern for resource folders /resources/1/44/51d523f0-4f47-487d-b7c8-53924cd0bcc9/7668867_resume.xls
        //        var user = await _userManager.FindByIdAsync(userId);
        //        if (files.Count > 0)
        //        {
        //            var listOfDbPaths = new List<string>();
        //            foreach (var file in files)
        //            {
        //                CreatePathAndDirectory(file, countryId, user, out string fullPath, out string dbPath);
        //                listOfDbPaths.Add(dbPath);
        //            }
        //            foreach (var item in listOfDbPaths)
        //            {
        //                var jobAttachment = new JobAttachments
        //                {
        //                    AttachedFile = item,
        //                    IsDeleted = false,
        //                    JobId = jobId,
        //                    AttachmentTypeId = 1 //TODO : Ask What are types
        //                };
        //                _clientCommonService.AddJobAttachment(jobAttachment);
        //            }
        //            return Ok(listOfDbPaths);
        //        }
        //        else
        //        {
        //            return BadRequest();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Internal server error");
        //    }
        //}

        #region Local functions

        private async Task UploadFileAndSaveIntoDatabase(IFormFile file, bool isProfilePicture, ApplicationUser user, string fullPath, string dbPath)
        {
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            if (isProfilePicture)
            {
                if (user.UserType == 2)
                    user.ProfilePicture = dbPath;
                else
                    user.ProfilePicture = dbPath;
            }
            else
                user.UserResume = dbPath;

            await _userManager.UpdateAsync(user);
        }

        private void DeleteExistingFiles(bool isProfilePicture, ApplicationUser user)
        {
            var fileToDelete = string.Empty;
            if (isProfilePicture)
                fileToDelete = user.ProfilePicture;
            else
                fileToDelete = user.UserResume;
            if (!string.IsNullOrEmpty(fileToDelete))
            {
                if (System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), fileToDelete)))
                {
                    System.IO.File.Delete(fileToDelete);
                }
            }
        }

        private void DeleteExistingPorfolios(string userId)
        {
            //var portfolioFiles = user.UserPortfolios.Split(" | ");
            var portfolioFiles = _userPortifolioService.GetUserPortifolios(userId);
            foreach (var item in portfolioFiles)
            {
                if (!string.IsNullOrEmpty(item.Link))
                {
                    if (System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), item.Link)))
                    {
                        System.IO.File.Delete(item.Link);
                    }
                }
            }
            _userPortifolioService.DeleteUserPortifolios(userId);

        }

        private static void CreatePathAndDirectory(IFormFile file, int countryId, ApplicationUser user, out string fullPath, out string dbPath)
        {
            var folderName = Path.Combine("Resources", countryId.ToString(), new Random().Next(0, 100).ToString(), user.Id.ToString());
            var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            var fileName = new Random().Next(0, 100000).ToString() + ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            fullPath = Path.Combine(pathToSave, fileName);
            dbPath = Path.Combine(folderName, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        }
        #endregion
    }
}
