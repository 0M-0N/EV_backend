using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class JobAttachments
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int AttachmentTypeId { get; set; }
        public string AttachedFile { get; set; }
        public bool IsActive { get; set; }
        public bool? IsModified { get; set; }
        public bool? IsDeleted { get; set; }
        public string Title { get; set; }

        public virtual Jobs Job { get; set; }
    }
}
