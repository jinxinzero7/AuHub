using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Infrastructure.BackgroundServices;
using Auctions.Infrastructure.Data;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using IntegrationTestSupport;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class AuctionsPersistenceTests : IAsyncLifetime
{
    private readonly PostgresTestDatabase _database = new("auctions_persistence_tests");
    private AuctionsPersistenceFactory _factory = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _factory = new AuctionsPersistenceFactory(_database.ConnectionString);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LotLifecycle_PersistsDraftModerationApprovalAndPublicListing()
    {
        var sellerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Vintage camera Zenit");

        var createdLot = await LoadLotAsync(lotId);
        createdLot.Status.Should().Be(LotStatus.Draft);
        createdLot.SellerId.Should().Be(sellerId);
        createdLot.CurrentPrice.Amount.Should().Be(1200m);
        createdLot.SupportedDeliveryProviders.Should().BeEquivalentTo([DeliveryProvider.Cdek, DeliveryProvider.RussianPost]);

        Authenticate(client, otherUserId);
        var otherUserSubmitResponse = await client.PostAsync($"/api/lots/{lotId}/submit-for-moderation", null);
        otherUserSubmitResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authenticate(client, sellerId);
        var submitResponse = await client.PostAsync($"/api/lots/{lotId}/submit-for-moderation", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pendingLot = await LoadLotAsync(lotId);
        pendingLot.Status.Should().Be(LotStatus.PendingModeration);

        var editWhilePendingResponse = await client.PutAsJsonAsync($"/api/lots/{lotId}", new
        {
            title = "Edited while pending",
            description = "This change must not be persisted",
            startingPrice = 1500m,
            durationHours = 24,
            supportedDeliveryProviders = new[] { "Cdek" },
            submitForModeration = false
        });

        editWhilePendingResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var unchangedPendingLot = await LoadLotAsync(lotId);
        unchangedPendingLot.Title.Should().Be("Vintage camera Zenit");
        unchangedPendingLot.Status.Should().Be(LotStatus.PendingModeration);

        Authenticate(client, adminId, "Admin");
        var approveResponse = await client.PostAsync($"/api/lots/{lotId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeLot = await LoadLotAsync(lotId);
        activeLot.Status.Should().Be(LotStatus.Active);
        activeLot.StartTime.Should().NotBeNull();
        activeLot.EndTime.Should().NotBeNull();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
            var auditLogExists = await dbContext.AdminAuditLogs
                .AnyAsync(log =>
                    log.ActorUserId == adminId &&
                    log.Action == "LotApprove" &&
                    log.TargetId == lotId);

            auditLogExists.Should().BeTrue();
        }

        client.DefaultRequestHeaders.Authorization = null;
        var publicResponse = await client.GetAsync("/api/lots?onlyActive=true&page=1&pageSize=20");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicJson = await JsonNode.ParseAsync(await publicResponse.Content.ReadAsStreamAsync());
        var lots = publicJson?["lots"]?.AsArray();
        lots.Should().NotBeNull();
        lots!.Select(node => Guid.Parse(node!["id"]!.GetValue<string>()))
            .Should()
            .Contain(lotId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectedLot_CanBeEditedBackToDraftAndPersistsAuditLog()
    {
        var sellerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Porcelain tea set");

        Authenticate(client, sellerId);
        var submitResponse = await client.PostAsync($"/api/lots/{lotId}/submit-for-moderation", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Authenticate(client, adminId, "Admin");
        var rejectResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/reject", new
        {
            reason = "Photos do not show the item condition"
        });

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rejectedLot = await LoadLotAsync(lotId);
        rejectedLot.Status.Should().Be(LotStatus.Rejected);
        rejectedLot.AdminComment.Should().Be("Photos do not show the item condition");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
            var auditLogExists = await dbContext.AdminAuditLogs
                .AnyAsync(log =>
                    log.ActorUserId == adminId &&
                    log.Action == "LotReject" &&
                    log.TargetId == lotId &&
                    log.Details == "Photos do not show the item condition");

            auditLogExists.Should().BeTrue();
        }

        Authenticate(client, sellerId);
        var editResponse = await client.PutAsJsonAsync($"/api/lots/{lotId}", new
        {
            title = "Porcelain tea set with detailed photos",
            description = "Updated description with condition details and complete photo set",
            startingPrice = 2200m,
            durationHours = 72,
            supportedDeliveryProviders = new[] { "YandexDelivery", "RussianPost" },
            submitForModeration = false
        });

        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var editedLot = await LoadLotAsync(lotId);
        editedLot.Status.Should().Be(LotStatus.Draft);
        editedLot.AdminComment.Should().BeNull();
        editedLot.Title.Should().Be("Porcelain tea set with detailed photos");
        editedLot.CurrentPrice.Amount.Should().Be(2200m);
        editedLot.SupportedDeliveryProviders.Should().BeEquivalentTo([DeliveryProvider.YandexDelivery, DeliveryProvider.RussianPost]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PlaceBid_OnActiveLot_ReservesFundsAndPersistsBidAndOutbox()
    {
        var sellerId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Mechanical keyboard");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);

        await PlaceBidAsync(client, lotId, bidderId, 1500m);

        var lot = await LoadLotAsync(lotId);
        lot.CurrentPrice.Amount.Should().Be(1500m);
        lot.WinnerId.Should().BeNull();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var bid = await dbContext.Bids.AsNoTracking().SingleAsync(savedBid => savedBid.LotId == lotId);
        bid.BidderId.Should().Be(bidderId);
        bid.Amount.Amount.Should().Be(1500m);

        var bidPlacedOutboxExists = await dbContext.OutboxMessages
            .AnyAsync(message => message.Type == "BidPlaced" && message.Payload.Contains(lotId.ToString()));
        bidPlacedOutboxExists.Should().BeTrue();

        _factory.PaymentClient.ReservedFunds.Should().ContainSingle(reservation =>
            reservation.UserId == bidderId &&
            reservation.Amount == 1500m &&
            reservation.LotId == lotId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForceComplete_ActiveLotWithoutBids_PersistsCompletedNoWinner()
    {
        var sellerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "No-bid lot");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);

        Authenticate(client, sellerId);
        var sellerDemoCompleteResponse = await client.PostAsync($"/api/lots/{lotId}/demo-complete", null);
        sellerDemoCompleteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var activeLot = await LoadLotAsync(lotId);
        activeLot.Status.Should().Be(LotStatus.Active);

        Authenticate(client, adminId, "Admin");
        var completeResponse = await client.PostAsync($"/api/admin/lots/{lotId}/force-complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedLot = await LoadLotAsync(lotId);
        completedLot.Status.Should().Be(LotStatus.CompletedNoWinner);
        completedLot.WinnerId.Should().BeNull();
        completedLot.DeliveryRequestDeadlineAt.Should().BeNull();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var auditLogExists = await dbContext.AdminAuditLogs
            .AnyAsync(log =>
                log.ActorUserId == adminId &&
                log.Action == "LotForceComplete" &&
                log.TargetId == lotId);

        auditLogExists.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SellerDemoComplete_OwnedActiveLotWithBid_OpensDeliveryRequestWindow()
    {
        var sellerId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Demo complete lot");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);
        await PlaceBidAsync(client, lotId, winnerId, 1500m);

        Authenticate(client, otherUserId);
        var forbiddenResponse = await client.PostAsync($"/api/lots/{lotId}/demo-complete", null);
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authenticate(client, sellerId);
        var completeResponse = await client.PostAsync($"/api/lots/{lotId}/demo-complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedLot = await LoadLotAsync(lotId);
        completedLot.Status.Should().Be(LotStatus.DeliveryRequestPending);
        completedLot.WinnerId.Should().Be(winnerId);
        completedLot.DeliveryRequestDeadlineAt.Should().NotBeNull();

        _factory.PaymentClient.WinnerCharges.Should().ContainSingle(charge =>
            charge.WinnerId == winnerId &&
            charge.Amount == 1500m &&
            charge.LotId == lotId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeliveryFlow_FromCompletedLot_PersistsShippingConfirmationPayoutAndTrustScore()
    {
        var sellerId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Delivery flow lot");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);
        await PlaceBidAsync(client, lotId, winnerId, 1500m);

        Authenticate(client, adminId, "Admin");
        var completeResponse = await client.PostAsync($"/api/admin/lots/{lotId}/force-complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedLot = await LoadLotAsync(lotId);
        completedLot.Status.Should().Be(LotStatus.DeliveryRequestPending);
        completedLot.WinnerId.Should().Be(winnerId);
        completedLot.DeliveryRequestDeadlineAt.Should().NotBeNull();

        Authenticate(client, otherUserId);
        var otherUserDeliveryResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/delivery-request", new
        {
            provider = "Cdek",
            address = "Moscow, CDEK pickup point 42",
            recipientName = "Winner",
            recipientPhone = "+79990000000"
        });
        otherUserDeliveryResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authenticate(client, winnerId);
        var unsupportedProviderResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/delivery-request", new
        {
            provider = "YandexDelivery",
            address = "Moscow, Yandex pickup point 11",
            recipientName = "Winner",
            recipientPhone = "+79990000000"
        });
        unsupportedProviderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var deliveryResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/delivery-request", new
        {
            provider = "Cdek",
            address = "Moscow, CDEK pickup point 42",
            recipientName = "Winner",
            recipientPhone = "+79990000000"
        });
        deliveryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var shippingPendingLot = await LoadLotAsync(lotId);
        shippingPendingLot.Status.Should().Be(LotStatus.ShippingPending);
        shippingPendingLot.SelectedDeliveryProvider.Should().Be(DeliveryProvider.Cdek);
        shippingPendingLot.DeliveryAddress.Should().Be("Moscow, CDEK pickup point 42");
        shippingPendingLot.DeliveryRecipientName.Should().Be("Winner");
        shippingPendingLot.DeliveryRecipientPhone.Should().Be("+79990000000");

        Authenticate(client, otherUserId);
        var otherUserShipResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/ship", new
        {
            trackingNumber = "CDEK-123"
        });
        otherUserShipResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authenticate(client, sellerId);
        var shipResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/ship", new
        {
            trackingNumber = "CDEK-123"
        });
        shipResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var shippedLot = await LoadLotAsync(lotId);
        shippedLot.Status.Should().Be(LotStatus.Shipped);
        shippedLot.TrackingNumber.Should().Be("CDEK-123");

        Authenticate(client, otherUserId);
        var otherUserConfirmResponse = await client.PostAsync($"/api/lots/{lotId}/confirm-delivery", null);
        otherUserConfirmResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authenticate(client, winnerId);
        var confirmResponse = await client.PostAsync($"/api/lots/{lotId}/confirm-delivery", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedTransactionLot = await LoadLotAsync(lotId);
        completedTransactionLot.Status.Should().Be(LotStatus.TransactionComplete);

        _factory.PaymentClient.SellerTransfers.Should().ContainSingle(transfer =>
            transfer.SellerId == sellerId &&
            transfer.Amount == 1485m &&
            transfer.ServiceFee == 15m &&
            transfer.LotId == lotId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var trustScoreEventExists = await dbContext.TrustScoreEvents
            .AnyAsync(trustEvent =>
                trustEvent.UserId == sellerId &&
                trustEvent.Subject == TrustScoreSubject.Seller &&
                trustEvent.Reason == TrustScoreReason.SuccessfulSale &&
                trustEvent.ReferenceId == lotId);

        trustScoreEventExists.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeliveryRequestExpiration_OverdueWinnerWindow_RefundsBuyerAndPersistsTrustPenalty()
    {
        var sellerId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Expired delivery request lot");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);
        await PlaceBidAsync(client, lotId, winnerId, 1500m);

        Authenticate(client, adminId, "Admin");
        var completeResponse = await client.PostAsync($"/api/admin/lots/{lotId}/force-complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
            var lot = await dbContext.Lots.SingleAsync(savedLot => savedLot.Id == lotId);
            dbContext.Entry(lot).Property(nameof(Lot.DeliveryRequestDeadlineAt)).CurrentValue = DateTime.UtcNow.AddMinutes(-5);
            await dbContext.SaveChangesAsync();
        }

        var expirationService = new DeliveryRequestExpirationService(
            NullLogger<DeliveryRequestExpirationService>.Instance,
            _factory.Services);

        await expirationService.RunOnceAsync();

        var expiredLot = await LoadLotAsync(lotId);
        expiredLot.Status.Should().Be(LotStatus.DeliveryRequestExpired);

        _factory.PaymentClient.RefundedFunds.Should().ContainSingle(refund =>
            refund.UserId == winnerId &&
            refund.Amount == 1500m &&
            refund.LotId == lotId);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var trustScoreEventExists = await verificationDbContext.TrustScoreEvents
            .AnyAsync(trustEvent =>
                trustEvent.UserId == winnerId &&
                trustEvent.Subject == TrustScoreSubject.Buyer &&
                trustEvent.Reason == TrustScoreReason.DeliveryRequestExpired &&
                trustEvent.ReferenceId == lotId);

        trustScoreEventExists.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PlaceBid_IdempotentRetry_ReturnsExistingBidWithoutDuplicatePersistenceOrReserve()
    {
        var sellerId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Idempotent bid lot");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);

        await PlaceBidAsync(client, lotId, bidderId, 1500m, idempotencyKey);
        await PlaceBidAsync(client, lotId, bidderId, 1500m, idempotencyKey);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var bids = await dbContext.Bids.AsNoTracking().Where(bid => bid.LotId == lotId).ToListAsync();

        bids.Should().ContainSingle();
        bids.Single().IdempotencyKey.Should().Be(idempotencyKey);
        _factory.PaymentClient.ReservedFunds.Should().ContainSingle(reservation =>
            reservation.UserId == bidderId &&
            reservation.Amount == 1500m &&
            reservation.LotId == lotId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HigherBid_ReleasesPreviousBidderAndPersistsReleaseOutbox()
    {
        var sellerId = Guid.NewGuid();
        var firstBidderId = Guid.NewGuid();
        var secondBidderId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Previous bidder release lot");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);
        await PlaceBidAsync(client, lotId, firstBidderId, 1500m);
        await PlaceBidAsync(client, lotId, secondBidderId, 1800m);

        var lot = await LoadLotAsync(lotId);
        lot.CurrentPrice.Amount.Should().Be(1800m);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var bids = await dbContext.Bids.AsNoTracking().Where(bid => bid.LotId == lotId).ToListAsync();
        bids.Should().HaveCount(2);

        var releaseOutboxExists = await dbContext.OutboxMessages
            .AnyAsync(message => message.Type == "ReleasePreviousBidderFunds" && message.Payload.Contains(firstBidderId.ToString()));
        releaseOutboxExists.Should().BeTrue();

        _factory.PaymentClient.ReleasedFunds.Should().ContainSingle(release =>
            release.UserId == firstBidderId &&
            release.Amount == 1500m &&
            release.LotId == lotId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HigherBid_ByCurrentTopBidder_ReservesOnlyDeltaWithoutRelease()
    {
        var sellerId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Same bidder delta reserve lot");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);
        await PlaceBidAsync(client, lotId, bidderId, 1500m);
        await PlaceBidAsync(client, lotId, bidderId, 1800m);

        var lot = await LoadLotAsync(lotId);
        lot.CurrentPrice.Amount.Should().Be(1800m);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var bids = await dbContext.Bids.AsNoTracking().Where(bid => bid.LotId == lotId).ToListAsync();
        bids.Should().HaveCount(2);

        _factory.PaymentClient.ReservedFunds.Should().ContainSingle(reservation =>
            reservation.UserId == bidderId &&
            reservation.Amount == 1500m &&
            reservation.LotId == lotId);
        _factory.PaymentClient.ReservedFunds.Should().ContainSingle(reservation =>
            reservation.UserId == bidderId &&
            reservation.Amount == 300m &&
            reservation.LotId == lotId);
        _factory.PaymentClient.ReleasedFunds.Should().NotContain(release => release.LotId == lotId);
    }

    private async Task<Guid> CreateDraftLotAsync(HttpClient client, Guid sellerId, string title)
    {
        Authenticate(client, sellerId);
        var createResponse = await client.PostAsJsonAsync("/api/lots", new
        {
            title,
            description = "Working film camera with original leather case",
            startingPrice = 1200m,
            durationHours = 48,
            supportedDeliveryProviders = new[] { "Cdek", "RussianPost" }
        });

        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createBody);
        return await ReadGuidAsync(createResponse, "lotId");
    }

    private static async Task SubmitAndApproveLotAsync(HttpClient client, Guid lotId, Guid sellerId, Guid adminId)
    {
        Authenticate(client, sellerId);
        var submitResponse = await client.PostAsync($"/api/lots/{lotId}/submit-for-moderation", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Authenticate(client, adminId, "Admin");
        var approveResponse = await client.PostAsync($"/api/lots/{lotId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task PlaceBidAsync(HttpClient client, Guid lotId, Guid bidderId, decimal amount, Guid? idempotencyKey = null)
    {
        Authenticate(client, bidderId);
        var bidResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/bids", new
        {
            amount,
            idempotencyKey = idempotencyKey ?? Guid.NewGuid()
        });

        var bidBody = await bidResponse.Content.ReadAsStringAsync();
        bidResponse.StatusCode.Should().Be(HttpStatusCode.OK, bidBody);
    }

    private async Task<Lot> LoadLotAsync(Guid lotId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        return await dbContext.Lots
            .AsNoTracking()
            .SingleAsync(lot => lot.Id == lotId);
    }

    private static void Authenticate(HttpClient client, Guid userId, string role = "User")
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestTokenFactory.CreateUserToken(userId, role));
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string propertyName)
    {
        var json = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());
        return Guid.Parse(json![propertyName]!.GetValue<string>());
    }

    private sealed class AuctionsPersistenceFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public FakePaymentClient PaymentClient { get; } = new();

        public AuctionsPersistenceFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["Jwt:Issuer"] = JwtTestTokenFactory.Issuer,
                    ["Jwt:Audience"] = JwtTestTokenFactory.Audience,
                    ["Jwt:Secret"] = JwtTestTokenFactory.Secret,
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
                services.RemoveAll<IEventPublisher>();
                services.RemoveAll<INotificationClient>();
                services.RemoveAll<IImageStorageService>();
                services.RemoveAll<IPaymentClient>();

                services.AddSingleton<IEventPublisher, NoopEventPublisher>();
                services.AddSingleton<INotificationClient, NoopNotificationClient>();
                services.AddSingleton<IImageStorageService, NoopImageStorageService>();
                services.AddSingleton<IPaymentClient>(PaymentClient);
                services.AddSingleton(Substitute.For<IPublishEndpoint>());
            });
        }
    }

    private sealed class FakePaymentClient : IPaymentClient
    {
        public List<FundsReservation> ReservedFunds { get; } = new();
        public List<FundsRelease> ReleasedFunds { get; } = new();
        public List<WinnerCharge> WinnerCharges { get; } = new();
        public List<SellerTransfer> SellerTransfers { get; } = new();
        public List<FundsRefund> RefundedFunds { get; } = new();

        public Task<BalanceResult> GetBalanceAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult(new BalanceResult(true, 100_000m));
        }

        public Task<PaymentResult> ReserveFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
        {
            ReservedFunds.Add(new FundsReservation(userId, amount, lotId));
            return Task.FromResult(new PaymentResult(true));
        }

        public Task<PaymentResult> ReleaseFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
        {
            ReleasedFunds.Add(new FundsRelease(userId, amount, lotId));
            return Task.FromResult(new PaymentResult(true));
        }

        public Task<PaymentResult> ChargeWinnerAsync(Guid winnerId, decimal amount, Guid lotId, CancellationToken ct = default)
        {
            WinnerCharges.Add(new WinnerCharge(winnerId, amount, lotId));
            return Task.FromResult(new PaymentResult(true));
        }

        public Task<PaymentResult> TransferToSellerAsync(Guid sellerId, decimal amount, decimal serviceFee, Guid lotId, CancellationToken ct = default)
        {
            SellerTransfers.Add(new SellerTransfer(sellerId, amount, serviceFee, lotId));
            return Task.FromResult(new PaymentResult(true));
        }

        public Task<PaymentResult> RefundFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
        {
            RefundedFunds.Add(new FundsRefund(userId, amount, lotId));
            return Task.FromResult(new PaymentResult(true));
        }
    }

    private sealed record FundsReservation(Guid UserId, decimal Amount, Guid LotId);
    private sealed record FundsRelease(Guid UserId, decimal Amount, Guid LotId);
    private sealed record WinnerCharge(Guid WinnerId, decimal Amount, Guid LotId);
    private sealed record SellerTransfer(Guid SellerId, decimal Amount, decimal ServiceFee, Guid LotId);
    private sealed record FundsRefund(Guid UserId, decimal Amount, Guid LotId);

    private sealed class NoopEventPublisher : IEventPublisher
    {
        public Task PublishNewBidAsync(Guid lotId, decimal newPrice, string bidderName, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishLotCompletedAsync(Guid lotId, string title, decimal finalPrice, string? winnerName, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishUserNotificationAsync(Guid userId, string type, string message, Guid? lotId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopNotificationClient : INotificationClient
    {
        public Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopImageStorageService : IImageStorageService
    {
        public Task<string> UploadAsync(Stream fileStream, string objectName, string contentType, CancellationToken ct = default) => Task.FromResult(objectName);

        public Task<string> GetPresignedUrlAsync(string objectName, int expiresMinutes = 60, CancellationToken ct = default) => Task.FromResult($"http://localhost/{objectName}");

        public Task<(Stream Stream, string ContentType, long Size)> GetStreamAsync(string objectName, CancellationToken ct = default)
        {
            Stream stream = new MemoryStream();
            return Task.FromResult((stream, "application/octet-stream", 0L));
        }

        public Task DeleteAsync(string objectName, CancellationToken ct = default) => Task.CompletedTask;

        public Task InitializeBucketAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
