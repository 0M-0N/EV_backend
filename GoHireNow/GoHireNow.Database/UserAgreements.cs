using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
  public partial class UserAgreements
  {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Ip { get; set; }
    public int Type { get; set; }
    public DateTime CreatedDate { get; set; }
  }
}
