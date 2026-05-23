using AuHub.Shared.Results;
using FluentAssertions;

namespace Shared.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var result = Result.Failure("Something went wrong", 400);
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Something went wrong");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public void Failure_DefaultsTo400()
    {
        var result = Result.Failure("error");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public void GenericSuccess_CreatesResultWithValue()
    {
        var result = Result.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_CreatesFailedResultWithoutValue()
    {
        var result = Result.Failure<int>("not found", 404);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("not found");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public void IsFailure_IsOppositeOfIsSuccess()
    {
        var success = Result.Success();
        var failure = Result.Failure("err");

        success.IsFailure.Should().Be(!success.IsSuccess);
        failure.IsFailure.Should().Be(!failure.IsSuccess);
    }
}
