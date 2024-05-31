using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Service.CommonServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace GoHireNow.Identity.Filters
{
    public class AccountCustomExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is CustomException)
            {
                var ex = context.Exception as CustomException;
                context.HttpContext.Response.StatusCode = ex.StatusCode;
                context.Result = new ObjectResult(new ApiResponse<string>() { ErrorMessage = ex.Message, Success = false });
                LogError(ex);
            }
            else
            {
                var ex = context.Exception as Exception;
                context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Result = new ObjectResult(new ApiResponse<string>() { ErrorMessage = "Internal server error", Success = false });
                LogError(ex);
            }

            base.OnException(context);
        }

        private void LogError(CustomException ex)
        {
            var logService = new CustomLogService();
            var error = new LogErrorRequest();
            error.ErrorMessage = ex.ToString();
            logService.LogError(error);

        }

        private void LogError(Exception ex)
        {
            var logService = new CustomLogService();
            var error = new LogErrorRequest();
            error.ErrorMessage = ex.ToString();
            logService.LogError(error);
        }
    }
}
