using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class sp_ContractPayoutWeekly
  {
    [Key]
    public int ContractId { get; set; }
    public decimal amount { get; set; }
  }
}