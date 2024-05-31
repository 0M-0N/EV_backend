using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.MailModels
{
  public class EditMailMessageRequest
  {
    public int messageId { get; set; }
    public string message { get; set; }
  }
}
