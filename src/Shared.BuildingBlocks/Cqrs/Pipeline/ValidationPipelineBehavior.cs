using System.ComponentModel.DataAnnotations;

namespace Shared.BuildingBlocks.Cqrs;

public sealed class ValidationPipelineBehavior<TRequest, TResponse>(IEnumerable<IRequestValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var allErrors = validators
            .Select(v => v.Validate(request))
            .Where(errors => errors.Count > 0)
            .SelectMany(errors => errors)
            .GroupBy(pair => pair.Key)
            .ToDictionary(group => group.Key, group => group.SelectMany(x => x.Value).Distinct().ToArray());

        var annotationErrors = ValidateDataAnnotations(request);
        foreach (var error in annotationErrors)
        {
            if (allErrors.TryGetValue(error.Key, out var existing))
            {
                allErrors[error.Key] = existing.Concat(error.Value).Distinct().ToArray();
                continue;
            }

            allErrors[error.Key] = error.Value;
        }

        if (allErrors.Count > 0)
        {
            throw new RequestValidationException(allErrors);
        }

        return next();
    }

    private static Dictionary<string, string[]> ValidateDataAnnotations(TRequest request)
    {
        if (request is null)
        {
            return new Dictionary<string, string[]>();
        }

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        return results
            .SelectMany(result =>
            {
                var message = result.ErrorMessage ?? "The value is invalid.";
                return result.MemberNames.Any()
                    ? result.MemberNames.Select(name => new KeyValuePair<string, string>(name, message))
                    : [new KeyValuePair<string, string>("request", message)];
            })
            .GroupBy(pair => pair.Key)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).Distinct().ToArray());
    }
}
