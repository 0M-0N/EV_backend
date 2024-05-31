using System;

namespace GoHireNow.Models.ExceptionModels
{
    public class CustomException : Exception
    {
        public int StatusCode { get; set; }

        public CustomException()
        {

        }

        public CustomException(string message) : base(message)
        {

        }

        public CustomException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
        public CustomException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
