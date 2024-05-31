using GoHireNow.Api.Controllers;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Models.MailModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Security.Claims;

namespace GoHireNow.Api.Filters
{
    public class PricingPlanFilter : ActionFilterAttribute
    {
        public string EntryType { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            bool isCapable = false;
            UserCapablePricingPlanResponse userPricingPlan = null;
            IPricingService pricingService = (IPricingService)context.HttpContext.RequestServices.GetService(typeof(IPricingService));
            if (!string.IsNullOrEmpty(EntryType))
            {
                string toUserId = null;
                if (EntryType == "Contacts")
                {
                    SendMessageRequest model = context.ActionArguments.Values.FirstOrDefault() as SendMessageRequest;
                    toUserId = model.toUserId;
                }
                var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                userPricingPlan = pricingService.IsCapable(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), EntryType, toUserId);
                isCapable = userPricingPlan.Result;
            }
            if (!isCapable)
            {
                context.Result = new ObjectResult(new { stat = userPricingPlan.Stat, message = userPricingPlan.Message, result = userPricingPlan.Result });
                return;
            }
        }
    }
}
