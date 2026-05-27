using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using Payment.Application.Queries.GetBalance;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Payment.UnitTests;

public class GetBalanceQueryHandlerTests
{
    private readonly IWalletRepository _walletRepo;
    private readonly GetBalanceQueryHandler _handler;

    public GetBalanceQueryHandlerTests()
    {
        _walletRepo = Substitute.For<IWalletRepository>();
        _handler = new GetBalanceQueryHandler(_walletRepo);
    }

    private GetBalanceQuery CreateQuery(Guid? userId = null)
    {
        return new GetBalanceQuery { UserId = userId ?? Guid.NewGuid() };
    }

    [Fact]
    public async Task HandleAsync_ExistingWallet_ReturnsBalance()
    {
        var userId = Guid.NewGuid();
        var wallet = Wallet.Create(userId);
        wallet.Deposit(Money.FromDecimal(500));
        wallet.Freeze(Money.FromDecimal(100));
        _walletRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(CreateQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Balance.Should().Be(Money.FromDecimal(400));
        result.Value.FrozenBalance.Should().Be(Money.FromDecimal(100));
        result.Value.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task HandleAsync_NoWallet_ReturnsZeroBalance()
    {
        var userId = Guid.NewGuid();
        _walletRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var result = await _handler.HandleAsync(CreateQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Balance.Should().Be(Money.Zero);
        result.Value.FrozenBalance.Should().Be(Money.Zero);
        result.Value.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _walletRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateQuery());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to get balance");
        result.StatusCode.Should().Be(500);
    }
}
