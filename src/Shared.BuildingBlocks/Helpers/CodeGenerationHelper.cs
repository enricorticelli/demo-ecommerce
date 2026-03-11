namespace Shared.BuildingBlocks.Helpers;

public static class CodeGenerationHelper
{
    public static string GenerateNumericCode(int digits)
    {
        if (digits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), "Digits must be greater than zero.");
        }

        var min = (int)Math.Pow(10, digits - 1);
        var max = (int)Math.Pow(10, digits) - 1;
        var number = Random.Shared.Next(min, max + 1);
        return number.ToString();
    }
}
