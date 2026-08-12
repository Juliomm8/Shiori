using Shiori.ArchitectureTests.Baseline;
using Shiori.ArchitectureTests.Discovery;
using Shiori.ArchitectureTests.Model;

namespace Shiori.ArchitectureTests.Rules;

public sealed class AssemblyBoundaryTests
{
    private static readonly string RepositoryRoot =
        RepositoryRootLocator.FindRepositoryRoot();

    [Fact]
    public void Compiled_assembly_scan_should_cover_all_13_registered_projects()
    {
        var targets =
            DiscoverCompiledAssemblies();

        var expectedProjects =
            ProductionProjectRegistry.Projects
                .Select(project => project.Name)
                .OrderBy(
                    name => name,
                    StringComparer.Ordinal)
                .ToArray();

        var actualProjects =
            targets
                .Select(target => target.ProjectName)
                .OrderBy(
                    name => name,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(
            ProductionProjectRegistry.ExpectedProjectCount,
            targets.Count);

        Assert.Equal(
            expectedProjects,
            actualProjects);

        foreach (var target in targets)
        {
            var snapshot =
                CompiledAssemblyScanner.Scan(
                    target);

            Assert.Equal(
                target.ProjectName,
                snapshot.AssemblyName);
        }
    }

    [Fact]
    public void Compiled_shiori_dependencies_should_not_exceed_the_frozen_matrix()
    {
        var targets =
            DiscoverCompiledAssemblies();

        var registry =
            ProductionProjectRegistry.Projects
                .ToDictionary(
                    project => project.Name,
                    StringComparer.Ordinal);

        var violations =
            new List<string>();

        foreach (var target in targets)
        {
            var snapshot =
                CompiledAssemblyScanner.Scan(
                    target);

            var allowedReferences =
                registry[target.ProjectName]
                    .ExpectedProjectReferences
                    .ToHashSet(
                        StringComparer.Ordinal);

            var actualShioriReferences =
                snapshot.AssemblyReferences
                    .Where(reference =>
                        reference.StartsWith(
                            "Shiori.",
                            StringComparison.Ordinal))
                    .ToHashSet(
                        StringComparer.Ordinal);

            var unexpectedReferences =
                actualShioriReferences
                    .Except(
                        allowedReferences,
                        StringComparer.Ordinal)
                    .OrderBy(
                        reference => reference,
                        StringComparer.Ordinal)
                    .ToArray();

            foreach (var unexpectedReference in
                     unexpectedReferences)
            {
                violations.Add(
                    $"{target.ProjectName} compiled against " +
                    $"forbidden Shiori assembly " +
                    $"'{unexpectedReference}'.");
            }
        }

        AssertNoViolations(
            "Compiled Shiori assembly dependency boundaries were violated:",
            violations);
    }

    [Fact]
    public void Domain_and_application_compiled_code_should_not_use_forbidden_types()
    {
        var targets =
            DiscoverCompiledAssemblies()
                .Where(target =>
                    CompiledBoundaryPolicy
                        .IsDomainOrApplicationProject(
                            target.ProjectName))
                .ToArray();

        var violations =
            new List<string>();

        foreach (var target in targets)
        {
            var snapshot =
                CompiledAssemblyScanner.Scan(
                    target);

            foreach (var typeReference in
                     snapshot.TypeReferences)
            {
                var category =
                    CompiledBoundaryPolicy
                        .FindDomainOrApplicationViolation(
                            typeReference);

                if (category is null)
                {
                    continue;
                }

                violations.Add(
                    $"{target.ProjectName} uses forbidden compiled type " +
                    $"'{typeReference.TypeFullName}' " +
                    $"from '{typeReference.ScopeAssemblyName}' " +
                    $"[{category}].");
            }
        }

        AssertNoViolations(
            "Domain/Application compiled-type boundaries were violated:",
            violations);
    }

    [Fact]
    public void Application_public_contracts_should_not_expose_infrastructure_types()
    {
        var applicationTargets =
            DiscoverCompiledAssemblies()
                .Where(target =>
                    CompiledBoundaryPolicy
                        .IsApplicationProject(
                            target.ProjectName))
                .ToArray();

        Assert.Equal(
            3,
            applicationTargets.Length);

        var violations =
            new List<string>();

        foreach (var target in
                 applicationTargets)
        {
            var publicSurface =
                CompiledAssemblyScanner
                    .ScanPublicSurface(
                        target);

            foreach (var typeReference in
                     publicSurface)
            {
                var category =
                    CompiledBoundaryPolicy
                        .FindDomainOrApplicationViolation(
                            typeReference);

                if (category is null)
                {
                    continue;
                }

                violations.Add(
                    $"{target.ProjectName} public contract " +
                    $"'{typeReference.Owner}' exposes " +
                    $"'{typeReference.TypeFullName}' " +
                    $"[{category}].");
            }
        }

        AssertNoViolations(
            "Application public contracts expose forbidden infrastructure types:",
            violations);
    }

    [Fact]
    public void Api_should_not_use_domain_types_directly()
    {
        var apiTargets =
            DiscoverCompiledAssemblies()
                .Where(target =>
                    CompiledBoundaryPolicy
                        .IsApiProject(
                            target.ProjectName))
                .ToArray();

        Assert.Equal(
            3,
            apiTargets.Length);

        var violations =
            new List<string>();

        foreach (var target in
                 apiTargets)
        {
            var domainAssembly =
                CompiledBoundaryPolicy
                    .GetOwnDomainAssembly(
                        target.ProjectName);

            var snapshot =
                CompiledAssemblyScanner.Scan(
                    target);

            var domainTypeReferences =
                snapshot.TypeReferences
                    .Where(reference =>
                        reference.ScopeAssemblyName.Equals(
                            domainAssembly,
                            StringComparison.Ordinal))
                    .OrderBy(
                        reference =>
                            reference.TypeFullName,
                        StringComparer.Ordinal)
                    .ToArray();

            if (domainTypeReferences.Length > 0)
            {
                foreach (var reference in
                         domainTypeReferences)
                {
                    violations.Add(
                        $"{target.ProjectName} directly uses Domain type " +
                        $"'{reference.TypeFullName}' from " +
                        $"'{domainAssembly}'.");
                }

                continue;
            }

            if (snapshot.AssemblyReferences.Contains(
                    domainAssembly))
            {
                violations.Add(
                    $"{target.ProjectName} contains a direct compiled " +
                    $"assembly reference to '{domainAssembly}'.");
            }
        }

        AssertNoViolations(
            "API may not use Domain directly:",
            violations);
    }

    [Fact]
    public void Api_non_entrypoint_code_should_not_depend_on_infrastructure_implementations()
    {
        var apiTargets =
            DiscoverCompiledAssemblies()
                .Where(target =>
                    CompiledBoundaryPolicy
                        .IsApiProject(
                            target.ProjectName))
                .ToArray();

        Assert.Equal(
            3,
            apiTargets.Length);

        var violations =
            new List<string>();

        foreach (var target in
                 apiTargets)
        {
            var infrastructureAssembly =
                CompiledBoundaryPolicy
                    .GetOwnInfrastructureAssembly(
                        target.ProjectName);

            var methodUsages =
                CompiledAssemblyScanner
                    .ScanMethodTypeUsages(
                        target,
                        skipModuleEntryPoint: true);

            var forbiddenUsages =
                methodUsages
                    .Where(usage =>
                        usage.ScopeAssemblyName.Equals(
                            infrastructureAssembly,
                            StringComparison.Ordinal))
                    .OrderBy(
                        usage =>
                            usage.MethodFullName,
                        StringComparer.Ordinal)
                    .ThenBy(
                        usage =>
                            usage.TypeFullName,
                        StringComparer.Ordinal)
                    .ToArray();

            foreach (var usage in
                     forbiddenUsages)
            {
                violations.Add(
                    $"{target.ProjectName} method " +
                    $"'{usage.MethodFullName}' depends directly on " +
                    $"Infrastructure type " +
                    $"'{usage.TypeFullName}'.");
            }
        }

        AssertNoViolations(
            "API code outside the executable composition entry point " +
            "may not depend directly on Infrastructure implementation types:",
            violations);
    }

    private static IReadOnlyList<CompiledAssemblyTarget>
        DiscoverCompiledAssemblies()
    {
        var discoveredProjects =
            ProjectDiscovery
                .DiscoverProductionProjects(
                    RepositoryRoot);

        var targets =
            CompiledAssemblyDiscovery.Discover(
                discoveredProjects);

        if (targets.Count == 0)
        {
            throw new InvalidOperationException(
                "Compiled assembly discovery returned zero targets.");
        }

        return targets;
    }

    private static void AssertNoViolations(
        string heading,
        IReadOnlyCollection<string> violations)
    {
        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? heading
                : heading +
                  Environment.NewLine +
                  string.Join(
                      Environment.NewLine,
                      violations.Select(
                          violation =>
                              $" - {violation}")));
    }
}