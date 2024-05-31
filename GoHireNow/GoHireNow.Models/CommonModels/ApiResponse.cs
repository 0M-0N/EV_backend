namespace GoHireNow.Models.CommonModels
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public T Response { get; set; }
        public string Message { get; set; }
    }
}
