using GoHireNow.Database.ComplexTypes;
using GoHireNow.Database.GoHireNowTools;
using GoHireNow.Database.GoHireNowTools.Models;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace GoHireNow.Service.EmailServices
{
    public class EmailRecurringJobService : IEmailRecurringJobService
    {
        const string connection = @"Server=72.52.250.220; Database=gohirenow_dev;User Id=db_ghn_dev; Password=8p5hDEuhxr86t;"; //leave prod
        SqlConnection _Con = null;
        SqlCommand _cmd = null;
        SqlDataReader rd = null;
        private readonly IUserRoleService _userRoleService;
        public EmailRecurringJobService(IUserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }

        public async void SendCandiatesToClient()
        {
            _Con = new SqlConnection(connection);
            _cmd = new SqlCommand("spGetRegisteredUsersLastWeekWithMatchingSkills", _Con);
            if (_Con.State == ConnectionState.Closed)//ConnectionState is enum its    comes under System.Data name space   
            {
                _Con.Open();
            }
            rd = _cmd.ExecuteReader();
            List<spGetRegisteredUsersLastWeekWithMatchingSkills> candidates = new List<spGetRegisteredUsersLastWeekWithMatchingSkills>();
            while (rd.Read())
            {
                var candidate = new spGetRegisteredUsersLastWeekWithMatchingSkills();
                candidate.ClientId = rd["ClientId"].ToString();
                candidate.WorkerId = rd["WorkerId"].ToString();
                candidate.WorkerLastLoginTime = Convert.ToDateTime(rd["WorkerLastLoginTime"].ToString());
                candidate.WorkerName = rd["WorkerName"].ToString();
                candidate.WorkerSkills = rd["WorkerSkills"].ToString();
                candidate.WorkerSalary = rd["WorkerSalary"].ToString();
                candidate.WorkerAvailability = rd["WorkerAvailability"].ToString();
                candidate.ClientEmail = rd["ClientEmail"].ToString();
                candidate.WorkerTitle = !string.IsNullOrEmpty(rd["WorkerTitle"].ToString()) ? rd["WorkerTitle"].ToString().ReplaceInformation((int)UserTypeEnum.Client, _userRoleService.TextFilterCondition(rd["ClientId"].ToString(), (int)UserTypeEnum.Client, rd["WorkerId"].ToString()).Result) : null;
                candidate.WorkerProfilePicture = rd["WorkerProfilePicture"].ToString();
                candidates.Add(candidate);
            }
            var clients = candidates.GroupBy(o => o.ClientId);
            string htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "NewWorkersForYou.html");
            foreach (var item in clients.ToList())
            {
                var workers = candidates.Where(o => o.ClientId.Contains(item.Key)).ToList();
                var clientEmail = candidates.Where(o => o.ClientId.Contains(item.Key)).Select(o => o.ClientEmail).FirstOrDefault();
                var clientId = candidates.Where(o => o.ClientId.Contains(item.Key)).Select(o => o.ClientId).FirstOrDefault();
                StringBuilder htmlString = new StringBuilder();
                htmlString.AppendLine("<table style=\"border-collapse: collapse;background-color: #fff;\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" width=\"90%\"  bgcolor=\"#fff\">");
                htmlString.AppendLine("    <tr style=\"background-color: #fff;\">");
                htmlString.AppendLine("      <td>");
                foreach (var worker in workers)
                {
                    worker.WorkerProfilePicture = !string.IsNullOrEmpty(worker.WorkerProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{worker.WorkerProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}";
                    worker.WorkerSalary = !string.IsNullOrEmpty(worker.WorkerSalary) ? "$" + worker.WorkerSalary + "/month" : "";
                    htmlString.AppendLine(" <tr style=\"background-color: #fff;\">");
                    htmlString.AppendLine("      <td>");
                    htmlString.AppendLine("       <tr class=\"table_row\" style=\"text-align:center; height: 128px; width: 70%\">");
                    htmlString.AppendLine("        <td class=\"right_td\" width=\"15%\" style=\"border: none\"></td>");
                    htmlString.AppendLine("        <td class=\"templateColumnContainer\" style=\"padding-left: 40px; width: 100px; border: 1px solid #DCDCDC;border-right: none\";>");
                    htmlString.AppendLine("            <a href='https://www.evirtualassistants.com/work-profile/" + worker.WorkerId + "'><img style=\"width: 85px;");
                    htmlString.AppendLine("                                height: 85px;");
                    htmlString.AppendLine("                                max-height: 85px;");
                    htmlString.AppendLine("                                border-radius: 50%;");
                    htmlString.AppendLine("                                min-height:85px;\" src=\"" + worker.WorkerProfilePicture + "\" width=\"85\" height=\"85\" alt=\"\" /></a>");
                    htmlString.AppendLine("        </td>");
                    htmlString.AppendLine("        <td class=\"templateColumnContainer\" style=\"");
                    htmlString.AppendLine("                    font-family: Lato, sans-serif;");
                    htmlString.AppendLine("                    font-size: 18px;");
                    htmlString.AppendLine("                    height: 100px;");
                    htmlString.AppendLine("                    padding-left: 40px;");
                    htmlString.AppendLine("                    text-align: left;");
                    htmlString.AppendLine("                    border: 1px solid #DCDCDC;");
                    htmlString.AppendLine("                    border-right: none;");
                    htmlString.AppendLine("                    border-left: none\">");
                    htmlString.AppendLine("            <div class=\"name_title\" style=\"color: #333333;font-family: 'Lato-Black',sans-serif;font-size: 18px;font-weight: 900;line-height: 18px;padding-bottom: 10px\"><a style=\"text-decoration:none;color:#333333 !important\" href='https://www.evirtualassistants.com/work-profile/" + worker.WorkerId + "'>" + worker.WorkerName + " </a></div>");
                    htmlString.AppendLine("            <div style=\"font-family: 'Lato-Regular',sans-serif;padding-bottom: 10px\">" + worker.WorkerTitle + "</div>");
                    htmlString.AppendLine("                    <div style=\"font-family: 'Lato-Regular',sans-serif;color: #ed7b18;\">" + worker.WorkerSkills + "</div>");
                    htmlString.AppendLine("        </td>");
                    htmlString.AppendLine("<td class=\"templateColumnContainer\" style=\"");
                    htmlString.AppendLine("                    text-align: right;");
                    htmlString.AppendLine("	font-family: Lato, sans-serif;");
                    htmlString.AppendLine("	font-size: 18px;");
                    htmlString.AppendLine("	padding-right: 60px;");
                    htmlString.AppendLine("	height: 100px;");
                    htmlString.AppendLine("	border: 1px solid #DCDCDC;");
                    htmlString.AppendLine("	border-left: none;");
                    htmlString.AppendLine("	vertical-align: top;\"");
                    htmlString.AppendLine("                >");
                    htmlString.AppendLine("                    <div class=\"weekly_rate\" style=\"color: #333333;font-family: 'Lato-Black',sans-serif;font-size: 18px;font-weight: 900;line-height: 18px;padding-bottom: 10px;vertical-align: top; margin-top: 20px;\">" + worker.WorkerSalary + "</div>");
                    htmlString.AppendLine("                    <div style=\"font-family: 'Lato-Regular',sans-serif;padding-bottom: 10px; vertical-align: top;\">" + worker.WorkerAvailability + "</div>");
                    htmlString.AppendLine("                </td>");
                    htmlString.AppendLine("        <td class=\"right_td\" width=\"15%\" style=\"border: none\"></td>");
                    htmlString.AppendLine("    </tr>");
                }
                htmlString.AppendLine("</td></tr></table>");
                htmlContent = htmlContent.Replace("[Workers]", htmlString.ToString());
                htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + clientEmail + "&code=" + clientId + "&type=1");

                using (var _toolsContext = new GoHireNowToolsContext())
                {
                    var sender = new mailer_sender();
                    sender.ms_custom_id = 0;
                    sender.ms_custom_type = 7;
                    sender.ms_date = DateTime.Now;
                    sender.ms_send_date = DateTime.Now;
                    sender.ms_email = clientEmail;
                    sender.ms_name = "";
                    sender.ms_subject = "New Candidates for your Job";
                    sender.ms_message = htmlContent;
                    sender.ms_from_email = "no-reply@evirtualassistants.com";
                    sender.ms_from_name = "eVirtualAssistants";
                    sender.ms_priority = 2;
                    sender.ms_issent = 0;
                    sender.ms_unsubscribe = Guid.NewGuid();

                    await _toolsContext.mailer_sender.AddAsync(sender);
                    await _toolsContext.SaveChangesAsync();
                }

                // using (MailMessage messageObj = new MailMessage
                // {
                //     IsBodyHtml = true,
                //     BodyEncoding = System.Text.Encoding.UTF8,
                //     From = new MailAddress("no-reply@evirtualassistants.com", "eVirtualAssistants")
                // })
                // {
                    
                //     htmlContent = htmlContent.Replace("[Workers]", htmlString.ToString());
                //     htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + clientEmail + "&code=" + clientId + "&type=1");
                //     messageObj.To.Add(new MailAddress(clientEmail));
                //     messageObj.Subject = "New Candidates for your Job";
                //     messageObj.Body = htmlContent;
                //     using (var client = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587))
                //     {
                //         client.Credentials =
                //             new NetworkCredential("AKIAIA5SSI4EFREFBKVQ", "AjeA3B59Qu1048gPkxN+xD5nw1uxduuYsVaojohTUa0A");
                //         client.EnableSsl = true;
                //         try
                //         {
                //             client.Send(messageObj);
                //         }
                //         catch (Exception ex)
                //         {

                //         }
                //     }
                // }
            }


        }

        public async void SendJobsToWorker()
        {
            _Con = new SqlConnection(connection);
            _cmd = new SqlCommand("spGetRecentJobsLastDayWithMatchingSkills", _Con);
            if (_Con.State == ConnectionState.Closed)//ConnectionState is enum its    comes under System.Data name space   
            {
                _Con.Open();
            }
            rd = _cmd.ExecuteReader();
            List<spGetRecentJobsLastDayWithMatchingSkills> jobs = new List<spGetRecentJobsLastDayWithMatchingSkills>();
            while (rd.Read())
            {
                var job = new spGetRecentJobsLastDayWithMatchingSkills();
                job.ClientName = rd["ClientName"].ToString();
                job.WorkerId = rd["WorkerId"].ToString();
                job.ClientProfilePicture = rd["ClientProfilePicture"].ToString();
                job.JobSalary = Convert.ToDecimal(rd["JobSalary"].ToString());
                job.JobSkills = rd["JobSkills"].ToString();
                job.JobTitle = !string.IsNullOrEmpty(rd["JobTitle"].ToString()) ? rd["JobTitle"].ToString().ReplaceInformation((int)UserTypeEnum.Worker, _userRoleService.TextFilterCondition(rd["JobTitle"].ToString(), (int)UserTypeEnum.Worker, rd["ClientName"].ToString()).Result) : null;
                job.JobId = Convert.ToInt32(rd["JobId"].ToString());
                job.WorkerEmail = rd["WorkerEmail"].ToString();
                job.JobType = rd["JobType"].ToString();
                job.WorkerId = rd["WorkerId"].ToString();
                jobs.Add(job);
            }
            var workers = jobs.GroupBy(o => o.WorkerId);

            foreach (var item in workers)
            {
                string htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "NewJobsForYou.html");
                var workerJobs = jobs.Where(o => o.WorkerId.Contains(item.Key)).ToList();
                var workerEmail = jobs.Where(o => o.WorkerId.Contains(item.Key)).Select(o => o.WorkerEmail).FirstOrDefault();
                var workerId = jobs.Where(o => o.WorkerId.Contains(item.Key)).Select(o => o.WorkerId).FirstOrDefault();
                StringBuilder htmlString = new StringBuilder();
                htmlString.AppendLine("<table style=\"border-collapse: collapse;background-color: #fff;\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" width=\"90%\"  bgcolor=\"#fff\">");
                htmlString.AppendLine("    <tr style=\"background-color: #fff;\">");
                htmlString.AppendLine("      <td>");
                foreach (var worker in workerJobs)
                {
                    worker.ClientProfilePicture = !string.IsNullOrEmpty(worker.ClientProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{worker.ClientProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}";
                    htmlString.AppendLine(" <tr style=\"background-color: #fff;\">");
                    htmlString.AppendLine("      <td>");
                    htmlString.AppendLine("       <tr class=\"table_row\" style=\"text-align:center; height: 128px; width: 70%\">");
                    htmlString.AppendLine("        <td class=\"right_td\" width=\"15%\" style=\"border: none\"></td>");
                    htmlString.AppendLine("        <td class=\"templateColumnContainer\" style=\"padding-left: 40px; width: 100px; border: 1px solid #DCDCDC;border-right: none\";>");
                    htmlString.AppendLine("            <img style=\"width: 85px;");
                    htmlString.AppendLine("                                height: 85px;");
                    htmlString.AppendLine("                                max-height: 85px;");
                    htmlString.AppendLine("                                border-radius: 50%;");
                    htmlString.AppendLine("                                min-height:85px;\" src=\"" + worker.ClientProfilePicture + "\" width=\"85\" height=\"85\" alt=\"\" />");
                    htmlString.AppendLine("        </td>");
                    htmlString.AppendLine("        <td class=\"templateColumnContainer\" style=\"");
                    htmlString.AppendLine("                    font-family: Lato, sans-serif;");
                    htmlString.AppendLine("                    font-size: 18px;");
                    htmlString.AppendLine("                    height: 100px;");
                    htmlString.AppendLine("                    padding-left: 40px;");
                    htmlString.AppendLine("                    text-align: left;");
                    htmlString.AppendLine("                    border: 1px solid #DCDCDC;");
                    htmlString.AppendLine("                    border-right: none;");
                    htmlString.AppendLine("                    border-left: none\">");
                    htmlString.AppendLine("            <div class=\"name_title\" style=\"color: #333333;font-family: 'Lato-Black',sans-serif;font-size: 18px;font-weight: 900;line-height: 18px;padding-bottom: 10px\"><a style=\"text-decoration:none;color:#333333 !important\" href='https://www.evirtualassistants.com/job-details-work/" + worker.JobId + "'>" + worker.JobTitle + " </a></div>");
                    htmlString.AppendLine("            <div style=\"font-family: 'Lato-Regular',sans-serif;padding-bottom: 10px\">" + worker.ClientName + "</div>");
                    htmlString.AppendLine("                    <div style=\"font-family: 'Lato-Regular',sans-serif;color: #ed7b18;\">" + worker.JobSkills + "</div>");
                    htmlString.AppendLine("        </td>");
                    htmlString.AppendLine("<td class=\"templateColumnContainer\" style=\"");
                    htmlString.AppendLine("                    text-align: right;");
                    htmlString.AppendLine("	font-family: Lato, sans-serif;");
                    htmlString.AppendLine("	font-size: 18px;");
                    htmlString.AppendLine("	padding-right: 60px;");
                    htmlString.AppendLine("	height: 100px;");
                    htmlString.AppendLine("	border: 1px solid #DCDCDC;");
                    htmlString.AppendLine("	border-left: none;");
                    htmlString.AppendLine("	vertical-align: top;\"");
                    htmlString.AppendLine("                >");
                    htmlString.AppendLine("                    <div class=\"weekly_rate\" style=\"color: #333333;font-family: 'Lato-Black',sans-serif;font-size: 18px;font-weight: 900;line-height: 18px;padding-bottom: 10px;vertical-align: top; margin-top: 20px;\">$" + worker.JobSalary + "/month</div>");
                    htmlString.AppendLine("                    <div style=\"font-family: 'Lato-Regular',sans-serif;padding-bottom: 10px; vertical-align: top;\">" + worker.JobType + "</div>");
                    htmlString.AppendLine("                </td>");
                    htmlString.AppendLine("        <td class=\"right_td\" width=\"15%\" style=\"border: none\"></td>");
                    htmlString.AppendLine("    </tr>");

                }
                htmlString.AppendLine("</td></tr></table>");
                htmlContent = htmlContent.Replace("[Jobs]", htmlString.ToString());
                htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + workerEmail + "&code=" + workerId + "&type=2");

                using (var _toolsContext = new GoHireNowToolsContext())
                {
                    var sender = new mailer_sender();
                    sender.ms_custom_id = 0;
                    sender.ms_custom_type = 8;
                    sender.ms_date = DateTime.Now;
                    sender.ms_send_date = DateTime.Now;
                    sender.ms_email = workerEmail;
                    sender.ms_name = "";
                    sender.ms_subject = "New Jobs for you";
                    sender.ms_message = htmlContent;
                    sender.ms_from_email = "no-reply@evirtualassistants.com";
                    sender.ms_from_name = "eVirtualAssistants";
                    sender.ms_priority = 10;
                    sender.ms_issent = 0;
                    sender.ms_unsubscribe = Guid.NewGuid();

                    await _toolsContext.mailer_sender.AddAsync(sender);
                    await _toolsContext.SaveChangesAsync();
                }

                // using (MailMessage messageObj = new MailMessage
                // {
                //     IsBodyHtml = true,
                //     BodyEncoding = System.Text.Encoding.UTF8,
                //     From = new MailAddress("no-reply@evirtualassistants.com", "eVirtualAssistants")
                // })
                // {
                //     htmlContent = htmlContent.Replace("[Jobs]", htmlString.ToString());
                //     htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + workerEmail + "&code=" + workerId + "&type=2");
                //     messageObj.To.Add(new MailAddress(workerEmail));
                //     messageObj.Subject = "New Jobs for you";
                //     messageObj.Body = htmlContent;
                //     using (var client = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587))
                //     {
                //         client.Credentials =
                //             new NetworkCredential("AKIAIA5SSI4EFREFBKVQ", "AjeA3B59Qu1048gPkxN+xD5nw1uxduuYsVaojohTUa0A");
                //         client.EnableSsl = true;
                //         try
                //         {
                //             client.Send(messageObj);
                //         }
                //         catch (Exception ex)
                //         {

                //         }
                //     }
                // }
            }


        }

        public async void SendApplicantsToClients()
        {
            _Con = new SqlConnection(connection);
            _cmd = new SqlCommand("spGetLastHourRecentApplicantsOfJobs", _Con);
            if (_Con.State == ConnectionState.Closed)//ConnectionState is enum its    comes under System.Data name space   
            {
                _Con.Open();
            }
            rd = _cmd.ExecuteReader();
            List<spGetLastHourRecentApplicantsOfJobs> applicants = new List<spGetLastHourRecentApplicantsOfJobs>();
            while (rd.Read())
            {
                var applicant = new spGetLastHourRecentApplicantsOfJobs();
                applicant.ClientEmail = rd["ClientEmail"].ToString();
                applicant.WorkerId = rd["WorkerId"].ToString();
                applicant.ProfilePicture = rd["ProfilePicture"].ToString();
                applicant.TotalJobApplications = Convert.ToInt32(rd["TotalJobApplications"].ToString());
                applicant.UserSkills = rd["UserSkills"].ToString();
 //               applicant.CoverLetter = rd["CoverLetter"].ToString();
                applicant.CoverLetter = !string.IsNullOrEmpty(rd["CoverLetter"].ToString()) ? rd["CoverLetter"].ToString().ReplaceInformation((int)UserTypeEnum.Client, _userRoleService.TextFilterCondition(rd["ClientId"].ToString(), (int)UserTypeEnum.Client, rd["WorkerId"].ToString()).Result) : null;
                applicant.SkilliNameList = rd["SkilliNameList"].ToString();
                applicant.WorkerName = rd["WorkerName"].ToString();
                applicant.rating = rd["rating"].ToString();
                applicant.JobTitle = rd["JobTitle"].ToString();
                applicant.WorkerTitle = !string.IsNullOrEmpty(rd["WorkerTitle"].ToString()) ? rd["WorkerTitle"].ToString().ReplaceInformation((int)UserTypeEnum.Client, _userRoleService.TextFilterCondition(rd["ClientId"].ToString(), (int)UserTypeEnum.Client, rd["WorkerId"].ToString()).Result) : null;
                applicant.JobId = Convert.ToInt32(rd["JobId"].ToString());
                applicant.ClientId = rd["ClientId"].ToString();
                applicants.Add(applicant);
            }
            var clients = applicants.GroupBy(o => o.JobId);
            
            foreach (var item in clients)
            {
                var workers = applicants.Where(o => o.JobId.Equals(item.Key)).ToList();
                var job = applicants.Where(o => o.JobId.Equals(item.Key)).FirstOrDefault();
                var clientEmail = job.ClientEmail;
                var clientId = job.ClientId;
                var jobTitle = job.JobTitle;
                var jobId = job.JobId;
                StringBuilder htmlString = new StringBuilder();
                int totalJobApplicationsOfClient = job.TotalJobApplications;
                foreach (var worker in workers)
                {
                    worker.ProfilePicture = !string.IsNullOrEmpty(worker.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{worker.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}";
                    htmlString.AppendLine("  <tr class=\"table_row\" style=\"text-align:center; width: 70%\">");
                    htmlString.AppendLine("        <td width=\"15%\" style=\"border: none\"></td>");
                    htmlString.AppendLine("        <td class=\"templateColumnContainer\" style=\"padding-left: 40px; width: 100px; border: 1px solid #DCDCDC;border-right: none\"  valign=\"middle\">");
                    htmlString.AppendLine("           <a href=\"https://www.evirtualassistants.com/job-details-hire/" + job.JobId + "?fromClientMail=true\"> <img style=\"width: 85px;");
                    htmlString.AppendLine("                                height: 85px;");
                    htmlString.AppendLine("                                max-height: 85px;");
                    htmlString.AppendLine("                                border-radius: 50%;");
                    htmlString.AppendLine("                                min-height:85px;\" src=\"" + worker.ProfilePicture + "\" width=\"85\" height=\"85\" alt=\"\"></a>");
                    htmlString.AppendLine("        </td>");
                    htmlString.AppendLine("        <td class=\"templateColumnContainer\" style=\"");
                    htmlString.AppendLine("                    font-family: Lato, sans-serif;");
                    htmlString.AppendLine("                    font-size: 18px;");
                    htmlString.AppendLine("                    padding-left: 40px;");
                    htmlString.AppendLine("                    padding-top: 13px;");
                    htmlString.AppendLine("                    text-align: left;");
                    htmlString.AppendLine("                    border: 1px solid #DCDCDC;");
                    htmlString.AppendLine("                    border-right: none;");
                    htmlString.AppendLine("                    border-left: none\">");
                    htmlString.AppendLine("            <div class=\"name_title\" style=\"color: #333333;font-family: 'Lato-Black',sans-serif;font-size: 18px;font-weight: 900;line-height: 18px;padding-bottom: 10px\"><a style=\"text-decoration:none; color:#000000\" href=\"https://www.evirtualassistants.com/job-details-hire/" + job.JobId + "?fromClientMail=true\" >" + worker.WorkerName + "</a></div>");
                    htmlString.AppendLine("            <div style=\"width:160px; font-family: 'Lato-Regular',sans-serif;padding-bottom: 40px\"><div style=\"float:left; \"><span style=\"background-color:#e17518;color:white; padding: 5px; display: inline-block;\"><b>" + worker.rating + "</b></span></div><div style=\"float:right;\"><span style=\"padding: 0px; display: inline-block;\"><img src=\"https://devapiv1.evirtualassistants.com/EmailTemplateResources/Assets/s" + Math.Floor(Convert.ToDecimal(worker.rating)) + ".png\"></span></div></div>");
                    htmlString.AppendLine("            <div style=\"font-family: 'Lato-Regular',sans-serif;padding-bottom: 10px\">" + worker.WorkerTitle + "</div>");
                    htmlString.AppendLine("            <div style = \"font -family: 'Lato-Regular',sans-serif;padding-bottom: 10px; color:#ED7B18;\" >" + worker.SkilliNameList + "</ div >");
                    htmlString.AppendLine("        </td>");
                    htmlString.AppendLine("        <td class=\"right_td\" width=\"15%\" style=\"border-left: 1px solid #DCDCDC;\" ></td>");
                    htmlString.AppendLine("    </tr>");

                    htmlString.AppendLine("                         <tr class=\"table_row\" style=\"text-align:center;  width: 70%\">");
                    htmlString.AppendLine("<td width=\"15%\" style=\"border: none\"></td>");
                    htmlString.AppendLine("<td class=\"templateColumnContainer\" style=\"padding-left: 40px; width: 100px; border: 1px solid #DCDCDC;border-right: none\">");
                    htmlString.AppendLine("</td>");
                    htmlString.AppendLine("                            <td class=\"templateColumnContainer\" style=\"");
                    htmlString.AppendLine("font-family: Lato, sans-serif;");
                    htmlString.AppendLine("                                        font-size: 18px;");
                    htmlString.AppendLine("                                        padding-left: 40px;");
                    htmlString.AppendLine("text-align: left;");
                    htmlString.AppendLine("                                        border: 1px solid #DCDCDC;");
                    htmlString.AppendLine("border-right: none;");
                    htmlString.AppendLine("                                        border-left: none\">");
                    htmlString.AppendLine("<div style=\"font-family: 'Lato-Regular',sans-serif;padding-bottom: 10px\"><br>");
                    htmlString.AppendLine(worker.CoverLetter);
                    htmlString.AppendLine("<br><br></div>");
                    htmlString.AppendLine("<div style=\"width:100%\"><center><!--[if mso]>");
                    htmlString.AppendLine("<v:roundrect xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:w=\"urn:schemas-microsoft-com:office:word\" href=\"https://www.evirtualassistants.com/job-details-hire/" + job.JobId + "?fromClientMail=true\" style=\"height:35px;v-text-anchor:middle;width:114px;\" arcsize=\"100%\" stroke=\"f\" fillcolor=\"#ED7B18\">");
                    htmlString.AppendLine("<w:anchorlock/>");
                    htmlString.AppendLine("<center>");
                    htmlString.AppendLine("<![endif]-->");
                    htmlString.AppendLine("<a href=\"https://www.evirtualassistants.com/job-details-hire/" + job.JobId + "?fromClientMail=true\" style=\"background-color:#ED7B18;border-radius:35px;color:#ffffff;display:inline-block;font-family:sans-serif;font-size:13px;font-weight:bold;line-height:35px;text-align:center;text-decoration:none;width:114px;-webkit-text-size-adjust:none;\">VIEW</a>");
                    htmlString.AppendLine("<!--[if mso]>");
                    htmlString.AppendLine("</center>");
                    htmlString.AppendLine("</v:roundrect>");
                    htmlString.AppendLine("<![endif]--></center></div><br><br><br>");
                    htmlString.AppendLine("</td>");
                    htmlString.AppendLine("<td class=\"right_td\" width=\"15%\" style=\"border-left: 1px solid #DCDCDC; \"></td>");
                    htmlString.AppendLine("</tr>");
                }
                String htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "AppliedOnYourJob.html");
                var html = htmlString.ToString();
                htmlContent = htmlContent.Replace("[Applicants]", html);
                htmlContent = htmlContent.Replace("[TotalJobApplications]", totalJobApplicationsOfClient.ToString());
                htmlContent = htmlContent.Replace("[HireUrl]", "https://www.evirtualassistants.com/job-details-hire/" + jobId + "?fromClientMail=true");
                htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + clientEmail + "&code=" + clientId + "&type=3");

                using (var _toolsContext = new GoHireNowToolsContext())
                {
                    var sender = new mailer_sender();
                    sender.ms_custom_id = 0;
                    sender.ms_custom_type = 9;
                    sender.ms_date = DateTime.Now;
                    sender.ms_send_date = DateTime.Now;
                    sender.ms_email = clientEmail;
                    sender.ms_name = "";
                    sender.ms_subject = "New Job Applicants - " + jobTitle + "";
                    sender.ms_message = htmlContent;
                    sender.ms_from_email = "no-reply@evirtualassistants.com";
                    sender.ms_from_name = "eVirtualAssistants";
                    sender.ms_priority = 2;
                    sender.ms_issent = 0;
                    sender.ms_unsubscribe = Guid.NewGuid();

                    await _toolsContext.mailer_sender.AddAsync(sender);
                    await _toolsContext.SaveChangesAsync();
                }

                // using (MailMessage messageObj = new MailMessage
                // {
                //     IsBodyHtml = true,
                //     BodyEncoding = System.Text.Encoding.UTF8,
                //     From = new MailAddress("no-reply@evirtualassistants.com", "eVirtualAssistants")
                // })
                // {
                //     String htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "AppliedOnYourJob.html");
                //     var html = htmlString.ToString();
                //     htmlContent = htmlContent.Replace("[Applicants]", html);
                //     htmlContent = htmlContent.Replace("[TotalJobApplications]", totalJobApplicationsOfClient.ToString());
                //     htmlContent = htmlContent.Replace("[HireUrl]", "https://www.evirtualassistants.com/job-details-hire/" + jobId + "?fromClientMail=true");
                //     htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + clientEmail + "&code=" + clientId + "&type=3");
                //     messageObj.To.Add(new MailAddress(clientEmail));
                //     messageObj.Subject = "New Job Applicants - " + jobTitle + "";
                //     messageObj.Body = htmlContent;
                //     using (var client = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587))
                //     {
                //         client.Credentials =
                //             new NetworkCredential("AKIAIA5SSI4EFREFBKVQ", "AjeA3B59Qu1048gPkxN+xD5nw1uxduuYsVaojohTUa0A");
                //         client.EnableSsl = true;
                //         try
                //         {
                //             client.Send(messageObj);
                //         }
                //         catch (Exception ex)
                //         {

                //         }
                //     }
                // }
            }


        }

        public async void SendClientMessageToPostJob()
        {
            _Con = new SqlConnection(connection);
            _cmd = new SqlCommand("spGetLastDayRegisteredUsersWithNoJob", _Con);
            if (_Con.State == ConnectionState.Closed)//ConnectionState is enum its    comes under System.Data name space   
            {
                _Con.Open();
            }
            rd = _cmd.ExecuteReader();
            List<spGetLastDayRegisteredUsersWithNoJob> clients = new List<spGetLastDayRegisteredUsersWithNoJob>();
            while (rd.Read())
            {
                var client = new spGetLastDayRegisteredUsersWithNoJob();
                client.Email = rd["Email"].ToString();
                client.Name = rd["Name"].ToString();
                client.TotalJobsPosted = Convert.ToInt32(rd["TotalJobsPosted"].ToString());
                client.Id = rd["Id"].ToString();
                clients.Add(client);
            }
            string htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "PostAJob.html");
            foreach (var item in clients)
            {
                htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + item.Email + "&code=" + item.Id + "&type=");
                using (var _toolsContext = new GoHireNowToolsContext())
                {
                    var sender = new mailer_sender();
                    sender.ms_custom_id = 0;
                    sender.ms_custom_type = 10;
                    sender.ms_date = DateTime.Now;
                    sender.ms_send_date = DateTime.Now;
                    sender.ms_email = item.Email;
                    sender.ms_name = "";
                    sender.ms_subject = "Post A Job - Get Your Work Done!";
                    sender.ms_message = htmlContent;
                    sender.ms_from_email = "no-reply@evirtualassistants.com";
                    sender.ms_from_name = "eVirtualAssistants";
                    sender.ms_priority = 3;
                    sender.ms_issent = 0;
                    sender.ms_unsubscribe = Guid.NewGuid();

                    await _toolsContext.mailer_sender.AddAsync(sender);
                    await _toolsContext.SaveChangesAsync();
                }

                // using (MailMessage messageObj = new MailMessage
                // {
                //     IsBodyHtml = true,
                //     BodyEncoding = System.Text.Encoding.UTF8,
                //     From = new MailAddress("no-reply@evirtualassistants.com", "eVirtualAssistants")
                // })
                // {
                //     htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + item.Email + "&code=" + item.Id + "&type=");
                //     messageObj.To.Add(new MailAddress(item.Email));
                //     messageObj.Subject = "Post A Job - Get Your Work Done!";
                //     messageObj.Body = htmlContent;
                //     using (var client = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587))
                //     {
                //         client.Credentials =
                //             new NetworkCredential("AKIAIA5SSI4EFREFBKVQ", "AjeA3B59Qu1048gPkxN+xD5nw1uxduuYsVaojohTUa0A");
                //         client.EnableSsl = true;
                //         try
                //         {
                //             client.Send(messageObj);
                //         }
                //         catch (Exception ex)
                //         {

                //         }
                //     }
                // }
            }
            _Con.Close();
            _cmd = new SqlCommand("spLastDayRegisteredUsersWithNoJobEmailStatus", _Con);
            if (_Con.State == ConnectionState.Closed)//ConnectionState is enum its    comes under System.Data name space   
            {
                _Con.Open();
            }
            _cmd.ExecuteNonQuery();
            _Con.Close();
        }

        public async void SendWorkersMessageToCompleteProfile()
        {
            _Con = new SqlConnection(connection);
            _cmd = new SqlCommand("spGetWorkersMessageToCompleteProfile", _Con);
            if (_Con.State == ConnectionState.Closed)//ConnectionState is enum its    comes under System.Data name space   
            {
                _Con.Open();
            }
            rd = _cmd.ExecuteReader();
            List<spGetWorkersMessageToCompleteProfile> workers = new List<spGetWorkersMessageToCompleteProfile>();
            while (rd.Read())
            {
                var worker = new spGetWorkersMessageToCompleteProfile();
                worker.Email = rd["Email"].ToString();
                worker.Name = rd["Name"].ToString();
                worker.Id = rd["Id"].ToString();
                workers.Add(worker);
            }
            string htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "CompleteProfile.html");
            foreach (var item in workers)
            {
                htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + item.Email + "&code=" + item.Id + "&type=5");
                using (var _toolsContext = new GoHireNowToolsContext())
                {
                    var sender = new mailer_sender();
                    sender.ms_custom_id = 0;
                    sender.ms_custom_type = 11;
                    sender.ms_date = DateTime.Now;
                    sender.ms_send_date = DateTime.Now;
                    sender.ms_email = item.Email;
                    sender.ms_name = "";
                    sender.ms_subject = "Complete your profile";
                    sender.ms_message = htmlContent;
                    sender.ms_from_email = "no-reply@evirtualassistants.com";
                    sender.ms_from_name = "eVirtualAssistants";
                    sender.ms_priority = 3;
                    sender.ms_issent = 0;
                    sender.ms_unsubscribe = Guid.NewGuid();

                    await _toolsContext.mailer_sender.AddAsync(sender);
                    await _toolsContext.SaveChangesAsync();
                }

                // using (MailMessage messageObj = new MailMessage
                // {
                //     IsBodyHtml = true,
                //     BodyEncoding = System.Text.Encoding.UTF8,
                //     From = new MailAddress("no-reply@evirtualassistants.com", "eVirtualAssistants")
                // })
                // {
                //     htmlContent = htmlContent.Replace("[Unsubscribe]", "https://www.evirtualassistants.com/unsubscribe?email=" + item.Email + "&code=" + item.Id + "&type=5");
                //     messageObj.To.Add(new MailAddress(item.Email));
                //     messageObj.Subject = "Complete your profile";
                //     messageObj.Body = htmlContent;
                //     using (var client = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587))
                //     {
                //         client.Credentials =
                //             new NetworkCredential("AKIAIA5SSI4EFREFBKVQ", "AjeA3B59Qu1048gPkxN+xD5nw1uxduuYsVaojohTUa0A");
                //         client.EnableSsl = true;
                //         try
                //         {
                //             client.Send(messageObj);
                //         }
                //         catch (Exception ex)
                //         {

                //         }
                //     }
                // }
            }
            _Con.Close();
            _cmd = new SqlCommand("spWorkersMessageToCompleteProfileStatus", _Con);
            if (_Con.State == ConnectionState.Closed)//ConnectionState is enum its    comes under System.Data name space   
            {
                _Con.Open();
            }
            _cmd.ExecuteNonQuery();
            _Con.Close();
        }
    }
}
