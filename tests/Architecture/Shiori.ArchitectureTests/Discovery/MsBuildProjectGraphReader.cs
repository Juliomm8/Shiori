using Shiori.ArchitectureTests.Model;

namespace Shiori.ArchitectureTests.Discovery;

internal static class MsBuildProjectGraphReader
{
    public static ProjectGraph Read(
        IReadOnlyList<DiscoveredProject> discoveredProjects)
    {
        ArgumentNullException.ThrowIfNull(discoveredProjects);

        if (discoveredProjects.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot evaluate the production project graph because " +
                "zero projects were supplied.");
        }

        MsBuildBootstrapper.EnsureRegistered();

        return MsBuildProjectGraphReaderCore.Read(
            discoveredProjects);
    }
}

internal static class MsBuildProjectGraphReaderCore
{
    public static ProjectGraph Read(
        IReadOnlyList<DiscoveredProject> discoveredProjects)
    {
        using var projectCollection =
            new Microsoft.Build.Evaluation.ProjectCollection();

        var pathComparer = GetPathComparer();

        var knownProjectsByFullPath = discoveredProjects
            .ToDictionary(
                project => NormalizeFullPath(project.FullPath),
                project => project.Name,
                pathComparer);

        var nodes = new List<ProjectNode>(
            discoveredProjects.Count);

        foreach (var discoveredProject in discoveredProjects
                     .OrderBy(
                         project => project.Name,
                         StringComparer.Ordinal))
        {
            var evaluatedProject =
                projectCollection.LoadProject(
                    discoveredProject.FullPath);

            try
            {
                var projectReferences = evaluatedProject
                    .GetItems("ProjectReference")
                    .Select(reference =>
                        ResolveReferencedProjectPath(
                            discoveredProject.FullPath,
                            reference.EvaluatedInclude))
                    .Select(NormalizeFullPath)
                    .Select(referencePath =>
                    {
                        if (!knownProjectsByFullPath.TryGetValue(
                                referencePath,
                                out var referencedProjectName))
                        {
                            throw new InvalidOperationException(
                                $"Production project '{discoveredProject.Name}' " +
                                $"references a project outside the discovered " +
                                $"production registry: '{referencePath}'.");
                        }

                        return referencedProjectName;
                    })
                    .ToHashSet(StringComparer.Ordinal);

                nodes.Add(
                    new ProjectNode(
                        discoveredProject.Name,
                        discoveredProject.FullPath,
                        projectReferences));
            }
            finally
            {
                projectCollection.UnloadProject(
                    evaluatedProject);
            }
        }

        return new ProjectGraph(nodes);
    }

    private static string ResolveReferencedProjectPath(
        string sourceProjectPath,
        string evaluatedInclude)
    {
        if (string.IsNullOrWhiteSpace(evaluatedInclude))
        {
            throw new InvalidOperationException(
                $"A ProjectReference in '{sourceProjectPath}' " +
                "evaluated to an empty path.");
        }

        var sourceDirectory =
            Path.GetDirectoryName(sourceProjectPath);

        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            throw new InvalidOperationException(
                $"Could not determine the directory of " +
                $"project '{sourceProjectPath}'.");
        }

        return Path.GetFullPath(
            Path.Combine(
                sourceDirectory,
                evaluatedInclude));
    }

    private static string NormalizeFullPath(string path)
    {
        return Path
            .GetFullPath(path)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }
}