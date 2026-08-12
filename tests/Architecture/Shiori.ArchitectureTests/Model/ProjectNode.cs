namespace Shiori.ArchitectureTests.Model;

internal sealed record ProjectNode(
    string Name,
    string FullPath,
    IReadOnlySet<string> ProjectReferences);