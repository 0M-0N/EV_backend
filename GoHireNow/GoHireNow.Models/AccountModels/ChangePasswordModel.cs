using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Models.AccountModels
{
    public class ChangePasswordModel
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}
