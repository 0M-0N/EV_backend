using System;
using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class sp_getWorkerSubscription
  {
    [Key]
    public int planid { get; set; }
    public int jobapplications { get; set; }
    public int maximumskills { get; set; }
    public int withdrawanytime { get; set; }
    public int profilevisits { get; set; }
    public int applicationstatus { get; set; }
    public int hideprofile { get; set; }
    public int viewapplications { get; set; }
    public int academyaccess { get; set; }
    public int fraudprotection { get; set; }
    public int emailnotifications { get; set; }
    public int aiapplications { get; set; }
    public int blackaccess { get; set; }
    public int featured { get; set; }
    public int firstmessage { get; set; }
    public int rockstarbadge { get; set; }
    public int livechat { get; set; }
    public int creditsleft { get; set; }
    public int skillsleft { get; set; }
    public int planprice { get; set; }
    public DateTime lastpaid { get; set; }
    public string planname { get; set; }
  }
}