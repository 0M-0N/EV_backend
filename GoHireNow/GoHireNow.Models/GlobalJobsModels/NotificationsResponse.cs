using GoHireNow.Models.CommonModels;
using GoHireNow.Models.WorkerModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.JobsModels
{
  public class NotificationsResponse
  {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string CompanyId { get; set; }
    public string CustomName { get; set; }
    public int CustomId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int IsDelerte { get; set; }
  }
}
