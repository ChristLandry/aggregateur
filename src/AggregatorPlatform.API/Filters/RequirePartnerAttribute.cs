using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AggregatorPlatform.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePartnerAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Items.TryGetValue("CurrentPartner", out var p) || p is not Partner)
        {
            context.Result = new ObjectResult(ApiResponse.Fail("PARTNER_REQUIRED", "Authenticated partner context required.")) { StatusCode = 401 };
        }
    }
}
