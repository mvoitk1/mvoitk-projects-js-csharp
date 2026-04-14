using System;

namespace App.DAL.EF.Seeding
{
    public static class InitialData
    {
        public static readonly (string roleName, string roleDisplayName, Guid id)[] Roles =
        {
            ("admin", "Administrators", Guid.Parse("9ac94ce0-d51a-4edf-97c2-40494db52818")),
        };
        
        public static readonly (string name, string password, string firstName, string lastName, Guid id, string role)[]
            Users =
            {
                ("akaver@akaver.com", "Foo.bar1", "Andres", "Käver",
                    Guid.Parse("1099178a-2010-4617-9d46-8e2b16034e1c"), "admin"),
            };

        public static readonly Guid ApiKey1Secret = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public static readonly Guid ApiKey2Secret = Guid.Parse("00000000-0000-0000-0000-000000000002");
    }
}