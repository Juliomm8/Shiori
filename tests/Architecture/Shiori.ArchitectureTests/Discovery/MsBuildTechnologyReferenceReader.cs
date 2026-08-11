using Shiori.ArchitectureTests.Model;

namespace Shiori.ArchitectureTests.Discovery;

internal static class MsBuildTechnologyReferenceReader
{
    public static IReadOnlyList<ProjectTechnologySnapshot> Read(
        IReadOnlyList<DiscoveredProject> discoveredProjects)
    {
        ArgumentNullException.ThrowIfNull(discoveredProjects);

        if (discoveredProjects.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot evaluate technology references because " +
                "zero production projects were supplied.");
        }

        MsBuildBootstrapper.EnsureRegistered();

        return MsBuildTechnologyReferenceReaderCore.Read(
            discoveredProjects);
    }
}

internal static class MsBuildTechnologyReferenceReaderCore
{
    public static IReadOnlyList<ProjectTechnologySnapshot> Read(
        IReadOnlyList<DiscoveredProject> discoveredProjects)
    {
        using var projectCollection =
            new Microsoft.Build.Evaluation.ProjectCollection();

        var snapshots = new List<ProjectTechnologySnapshot>(
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
                var packageReferences =
                    ReadEvaluatedItems(
                        evaluatedProject,
                        discoveredProject.Name,
                        "PackageReference");

                var frameworkReferences =
                    ReadEvaluatedItems(
                        evaluatedProject,
                        discoveredProject.Name,
                        "FrameworkReference");

                snapshots.Add(
                    new ProjectTechnologySnapshot(
                        discoveredProject.Name,
                        packageReferences,
                        frameworkReferences));
            }
            finally
            {
                projectCollection.UnloadProject(
                    evaluatedProject);
            }
        }

        if (snapshots.Count != discoveredProjects.Count)
        {
            throw new InvalidOperationException(
                $"Technology-reference discovery evaluated " +
                $"{snapshots.Count} project(s), but " +
                $"{discoveredProjects.Count} production project(s) " +
                $"were supplied.");
        }

        return snapshots;
    }

    private static IReadOnlySet<string> ReadEvaluatedItems(
        Microsoft.Build.Evaluation.Project evaluatedProject,
        string projectName,
        string itemType)
    {
        var references =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var item in evaluatedProject.GetItems(itemType))
        {
            var evaluatedInclude =
                item.EvaluatedInclude?.Trim();

            if (string.IsNullOrWhiteSpace(evaluatedInclude))
            {
                throw new InvalidOperationException(
                    $"{itemType} in project '{projectName}' " +
                    "evaluated to an empty identifier.");
            }

            references.Add(evaluatedInclude);
        }

        return references;
    }
}