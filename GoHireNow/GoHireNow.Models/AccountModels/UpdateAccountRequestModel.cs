using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.AccountModels
{
    public class UpdateAccountRequestModel
    {
        public string CompanyName { get; set; }
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string Timezone { get; set; }
        public int CountryId { get; set; }
    }
}
