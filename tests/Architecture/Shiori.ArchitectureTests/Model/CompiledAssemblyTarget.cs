namespace Shiori.ArchitectureTests.Model;

internal sealed record CompiledAssemblyTarget(
    string ProjectName,
    string ExpectedAssemblyName,
    string ProjectPath,
    string AssemblyPath);