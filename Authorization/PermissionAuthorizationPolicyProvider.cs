using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace mvc_boilerplate.Authorization
{
    public class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Check if the policy is already registered in AuthorizationOptions
            var policy = await base.GetPolicyAsync(policyName);

            if (policy != null)
                return policy;

            // Dynamically create a policy that requires a claim named "Permission"
            // with the value matching the policy name (e.g. "UserAccount.Edit").
            // This allows [Authorize(Policy = "Module.Action")] without manual registration.
            return new AuthorizationPolicyBuilder()
                .RequireClaim("Permission", policyName)
                .Build();
        }
    }
}
