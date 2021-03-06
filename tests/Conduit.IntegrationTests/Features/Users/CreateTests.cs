﻿using System.Linq;
using System.Threading.Tasks;
using Conduit.Features.Users;
using Conduit.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users
{
    public class CreateTests : SliceFixture
    {
        [Fact]
        public async Task Expect_Create_User()
        {
            Log.Information("--> Task Expect_Create_User()");

            var command = new Create.Command()
            {
                User = new Create.UserData()
                {
                    Email = "email",
                    Password = "password",
                    Username = "username"
                }
            };

            // find user
            var user = await SendAsync(command);

            var created = await ExecuteDbContextAsync(
                db => db.Persons.Where(d => d.Email == command.User.Email)
                .SingleOrDefaultAsync());

            Assert.NotNull(created);
            Assert.Equal(created.Hash, new PasswordHasher().Hash("password", created.Salt));
        }
    }
}