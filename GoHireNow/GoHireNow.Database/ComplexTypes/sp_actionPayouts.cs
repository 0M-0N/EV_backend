using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class sp_actionPayouts
  {
    [Key]
    public string userId { get; set; }
    public int type { get; set; }
    public decimal amount { get; set; }
  }
}