using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class InviteCompaniesRequest
    {
        public string user1 { get; set; }
        public string user2 { get; set; }
        public string email1 { get; set; }
        public string email2 { get; set; }
        public int jobId { get; set; }
    }
}
