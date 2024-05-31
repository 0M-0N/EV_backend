using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetIsSecured
  {
    [Key]
    public int r { get; set; }
    public decimal amount { get; set; }
  }
}