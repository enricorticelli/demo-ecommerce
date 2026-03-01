using FluentValidation;

namespace Order.Api.Application;

public sealed record CreateOrderCommand(Guid CartId, Guid UserId);

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
