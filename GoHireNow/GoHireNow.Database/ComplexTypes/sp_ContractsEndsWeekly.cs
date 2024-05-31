using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class sp_ContractsEndsWeekly
  {
    [Key]
    public int contractId { get; set; }
    public decimal amountDue { get; set; }
    public string StripeChargeId { get; set; }
  }
}