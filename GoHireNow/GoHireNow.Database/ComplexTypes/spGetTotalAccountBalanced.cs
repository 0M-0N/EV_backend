using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetTotalAccountBalanced
  {
    [Key]
    public decimal amount { get; set; }
  }
}