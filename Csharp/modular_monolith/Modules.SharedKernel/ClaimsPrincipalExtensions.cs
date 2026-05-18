using System.Security.Claims;

namespace Modules.SharedKernel;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public TKey UserId<TKey>() where TKey : struct
        {
            var stringId = user.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value.Trim();

            if (typeof(TKey) == typeof(string))
            {
                return (TKey)Convert.ChangeType(stringId, typeof(TKey));
            }

            if (typeof(TKey) == typeof(int) || typeof(TKey) == typeof(long))
            {
                return (TKey)Convert.ChangeType(stringId, typeof(TKey));
            }

            if (typeof(TKey) == typeof(Guid))
            {
                return (TKey)Convert.ChangeType(new Guid(stringId), typeof(TKey));
            }

            throw new Exception("invalid type provided");
        }

        public Guid UserId() => user.UserId<Guid>();
    }
}
