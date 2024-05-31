using System;

namespace GoHireNow.Database
{
  public partial class ContractsSecured
  {
    public int Id { get; set; }
    public int ContractId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime PeriodDate { get; set; }
    public decimal Amount { get; set; }
    public string StripeChargeId { get; set; }
    public string ChargeId { get; set; }
    public string RefundId { get; set; }
    public int Method { get; set; }
    public int Type { get; set; }
    public int IsDeleted { get; set; }
  }
}