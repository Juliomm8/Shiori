using Shiori.ArchitectureTests.Model;

namespace Shiori.ArchitectureTests.Discovery;

internal static class CompiledAssemblyDiscovery
{
    public static IReadOnlyList<CompiledAssemblyTarget> Discover(
        IReadOnlyList<DiscoveredProject> discoveredProjects)
    {
        ArgumentNullException.ThrowIfNull(
            discoveredProjects);

        if (discoveredProjects.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot discover compiled assemblies because " +
                "zero production projects were supplied.");
        }

        var configuration =
            BuildConfigurationLocator
                .GetCurrentConfiguration();

        MsBuildBootstrapper.EnsureRegistered();

        return CompiledAssemblyDiscoveryCore.Discover(
            discoveredProjects,
            configuration);
    }
}

internal static class CompiledAssemblyDiscoveryCore
{
    public static IReadOnlyList<CompiledAssemblyTarget> Discover(
        IReadOnlyList<DiscoveredProject> discoveredProjects,
        string configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            configuration);

        using var projectCollection =
            new Microsoft.Build.Evaluation.ProjectCollection();

        projectCollection.SetGlobalProperty(
            "Configuration",
            configuration);

        var targets =
            new List<CompiledAssemblyTarget>(
                discoveredProjects.Count);

        foreach (var discoveredProject in
                 discoveredProjects.OrderBy(
                     project => project.Name,
                     StringComparer.Ordinal))
        {
            var evaluatedProject =
                projectCollection.LoadProject(
                    discoveredProject.FullPath);

            try
            {
                var assemblyName =
                    evaluatedProject
                        .GetPropertyValue("AssemblyName")
                        .Trim();

                if (string.IsNullOrWhiteSpace(
                        assemblyName))
                {
                    throw new InvalidOperationException(
                        $"Project '{discoveredProject.Name}' " +
                        "evaluated to an empty AssemblyName.");
                }

                if (!assemblyName.Equals(
                        discoveredProject.Name,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Production project " +
                        $"'{discoveredProject.Name}' produces " +
                        $"assembly '{assemblyName}'. " +
                        "Production project and assembly names " +
                        "must remain aligned.");
                }

                var targetPath =
                    evaluatedProject
                        .GetPropertyValue("TargetPath")
                        .Trim();

                if (string.IsNullOrWhiteSpace(
                        targetPath))
                {
                    throw new InvalidOperationException(
                        $"Project '{discoveredProject.Name}' " +
                        "evaluated to an empty TargetPath.");
                }

                var projectDirectory =
                    Path.GetDirectoryName(
                        discoveredProject.FullPath);

                if (string.IsNullOrWhiteSpace(
                        projectDirectory))
                {
                    throw new InvalidOperationException(
                        $"Could not determine the directory for " +
                        $"project '{discoveredProject.Name}'.");
                }

                var fullTargetPath =
                    Path.IsPathRooted(targetPath)
                        ? Path.GetFullPath(targetPath)
                        : Path.GetFullPath(
                            Path.Combine(
                                projectDirectory,
                                targetPath));

                if (!File.Exists(fullTargetPath))
                {
                    throw new FileNotFoundException(
                        $"Expected compiled assembly for " +
                        $"'{discoveredProject.Name}' was not found. " +
                        $"Build configuration: '{configuration}'. " +
                        $"Expected path: '{fullTargetPath}'. " +
                        "Architecture Tests fail closed when a " +
                        "production assembly is missing.",
                        fullTargetPath);
                }

                targets.Add(
                    new CompiledAssemblyTarget(
                        ProjectName:
                            discoveredProject.Name,
                        ExpectedAssemblyName:
                            assemblyName,
                        ProjectPath:
                            discoveredProject.FullPath,
                        AssemblyPath:
                            fullTargetPath));
            }
            finally
            {
                projectCollection.UnloadProject(
                    evaluatedProject);
            }
        }

        if (targets.Count !=
            discoveredProjects.Count)
        {
            throw new InvalidOperationException(
                $"Compiled assembly discovery returned " +
                $"{targets.Count} target(s), but " +
                $"{discoveredProjects.Count} production " +
                "project(s) were supplied.");
        }

        return targets;
    }
}