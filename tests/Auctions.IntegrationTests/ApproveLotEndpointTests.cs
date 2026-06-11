using System.Security.Claims;
using Auctions.API.Endpoints.Lots;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using AuHub.Shared.ValueObjects;
using FastEndpoints;
using FluentAssertions;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class ApproveLotEndpointTests
{
    [Fact]
    public async Task HandleAsync_ApprovedLot_WritesAdminAuditLog()
    {
        var adminId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var lot = Lot.Create("Lot", "Description", Money.FromDecimal(100m), TimeSpan.FromDays(1), sellerId, [DeliveryProvider.Cdek]);
        lot.SubmitForModeration();

        var lotRepository = Substitute.For<ILotRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var notificationClient = Substitute.For<INotificationClient>();
        var auditLogRepository = Substitute.For<IAdminAuditLogRepository>();

        lotRepository.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        lotRepository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        notificationClient.SendNotificationAsync(Arg.Any<Guid>(), Arg.Any<NotificationType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        eventPublisher.PublishUserNotificationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        auditLogRepository.AddAsync(Arg.Any<AdminAuditLog>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        auditLogRepository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var endpoint = Factory.Create<ApproveLotEndpoint>(ctx =>
        {
            ctx.Request.RouteValues["id"] = lot.Id;
            ctx.User = CreateAdmin(adminId);
        }, lotRepository, eventPublisher, notificationClient, auditLogRepository);

        await endpoint.HandleAsync(CancellationToken.None);

        await auditLogRepository.Received(1).AddAsync(
            Arg.Is<AdminAuditLog>(log =>
                log.ActorUserId == adminId &&
                log.Action == "LotApprove" &&
                log.TargetType == "Lot" &&
                log.TargetId == lot.Id),
            Arg.Any<CancellationToken>());
        await auditLogRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ClaimsPrincipal CreateAdmin(Guid userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "Test");

        return new ClaimsPrincipal(identity);
    }
}
