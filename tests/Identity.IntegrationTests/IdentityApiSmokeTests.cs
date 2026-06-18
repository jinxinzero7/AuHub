using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Identity.API.Middleware;
using Identity.Application.Services;
using Identity.Application.Queries.GetAdminUserDetail;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;

namespace Identity.IntegrationTests;

public class IdentityApiSmokeTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Health_ReturnsOk()
    {
        using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/users/banned");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminBanListAndUnban_UpdatesUserModerationState()
    {
        using var factory = new IdentityApiFactory();
        var user = User.Create("seller@example.com", "+79990001000", "seller_user", "hash", "Seller", UserRole.User);
        var adminId = Guid.NewGuid();
        factory.Repository.Seed(user);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(adminId, "Admin"));

        var banResponse = await client.PostAsJsonAsync($"/api/auth/users/{user.Id}/ban", new { Reason = "Fraud risk" });
        var bannedResponse = await client.GetAsync("/api/auth/users/banned");
        var unbanResponse = await client.PostAsync($"/api/auth/users/{user.Id}/unban", null);

        banResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        bannedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        unbanResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var bannedUsers = await bannedResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bannedUser = bannedUsers.EnumerateArray().Should().ContainSingle().Subject;
        bannedUser.GetProperty("userId").GetGuid().Should().Be(user.Id);
        bannedUser.GetProperty("reason").GetString().Should().Be("Fraud risk");
        factory.Repository.RevokedUserIds.Should().Contain(user.Id);
        factory.Repository.AuditLogs.Should().Contain(log =>
            log.ActorUserId == adminId &&
            log.Action == "UserBan" &&
            log.TargetType == "User" &&
            log.TargetId == user.Id &&
            log.Details == "Fraud risk");
        factory.Repository.AuditLogs.Should().Contain(log =>
            log.ActorUserId == adminId &&
            log.Action == "UserUnban" &&
            log.TargetType == "User" &&
            log.TargetId == user.Id);
        user.IsBanned.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BanMiddleware_BannedAuthenticatedUser_ReturnsForbidden()
    {
        var user = User.Create("banned@example.com", "+79990001001", "banned_user", "hash", "Banned User", UserRole.User);
        user.Ban("Policy violation");
        var repository = new InMemoryUserRepository();
        repository.Seed(user);
        var nextCalled = false;
        var middleware = new BanMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/protected";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, "User")
        }, "Test"));

        await middleware.InvokeAsync(context, repository);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublicProfile_ReturnsSafeSellerTrustFields()
    {
        using var factory = new IdentityApiFactory();
        var user = User.Create("seller@example.com", "+79990001002", "trusted_seller", "hash", "Trusted Seller", UserRole.User);
        user.MarkDocumentVerified();
        factory.Repository.Seed(user);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/auth/users/{user.Id}/public-profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
        profile.GetProperty("userId").GetGuid().Should().Be(user.Id);
        profile.GetProperty("nickname").GetString().Should().Be("trusted_seller");
        profile.GetProperty("name").GetString().Should().Be("Trusted Seller");
        profile.GetProperty("documentVerificationStatus").GetString().Should().Be("Verified");
        profile.TryGetProperty("email", out _).Should().BeFalse();
        profile.TryGetProperty("phoneNumber", out _).Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublicProfile_UnknownUser_ReturnsNotFound()
    {
        using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/auth/users/{Guid.NewGuid()}/public-profile");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminUserDetail_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/auth/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminUserDetail_WithUserToken_ReturnsForbidden()
    {
        using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(Guid.NewGuid(), "User"));

        var response = await client.GetAsync($"/api/auth/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminUserDetail_WithAdminToken_ReturnsSafeProfileAndDocumentMetadata()
    {
        using var factory = new IdentityApiFactory();
        var user = User.Create("detail@example.com", "+79990001003", "detail_user", "hash", "Detail User", UserRole.User);
        var documentRequest = DocumentVerificationRequest.Create(user.Id, "private/passport.jpg", "private/selfie.jpg");
        factory.Repository.Seed(user);
        factory.DocumentRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns([documentRequest]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(Guid.NewGuid(), "Admin"));

        var response = await client.GetAsync($"/api/auth/users/{user.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("userId").GetGuid().Should().Be(user.Id);
        json.GetProperty("documentVerificationHistory").EnumerateArray().Should().ContainSingle();
        var rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("passportImagePath");
        rawJson.Should().NotContain("selfieImagePath");
        rawJson.Should().NotContain("private/passport.jpg");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminUserDetail_UnknownUser_ReturnsNotFound()
    {
        using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(Guid.NewGuid(), "Admin"));

        var response = await client.GetAsync($"/api/auth/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("PassportImagePath")]
    [InlineData("SelfieImagePath")]
    [InlineData("PassportFile")]
    [InlineData("SelfieFile")]
    public void AdminUserDetailContract_DoesNotExposeDocumentStorageOrFiles(string propertyName)
    {
        typeof(AdminUserDetailResponse).GetProperty(propertyName).Should().BeNull();
        typeof(AdminDocumentVerificationMetadata).GetProperty(propertyName).Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DocumentVerificationUpload_ValidFiles_UploadsPrivateObjects()
    {
        using var factory = new IdentityApiFactory();
        var userId = Guid.NewGuid();
        factory.DocumentStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<string>(1));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId, "User"));
        using var content = CreateUploadContent(
            ("passportImage", "passport.jpg", "image/jpeg"),
            ("selfieImage", "selfie.png", "image/png"));

        var response = await client.PostAsync("/api/auth/document-verification/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var passportImagePath = payload.GetProperty("passportImagePath").GetString();
        var selfieImagePath = payload.GetProperty("selfieImagePath").GetString();
        passportImagePath.Should().StartWith($"document-verifications/{userId}/passport-");
        selfieImagePath.Should().StartWith($"document-verifications/{userId}/selfie-");
        await factory.DocumentStorage.Received(1).UploadAsync(Arg.Any<Stream>(), passportImagePath!, "image/jpeg", Arg.Any<CancellationToken>());
        await factory.DocumentStorage.Received(1).UploadAsync(Arg.Any<Stream>(), selfieImagePath!, "image/png", Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DocumentVerificationUpload_UnsupportedFileType_ReturnsBadRequest()
    {
        using var factory = new IdentityApiFactory();
        var userId = Guid.NewGuid();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId, "User"));
        using var content = CreateUploadContent(
            ("passportImage", "passport.txt", "text/plain"),
            ("selfieImage", "selfie.png", "image/png"));

        var response = await client.PostAsync("/api/auth/document-verification/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await factory.DocumentStorage.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static string CreateJwt(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "AuHub",
            audience: "AuHub-Users",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static MultipartFormDataContent CreateUploadContent(params (string Name, string FileName, string ContentType)[] files)
    {
        var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new ByteArrayContent("test image"u8.ToArray());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            content.Add(fileContent, file.Name, file.FileName);
        }

        return content;
    }

    private sealed class IdentityApiFactory : WebApplicationFactory<Program>
    {
        public InMemoryUserRepository Repository { get; } = new();
        public IDocumentStorageService DocumentStorage { get; } = Substitute.For<IDocumentStorageService>();
        public IDocumentVerificationRequestRepository DocumentRepository { get; } = Substitute.For<IDocumentVerificationRequestRepository>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=identity_test;Username=postgres;Password=postgres",
                    ["Jwt:Issuer"] = "AuHub",
                    ["Jwt:Audience"] = "AuHub-Users",
                    ["Jwt:Secret"] = "AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IRefreshTokenRepository>();
                services.RemoveAll<IAdminAuditLogRepository>();
                services.RemoveAll<IDocumentStorageService>();
                services.RemoveAll<IDocumentVerificationRequestRepository>();
                services.AddSingleton<IUserRepository>(Repository);
                services.AddSingleton<IRefreshTokenRepository>(Repository);
                services.AddSingleton<IAdminAuditLogRepository>(Repository);
                services.AddSingleton(DocumentStorage);
                services.AddSingleton(DocumentRepository);
            });
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository, IRefreshTokenRepository, IAdminAuditLogRepository
    {
        private readonly List<User> _users = new();
        private readonly List<RefreshToken> _refreshTokens = new();

        public List<Guid> RevokedUserIds { get; } = new();
        public List<AdminAuditLog> AuditLogs { get; } = new();

        public void Seed(User user)
        {
            _users.Add(user);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Email == email.ToLowerInvariant()));
        }

        public Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            var normalizedPhoneNumber = User.NormalizePhoneNumber(phoneNumber);

            return Task.FromResult(_users.FirstOrDefault(u => u.PhoneNumber == normalizedPhoneNumber));
        }

        public Task<User?> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default)
        {
            var normalizedNickname = nickname.Trim().ToLowerInvariant();

            return Task.FromResult(_users.FirstOrDefault(u => u.Nickname.ToLowerInvariant() == normalizedNickname));
        }

        public Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken cancellationToken = default)
        {
            return identifier.Contains('@')
                ? GetByEmailAsync(identifier, cancellationToken)
                : GetByPhoneNumberAsync(identifier, cancellationToken);
        }

        public Task<List<User>> GetBannedUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users
                .Where(u => u.IsBanned)
                .OrderByDescending(u => u.BannedAt)
                .ToList());
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_refreshTokens.FirstOrDefault(t => t.Token == token));
        }

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            _refreshTokens.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task AddAsync(AdminAuditLog auditLog, CancellationToken cancellationToken = default)
        {
            AuditLogs.Add(auditLog);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RevokedUserIds.Add(userId);
            foreach (var token in _refreshTokens.Where(t => t.UserId == userId && !t.IsRevoked))
            {
                token.Revoke();
            }

            return Task.CompletedTask;
        }

        public Task RevokeTokenAsync(Guid tokenId, CancellationToken cancellationToken = default)
        {
            var token = _refreshTokens.FirstOrDefault(t => t.Id == tokenId);
            token?.Revoke();
            return Task.CompletedTask;
        }

        public Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken = default)
        {
            foreach (var token in _refreshTokens.Where(t => t.FamilyId == familyId && !t.IsRevoked))
            {
                token.Revoke();
            }

            return Task.CompletedTask;
        }
    }
}
