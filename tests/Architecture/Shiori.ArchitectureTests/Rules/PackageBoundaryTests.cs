using Shiori.ArchitectureTests.Baseline;
using Shiori.ArchitectureTests.Discovery;
using Shiori.ArchitectureTests.Model;

namespace Shiori.ArchitectureTests.Rules;

public sealed class PackageBoundaryTests
{
    private static readonly string RepositoryRoot =
        RepositoryRootLocator.FindRepositoryRoot();

    [Fact]
    public void Technology_reference_scan_should_cover_all_registered_production_projects()
    {
        var snapshots =
            ReadTechnologySnapshots();

        var expectedProjects =
            ProductionProjectRegistry.Projects
                .Select(project => project.Name)
                .OrderBy(
                    name => name,
                    StringComparer.Ordinal)
                .ToArray();

        var actualProjects =
            snapshots.Keys
                .OrderBy(
                    name => name,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(
            expectedProjects,
            actualProjects);
    }

    [Fact]
    public void Domain_and_application_should_not_reference_forbidden_technology_packages()
    {
        var snapshots =
            ReadTechnologySnapshots();

        var governedProjects =
            ProductionProjectRegistry.Projects
                .Where(project =>
                    PackageBoundaryPolicy
                        .IsDomainOrApplicationProject(
                            project.Name))
                .ToArray();

        var violations = new List<string>();

        foreach (var project in governedProjects)
        {
            if (!snapshots.TryGetValue(
                    project.Name,
                    out var snapshot))
            {
                violations.Add(
                    $"{project.Name} was not evaluated.");

                continue;
            }

            foreach (var packageId in
                     snapshot.PackageReferences
                         .OrderBy(
                             package => package,
                             StringComparer.OrdinalIgnoreCase))
            {
                var forbiddenRule =
                    PackageBoundaryPolicy
                        .FindDomainOrApplicationViolation(
                            packageId);

                if (forbiddenRule is null)
                {
                    continue;
                }

                violations.Add(
                    $"{project.Name} references forbidden package " +
                    $"'{packageId}' " +
                    $"[{forbiddenRule.Category}].");
            }

            if (snapshot.FrameworkReferences.Contains(
                    PackageBoundaryPolicy
                        .AspNetCoreFrameworkReference))
            {
                violations.Add(
                    $"{project.Name} references forbidden framework " +
                    $"'{PackageBoundaryPolicy.AspNetCoreFrameworkReference}' " +
                    "[HTTP transport / ASP.NET Core].");
            }
        }

        Assert.True(
            violations.Count == 0,
            BuildViolationMessage(
                "Domain/Application technology boundaries were violated:",
                violations));
    }

    [Fact]
    public void Gateway_should_not_reference_persistence_broker_or_provider_packages()
    {
        var snapshots =
            ReadTechnologySnapshots();

        const string gatewayProjectName =
            "Shiori.Gateway";

        Assert.True(
            snapshots.TryGetValue(
                gatewayProjectName,
                out var gatewaySnapshot),
            $"Expected production project " +
            $"'{gatewayProjectName}' was not evaluated.");

        var violations = new List<string>();

        foreach (var packageId in
                 gatewaySnapshot!.PackageReferences
                     .OrderBy(
                         package => package,
                         StringComparer.OrdinalIgnoreCase))
        {
            var forbiddenRule =
                PackageBoundaryPolicy
                    .FindGatewayViolation(
                        packageId);

            if (forbiddenRule is null)
            {
                continue;
            }

            violations.Add(
                $"{gatewayProjectName} references forbidden package " +
                $"'{packageId}' " +
                $"[{forbiddenRule.Category}].");
        }

        Assert.True(
            violations.Count == 0,
            BuildViolationMessage(
                "Gateway technology boundaries were violated:",
                violations));
    }

    [Fact]
    public void Production_projects_should_not_hide_implementation_dependencies_behind_nuget_packages()
    {
        var snapshots =
            ReadTechnologySnapshots();

        var violations = new List<string>();

        foreach (var snapshot in snapshots.Values
                     .OrderBy(
                         snapshot => snapshot.ProjectName,
                         StringComparer.Ordinal))
        {
            foreach (var packageId in
                     snapshot.PackageReferences
                         .OrderBy(
                             package => package,
                             StringComparer.OrdinalIgnoreCase))
            {
                if (!PackageBoundaryPolicy
                        .TryMatchProductionImplementationPackage(
                            packageId,
                            out var hiddenProjectName))
                {
                    continue;
                }

                violations.Add(
                    $"{snapshot.ProjectName} references package " +
                    $"'{packageId}', which hides the production " +
                    $"implementation boundary '{hiddenProjectName}'.");
            }
        }

        Assert.True(
            violations.Count == 0,
            BuildViolationMessage(
                "Production implementation dependencies may not be hidden behind NuGet packages:",
                violations));
    }

    private static IReadOnlyDictionary<string, ProjectTechnologySnapshot>
        ReadTechnologySnapshots()
    {
        var discoveredProjects =
            ProjectDiscovery.DiscoverProductionProjects(
                RepositoryRoot);

        var snapshots =
            MsBuildTechnologyReferenceReader.Read(
                discoveredProjects);

        if (snapshots.Count == 0)
        {
            throw new InvalidOperationException(
                "Technology-reference discovery returned zero " +
                "production project snapshots.");
        }

        return snapshots.ToDictionary(
            snapshot => snapshot.ProjectName,
            StringComparer.Ordinal);
    }

    private static string BuildViolationMessage(
        string heading,
        IReadOnlyCollection<string> violations)
    {
        if (violations.Count == 0)
        {
            return heading;
        }

        return heading +
               Environment.NewLine +
               string.Join(
                   Environment.NewLine,
                   violations.Select(
                       violation => $" - {violation}"));
    }
}