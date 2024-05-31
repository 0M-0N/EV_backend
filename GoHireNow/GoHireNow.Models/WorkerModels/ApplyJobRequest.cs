namespace GoHireNow.Models.WorkerModels
{
    public class ApplyJobRequest
    {
        public string Introduction { get; set; }
        public int JobId { get; set; }
        public string UserId { get; set; }
        public int UserUniqueId { get; set; }
        public bool Resume { get; set; }
        public string CoverLetter { get; set; }
    }
}
