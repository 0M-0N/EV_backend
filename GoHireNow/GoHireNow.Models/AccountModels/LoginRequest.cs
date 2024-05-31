using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Models.AccountModels
{
    public class LoginRequest
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public string Timezone { get; set; }
    }
}
