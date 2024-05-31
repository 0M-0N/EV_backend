namespace GoHireNow.Models.ClientModels
{
    public class ClientProfileProgressResponse
    {
        public string Id { get; set; }
        public int UniqueId { get; set; }
        public int Progress { get; set; }
        public bool Logo { get; set; }
        public bool Title { get; set; }
        public bool Description { get; set; }
        public bool Jobs { get; set; }
        public bool Contacts { get; set; }
        public bool Paid { get; set; }
    }
}
