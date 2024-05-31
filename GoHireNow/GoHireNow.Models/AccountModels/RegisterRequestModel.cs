using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Models.AccountModels
{
    public class RegisterRequestModel
    {
        [Required(ErrorMessage = "Email is required")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Minimum length of password should be 4 characters", MinimumLength = 4)]
        public string Password { get; set; }
        [Required]
        public int CountryId { get; set; }
        [Required]
        [Range(1, 2, ErrorMessage = ("Invalid UserTypeId"))]
        public int UserType { get; set; }
        public string CompanyName { get; set; }
        public string FullName { get; set; }
        public string RefUrl { get; set; }
        public string Timezone { get; set; }
        public int? RefId { get; set; }
    }
}
