using Shiori.ArchitectureTests.Baseline;
using Shiori.ArchitectureTests.Discovery;

namespace Shiori.ArchitectureTests.Rules;

public sealed class ProjectDependencyTests
{
    private static readonly string RepositoryRoot =
        RepositoryRootLocator.FindRepositoryRoot();

    [Fact]
    public void Production_projects_should_follow_the_frozen_reference_matrix()
    {
        var discoveredProjects =
            ProjectDiscovery.DiscoverProductionProjects(
                RepositoryRoot);

        var graph =
            MsBuildProjectGraphReader.Read(
                discoveredProjects);

        var violations = new List<string>();

        foreach (var expectedProject in
                 ProductionProjectRegistry.Projects)
        {
            var actualProject =
                graph.GetProject(expectedProject.Name);

            var expectedReferences =
                expectedProject.ExpectedProjectReferences
                    .ToHashSet(StringComparer.Ordinal);

            var unexpectedReferences =
                actualProject.ProjectReferences
                    .Except(
                        expectedReferences,
                        StringComparer.Ordinal)
                    .OrderBy(
                        reference => reference,
                        StringComparer.Ordinal)
                    .ToArray();

            var missingReferences =
                expectedReferences
                    .Except(
                        actualProject.ProjectReferences,
                        StringComparer.Ordinal)
                    .OrderBy(
                        reference => reference,
                        StringComparer.Ordinal)
                    .ToArray();

            if (unexpectedReferences.Length > 0)
            {
                violations.Add(
                    $"{expectedProject.Name} has forbidden ProjectReference(s): " +
                    string.Join(
                        ", ",
                        unexpectedReferences));
            }

            if (missingReferences.Length > 0)
            {
                violations.Add(
                    $"{expectedProject.Name} is missing required ProjectReference(s): " +
                    string.Join(
                        ", ",
                        missingReferences));
            }
        }

        Assert.True(
            violations.Count == 0,
            "The frozen production ProjectReference matrix was violated:" +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                violations.Select(
                    violation => $" - {violation}")));
    }

    [Fact]
    public void Production_project_graph_should_not_contain_cycles()
    {
        var discoveredProjects =
            ProjectDiscovery.DiscoverProductionProjects(
                RepositoryRoot);

        var graph =
            MsBuildProjectGraphReader.Read(
                discoveredProjects);

        var cycle = graph.FindCycle();

        Assert.True(
            cycle.Count == 0,
            "A production project dependency cycle was detected:" +
            Environment.NewLine +
            " - " +
            string.Join(
                " -> ",
                cycle));
    }
}