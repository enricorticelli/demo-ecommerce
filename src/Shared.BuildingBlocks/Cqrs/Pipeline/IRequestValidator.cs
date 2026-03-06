namespace Shared.BuildingBlocks.Cqrs;

public interface IRequestValidator<in TRequest>
{
    IReadOnlyDictionary<string, string[]> Validate(TRequest request);
}
