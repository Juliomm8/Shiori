namespace Shiori.ArchitectureTests.Discovery;

internal sealed record DiscoveredProject(
    string Name,
    string RelativePath,
    string FullPath);

internal static class ProjectDiscovery
{
    private const string SourceDirectoryName = "src";

    public static IReadOnlyList<DiscoveredProject> DiscoverProductionProjects(
        string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var sourceDirectory = Path.Combine(
            repositoryRoot,
            SourceDirectoryName);

        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Production source directory was not found: '{sourceDirectory}'.");
        }

        var projectFiles = Directory
            .EnumerateFiles(
                sourceDirectory,
                "*.csproj",
                SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (projectFiles.Length == 0)
        {
            throw new InvalidOperationException(
                $"Architecture discovery found zero production projects under " +
                $"'{sourceDirectory}'. The architecture scan cannot pass with " +
                $"zero discovered targets.");
        }

        return projectFiles
            .Select(projectPath => new DiscoveredProject(
                Name: Path.GetFileNameWithoutExtension(projectPath),
                RelativePath: NormalizeRelativePath(
                    repositoryRoot,
                    projectPath),
                FullPath: Path.GetFullPath(projectPath)))
            .ToArray();
    }

    private static string NormalizeRelativePath(
        string repositoryRoot,
        string projectPath)
    {
        return Path
            .GetRelativePath(repositoryRoot, projectPath)
            .Replace('\\', '/');
    }
}