namespace GoHireNow.Models.WorkerModels
{
    public class WorkerProfileProgressResponse
    {
        public string Id { get; set; }
        public int UserUniqueId { get; set; }
        public int Progress { get; set; }
        public bool ProfilePicture { get; set; }
        public bool Title { get; set; }
        public bool Description { get; set; }
        public bool Salary { get; set; }
        public bool Availability { get; set; }
        public bool Education { get; set; }
        public bool Experience { get; set; }
        public int featured { get; set; }
        public decimal rating { get; set; }
        public bool Skills { get; set; }
        public bool Portfolio { get; set; }
        public bool AppliedJob { get; set; }
    }
}
