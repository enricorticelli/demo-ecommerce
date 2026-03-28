namespace Shared.BuildingBlocks.Configuration;

public static class EnvironmentVariableReader
{
    public static string ResolveRequired(string key)
    {
        return Resolve(key) ??
               throw new InvalidOperationException(
                   $"Configuration key '{key}' not found in environment variables or .env file.");
    }

    public static string? Resolve(string key)
    {
        return Environment.GetEnvironmentVariable(key)
               ?? TryReadFromDotEnv(key);
    }

    private static string? TryReadFromDotEnv(string key)
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var envPath = Path.Combine(current.FullName, ".env");
            var value = TryReadKeyFromFile(envPath, key);
            if (value is not null)
            {
                return value;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? TryReadKeyFromFile(string envPath, string key)
    {
        return !File.Exists(envPath)
            ? null
            : File.ReadLines(envPath).Select(rawLine => TryMatchLine(rawLine, key)).OfType<string>().FirstOrDefault();
    }

    private static string? TryMatchLine(string rawLine, string key)
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
        {
            return null;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var name = line[..separatorIndex];
        return string.Equals(name, key, StringComparison.Ordinal)
            ? line[(separatorIndex + 1)..]
            : null;
    }
}