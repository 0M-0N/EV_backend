using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using GoHireNow.Service.Interfaces;
using System;
using Slack.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace GoHireNow.Service.CommonServices
{
    public class CustomLogService : ICustomLogService
    {
        private IConfiguration _configuration { get; }
        public CustomLogService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public CustomLogService()
        {
        }

        public void LogError(LogErrorRequest error)
        {
            GlobalErrors globalError = new GlobalErrors()
            {
                CreateDate = DateTime.UtcNow,
                ErrorMessage = error.ErrorMessage,
                ErrorUrl = string.IsNullOrEmpty(error.ErrorUrl) ? null : error.ErrorUrl,
                IsDeleted = false,
                UserId = string.IsNullOrEmpty(error.UserId) ? null : error.UserId
            };

            using (var _context = new GoHireNowContext())
            {
                _context.GlobalErrors.Add(globalError);
                _context.SaveChanges();
            }

            var url = " https://hooks.slack.com/services/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";

            var slackClient = new SlackClient(url);

            if (_configuration.GetValue<string>("APIDomain") == "https://apiv1.evirtualassistants.com")
            {
                var slackMessage = new SlackMessage
                {
                    Channel = "#eva-bugs",
                    Text = error.ErrorMessage,
                    IconEmoji = Emoji.Poop,
                    Username = "webhookbot"
                };

                slackClient.Post(slackMessage);
            }
        }

        public void LogSupport(LogSupportRequest request)
        {
            var url = "https://hooks.slack.com/services/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";

            var slackClient = new SlackClient(url);

            if (_configuration.GetValue<string>("APIDomain") == "https://apiv1.evirtualassistants.com")
            {
                var slackMessage = new SlackMessage
                {
                    Channel = "#eva-support",
                    Text = request.Text,
                    IconEmoji = Emoji.Poop,
                    Username = "webhookbot"
                };

                slackClient.Post(slackMessage);
            }
        }

        public void LogHRSupport(LogSupportRequest request)
        {
            var url = "https://hooks.slack.com/services/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";

            var slackClient = new SlackClient(url);

            if (_configuration.GetValue<string>("APIDomain") == "https://apiv1.evirtualassistants.com")
            {
                var slackMessage = new SlackMessage
                {
                    Channel = "#eva-hr",
                    Text = request.Text,
                    IconEmoji = Emoji.Poop,
                    Username = "webhookbot"
                };

                slackClient.Post(slackMessage);
            }
        }

        public void LogPayout(LogSupportRequest request)
        {
            var url = "https://hooks.slack.com/services/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";

            var slackClient = new SlackClient(url);

            if (_configuration.GetValue<string>("APIDomain") == "https://apiv1.evirtualassistants.com")
            {
                var slackMessage = new SlackMessage
                {
                    Channel = "#eva-revenues",
                    Text = request.Text,
                    IconEmoji = Emoji.Poop,
                    Username = "webhookbot"
                };

                slackClient.Post(slackMessage);
            }
        }

        public async Task<bool> ValidateFiles(IFormFileCollection files)
        {
            if (files.Any())
            {
                foreach (var file in files)
                {
                    var supportedTypes = new[] {
                        "DOC","DOCX","HTML","HTM","ODT","PDF","XLS","XLSX","ODS","PPT","PPTX","TXT","JPG","JPEG","GIF","PNG","BMP","TXT","RTF","ODP","ODS","TIFF", "MP3", "MP4",
                        "doc","docx","html","htm","odt","pdf","xls","xlsx","ods","ppt","pptx","txt","jpg","jpeg","gif","png","bmp","txt","rtf","odp","ods","tiff", "mp3", "mp4",
                    };
                    var fileExtension = System.IO.Path.GetExtension(file?.FileName);
                    var fileExt = !string.IsNullOrEmpty(fileExtension) ? fileExtension.Substring(1) : string.Empty;

                    if (!supportedTypes.Contains(fileExt))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
