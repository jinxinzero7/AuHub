using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Auctions.API.Endpoints.Lots;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using AuHub.Shared.ValueObjects;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class AuctionsApiSmokeTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Health_ReturnsOk()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateLot_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/lots", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PendingModeration_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/lots/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/lots")]
    [InlineData("/api/lots/00000000-0000-0000-0000-000000000001/bids")]
    [Trait("Category", "Integration")]
    public async Task MarketplaceMutation_WithAdminToken_ReturnsForbidden(string route)
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt(Guid.NewGuid(), "Admin"));

        var response = await client.PostAsJsonAsync(route, new { Amount = 1500m });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminRead_WithUserToken_ReturnsForbidden()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt(Guid.NewGuid(), "User"));

        var response = await client.GetAsync("/api/admin/lots/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SellerLots_Anonymous_ReturnsOnlyActivePublicLotsForSeller()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<GetLotsResponse>(
            $"/api/sellers/{factory.SellerId}/lots");

        response.Should().NotBeNull();
        response!.Lots.Should().ContainSingle(lot =>
            lot.SellerId == factory.SellerId && lot.Status == nameof(LotStatus.Active));
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(9);
        response.TotalCount.Should().Be(1);
    }

    [Theory]
    [InlineData("?page=0&pageSize=0", 1, 9)]
    [InlineData("?page=2&pageSize=101", 2, 9)]
    [InlineData("?page=2&pageSize=20&includeDrafts=true", 2, 20)]
    [Trait("Category", "Integration")]
    public async Task SellerLots_NormalizesPaginationAndIgnoresPrivateFilters(
        string queryString,
        int expectedPage,
        int expectedPageSize)
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<GetLotsResponse>(
            $"/api/sellers/{Guid.NewGuid()}/lots{queryString}");

        response.Should().NotBeNull();
        response!.Lots.Should().BeEmpty();
        response.Page.Should().Be(expectedPage);
        response.PageSize.Should().Be(expectedPageSize);
        response.TotalCount.Should().Be(0);
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

    private sealed class AuctionsApiFactory : WebApplicationFactory<Program>
    {
        public Guid SellerId { get; } = Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=auctions_test;Username=postgres;Password=postgres",
                    ["Jwt:Issuer"] = "AuHub",
                    ["Jwt:Audience"] = "AuHub-Users",
                    ["Jwt:Secret"] = "AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026",
                    ["MinIO:Endpoint"] = "localhost:9000",
                    ["MinIO:ExternalEndpoint"] = "http://localhost:9000",
                    ["MinIO:AccessKey"] = "test",
                    ["MinIO:SecretKey"] = "test",
                    ["MinIO:BucketName"] = "test",
                    ["DisableHostedServices"] = "true",
                    ["Payment:BaseUrl"] = "http://localhost",
                    ["Notifications:BaseUrl"] = "http://localhost"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                var activeLot = CreateActiveLot(SellerId, "Public seller lot");
                var draftLot = CreateDraftLot(SellerId, "Private seller draft");
                var otherSellerLot = CreateActiveLot(Guid.NewGuid(), "Other seller lot");
                var lotRepository = Substitute.For<ILotRepository>();
                lotRepository.GetBySellerIdAsync(
                        Arg.Any<Guid>(),
                        Arg.Any<bool>(),
                        Arg.Any<CancellationToken>())
                    .Returns([activeLot, draftLot, otherSellerLot]);

                services.RemoveAll<ILotRepository>();
                services.RemoveAll<IImageStorageService>();
                services.AddSingleton(lotRepository);
                services.AddSingleton(Substitute.For<IImageStorageService>());
                services.AddSingleton(Substitute.For<IPublishEndpoint>());
            });
        }
    }

    private static Lot CreateDraftLot(Guid sellerId, string title)
    {
        return Lot.Create(
            title,
            "Description",
            Money.FromDecimal(1000m),
            TimeSpan.FromDays(1),
            sellerId,
            [DeliveryProvider.Cdek]);
    }

    private static Lot CreateActiveLot(Guid sellerId, string title)
    {
        var lot = CreateDraftLot(sellerId, title);
        lot.SubmitForModeration();
        lot.Approve();
        return lot;
    }
}
