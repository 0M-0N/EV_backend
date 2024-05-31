using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class sp_UsersPayoutWeekly
  {
    [Key]
    public string userid { get; set; }
    public decimal amount { get; set; }
  }
}