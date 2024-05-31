using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetTotalCoAccountBalanced
  {
    [Key]
    public decimal amount { get; set; }
  }
}