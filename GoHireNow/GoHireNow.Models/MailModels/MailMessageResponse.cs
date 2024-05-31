using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class MailMessageResponse
    {
        public int MessageId { get; set; }
        public string Message { get; set; }
        public string Date { get; set; }
        public bool IsEdited { get; set; }
        public string Email { get; set; }
        public string Picture { get; set; }
        public bool Sent { get; set; }
        public string From { get; set; }
        public int MailId { get; set; }
        public int? CustomId { get; set; }
        public int? CustomIdType { get; set; }
        public string CustomLink { get; set; }
        public string Name { get; set; }
        public int UserType { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileImage { get; set; }
        public string FileExtension { get; set; }
    }
}
