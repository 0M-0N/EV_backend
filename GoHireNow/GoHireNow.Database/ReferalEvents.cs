using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
  public partial class ReferalEvents
  {
    public int Id { get; set; }
    public string UserId { get; set; }
    public int RefId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int IsDeleted { get; set; }
  }
}
