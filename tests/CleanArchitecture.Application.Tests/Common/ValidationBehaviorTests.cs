using CleanArchitecture.Application.Common.Behaviors;
using CleanArchitecture.Application.Common.Exceptions;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Moq;

namespace CleanArchitecture.Application.Tests.Common;

public class ValidationBehaviorTests
{
    public record SampleCommand(string Name) : IRequest<int>;

    public class SampleCommandValidator : AbstractValidator<SampleCommand>
    {
        public SampleCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        // Arrange
        var validators = new List<IValidator<SampleCommand>> { new SampleCommandValidator() };
        var behavior = new ValidationBehavior<SampleCommand, int>(validators);
        var command = new SampleCommand(string.Empty);

        var nextDelegate = new Mock<RequestHandlerDelegate<int>>();

        // Act
        Func<Task> act = async () => await behavior.Handle(command, nextDelegate.Object, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<CleanArchitecture.Application.Common.Exceptions.ValidationException>();
        exception.Which.Errors.Should().ContainKey("Name");
    }
}
