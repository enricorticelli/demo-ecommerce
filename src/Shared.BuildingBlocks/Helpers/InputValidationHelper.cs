using Shared.BuildingBlocks.Exceptions;

namespace Shared.BuildingBlocks.Helpers;

public static class InputValidationHelper
{
    public static string NormalizeRequiredLowerInvariant(string value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationAppException($"{fieldLabel} is required.");
        }

        return value.Trim().ToLowerInvariant();
    }

    public static void EnsureMinLength(string value, int minLength, string message)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < minLength)
        {
            throw new ValidationAppException(message);
        }
    }
}
