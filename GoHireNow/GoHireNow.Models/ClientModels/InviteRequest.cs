using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class InviteRequest
    {
        public List<int> jobs { get; set; }
        public string userId { get; set; }
        public string companyName { get; set; }
    }
}
