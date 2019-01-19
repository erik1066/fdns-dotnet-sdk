using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Xunit;

using Foundation.Sdk.Security;

namespace Foundation.Sdk.Tests
{
    public class AlwaysAllowHandlerTests
    {
        [Theory]
        [InlineData("fdns.object.bookstore.books.read", "issuer")]
        [InlineData("fdns.object.bookstore.books.write", "issuer")]
        [InlineData("fdns.object.bookstore.books.*", "issuer")]
        [InlineData("fdns.object.bookstore.*.*", "issuer")]
        [InlineData("nonsense", "issuer")]
        public async Task AlwaysAllow(string scope, string issuer)
        {
            // arrange
            var requirements = new [] { new HasScopeRequirement(scope, issuer)};
            var author = "author";
            var user = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new Claim[] {
                                new Claim(ClaimsIdentity.DefaultNameClaimType, author),
                            },
                            "Basic")
                        );
            
            var resource = new object();

            var context = new AuthorizationHandlerContext(requirements, user, resource);
            var subject = new AlwaysAllowHandler();

            // act
            await subject.HandleAsync(context);

            // assert
            Assert.True(context.HasSucceeded);
        }
    }
}