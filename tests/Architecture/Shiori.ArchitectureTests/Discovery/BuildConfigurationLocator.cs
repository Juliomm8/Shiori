namespace Shiori.ArchitectureTests.Discovery;

internal static class BuildConfigurationLocator
{
    public static string GetCurrentConfiguration()
    {
        var architectureTestAssemblyPath =
            typeof(BuildConfigurationLocator).Assembly.Location;

        if (string.IsNullOrWhiteSpace(
                architectureTestAssemblyPath))
        {
            throw new InvalidOperationException(
                "Could not determine the Architecture Tests assembly location.");
        }

        var assemblyDirectoryPath =
            Path.GetDirectoryName(
                architectureTestAssemblyPath);

        if (string.IsNullOrWhiteSpace(
                assemblyDirectoryPath))
        {
            throw new InvalidOperationException(
                $"Could not determine the directory for " +
                $"'{architectureTestAssemblyPath}'.");
        }

        var currentDirectory =
            new DirectoryInfo(
                assemblyDirectoryPath);

        while (currentDirectory.Parent is not null)
        {
            if (currentDirectory.Parent.Name.Equals(
                    "bin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return currentDirectory.Name;
            }

            currentDirectory =
                currentDirectory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not infer the active build configuration from " +
            $"Architecture Tests assembly path " +
            $"'{architectureTestAssemblyPath}'.");
    }
}