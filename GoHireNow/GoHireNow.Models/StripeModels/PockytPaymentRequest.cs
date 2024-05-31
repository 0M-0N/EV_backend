using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.StripeModels
{
  public class PockytPaymentRequest
  {
    public int Amount { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PaymentMethod { get; set; }
  }
}
