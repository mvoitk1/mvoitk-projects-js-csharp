using System.Collections.Generic;
using com.akaver.Domain.Base.Identity;

namespace App.Domain.Identity
{
    public class AppUser: BaseUser<AppUserRole>
    {
        public ICollection<RefreshToken>? RefreshTokens { get; set; }

    }
}