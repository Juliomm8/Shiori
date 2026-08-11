namespace Shiori.ArchitectureTests.Model;

internal sealed record ProjectTechnologySnapshot(
    string ProjectName,
    IReadOnlySet<string> PackageReferences,
    IReadOnlySet<string> FrameworkReferences);