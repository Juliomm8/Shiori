using Shiori.ArchitectureTests.Baseline;
using Shiori.ArchitectureTests.Discovery;

namespace Shiori.ArchitectureTests.Rules;

public sealed class ProductionRegistryTests
{
    private static readonly string RepositoryRoot =
        RepositoryRootLocator.FindRepositoryRoot();

    private static readonly string[] ForbiddenSharedProjectSegments =
    [
        "Shared",
        "Common",
        "Core",
        "SharedKernel"
    ];

    [Fact]
    public void Production_registry_should_contain_exactly_13_unique_projects()
    {
        var projects = ProductionProjectRegistry.Projects;

        Assert.Equal(
            ProductionProjectRegistry.ExpectedProjectCount,
            projects.Count);

        var duplicateNames = projects
            .GroupBy(
                project => project.Name,
                StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var duplicatePaths = projects
            .GroupBy(
                project => project.RelativePath,
                StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicateNames.Length == 0,
            $"Production registry contains duplicate project names:{Environment.NewLine}" +
            FormatLines(duplicateNames));

        Assert.True(
            duplicatePaths.Length == 0,
            $"Production registry contains duplicate project paths:{Environment.NewLine}" +
            FormatLines(duplicatePaths));
    }

    [Fact]
    public void Discovered_production_projects_should_match_the_frozen_registry_exactly()
    {
        var expectedProjects = ProductionProjectRegistry.Projects;

        var discoveredProjects =
            ProjectDiscovery.DiscoverProductionProjects(RepositoryRoot);

        var expectedPaths = expectedProjects
            .Select(project => project.RelativePath)
            .ToHashSet(StringComparer.Ordinal);

        var actualPaths = discoveredProjects
            .Select(project => project.RelativePath)
            .ToHashSet(StringComparer.Ordinal);

        var missingProjects = expectedPaths
            .Except(actualPaths, StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var unexpectedProjects = actualPaths
            .Except(expectedPaths, StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missingProjects.Length == 0 &&
            unexpectedProjects.Length == 0,
            BuildRegistryMismatchMessage(
                missingProjects,
                unexpectedProjects));

        Assert.Equal(
            ProductionProjectRegistry.ExpectedProjectCount,
            discoveredProjects.Count);
    }

    [Fact]
    public void Production_source_should_not_contain_unapproved_worker_or_shared_projects()
    {
        var discoveredProjects =
            ProjectDiscovery.DiscoverProductionProjects(RepositoryRoot);

        var forbiddenProjects = discoveredProjects
            .Where(project =>
                IsWorkerProject(project.Name) ||
                IsGenericSharedProject(project.Name))
            .Select(project => project.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            forbiddenProjects.Length == 0,
            $"Unapproved Worker or generic shared production projects were found:" +
            $"{Environment.NewLine}{FormatLines(forbiddenProjects)}");
    }

    [Fact]
    public void Project_discovery_should_fail_closed_when_zero_projects_are_found()
    {
        var temporaryRepositoryRoot = Path.Combine(
            Path.GetTempPath(),
            $"shiori-architecture-tests-{Guid.NewGuid():N}");

        var sourceDirectory = Path.Combine(
            temporaryRepositoryRoot,
            "src");

        Directory.CreateDirectory(sourceDirectory);

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => ProjectDiscovery.DiscoverProductionProjects(
                    temporaryRepositoryRoot));

            Assert.Contains(
                "zero production projects",
                exception.Message,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(temporaryRepositoryRoot))
            {
                Directory.Delete(
                    temporaryRepositoryRoot,
                    recursive: true);
            }
        }
    }

    private static bool IsWorkerProject(string projectName)
    {
        var segments = SplitProjectName(projectName);

        return segments.Any(segment =>
            segment.Equals(
                "Worker",
                StringComparison.OrdinalIgnoreCase) ||
            segment.Equals(
                "Workers",
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGenericSharedProject(string projectName)
    {
        var segments = SplitProjectName(projectName);

        return segments.Any(segment =>
            ForbiddenSharedProjectSegments.Contains(
                segment,
                StringComparer.OrdinalIgnoreCase));
    }

    private static string[] SplitProjectName(string projectName)
    {
        return projectName.Split(
            '.',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
    }

    private static string BuildRegistryMismatchMessage(
        IReadOnlyCollection<string> missingProjects,
        IReadOnlyCollection<string> unexpectedProjects)
    {
        var sections = new List<string>
        {
            "The discovered production-project registry does not match the frozen Shiori architecture."
        };

        if (missingProjects.Count > 0)
        {
            sections.Add(
                $"Missing production projects:{Environment.NewLine}" +
                FormatLines(missingProjects));
        }

        if (unexpectedProjects.Count > 0)
        {
            sections.Add(
                $"Unexpected production projects:{Environment.NewLine}" +
                FormatLines(unexpectedProjects));
        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            sections);
    }

    private static string FormatLines(
        IEnumerable<string> values)
    {
        var lines = values
            .Select(value => $" - {value}")
            .ToArray();

        return lines.Length == 0
            ? " - none"
            : string.Join(Environment.NewLine, lines);
    }
}