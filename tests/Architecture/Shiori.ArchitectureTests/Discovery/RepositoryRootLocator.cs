namespace Shiori.ArchitectureTests.Discovery;

internal static class RepositoryRootLocator
{
    private const string SolutionFileName = "Shiori.sln";
    private const string SourceDirectoryName = "src";

    public static string FindRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var solutionPath = Path.Combine(
                currentDirectory.FullName,
                SolutionFileName);

            var sourceDirectoryPath = Path.Combine(
                currentDirectory.FullName,
                SourceDirectoryName);

            if (File.Exists(solutionPath) &&
                Directory.Exists(sourceDirectoryPath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the Shiori repository root starting from " +
            $"'{AppContext.BaseDirectory}'. Expected to find both " +
            $"'{SolutionFileName}' and '{SourceDirectoryName}'.");
    }
}