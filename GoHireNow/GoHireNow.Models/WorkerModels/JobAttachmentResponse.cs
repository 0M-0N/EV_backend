using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.WorkerModels
{
    public class JobAttachmentResponse
    {

        public int Id { get; set; }
        public int JobId { get; set; }
        public int AttachmentTypeId { get; set; }
        public string AttachedFile { get; set; }
        public bool IsDeleted { get; set; }

    }
}
