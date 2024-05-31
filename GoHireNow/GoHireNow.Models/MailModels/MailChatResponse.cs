using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class MailChatResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastLogin { get; set; }
        public string Title { get; set; }
        public string Picture { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string[] Skills { get; set; }
        public string MyPicture { get; set; }
        public string MyName { get; set; }
        public DateTime MailDate { get; set; }
        public bool IsRead { get; set; }
        public int UserType { get; set; }
        public int JobId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public string ToTimezone { get; set; }
        public int? totalCount { get; set; }
        public int IsSuspended { get; set; }
        public bool Exchangable { get; set; }
    }
}
