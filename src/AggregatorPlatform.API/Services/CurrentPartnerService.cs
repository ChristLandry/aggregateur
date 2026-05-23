using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.API.Services;

public class CurrentPartnerService : ICurrentPartnerService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentPartnerService(IHttpContextAccessor accessor) => _accessor = accessor;

    public Partner? Current
    {
        get
        {
            var ctx = _accessor.HttpContext;
            if (ctx is null) return null;
            return ctx.Items.TryGetValue("CurrentPartner", out var p) ? p as Partner : null;
        }
    }

    public Guid? PartnerId => Current?.PartnerId;
}
