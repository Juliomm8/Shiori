namespace Shiori.ArchitectureTests.Baseline;

internal sealed record ProductionProject(
    string Name,
    string RelativePath,
    IReadOnlyList<string> ExpectedProjectReferences);

internal static class ProductionProjectRegistry
{
    public const int ExpectedProjectCount = 13;

    public static IReadOnlyList<ProductionProject> Projects { get; } =
    [
        new(
            "Shiori.Gateway",
            "src/Gateway/Shiori.Gateway/Shiori.Gateway.csproj",
            []),

        new(
            "Shiori.Identity.Api",
            "src/Services/Identity/Shiori.Identity.Api/Shiori.Identity.Api.csproj",
            [
                "Shiori.Identity.Application",
                "Shiori.Identity.Infrastructure"
            ]),
        new(
            "Shiori.Identity.Application",
            "src/Services/Identity/Shiori.Identity.Application/Shiori.Identity.Application.csproj",
            [
                "Shiori.Identity.Domain"
            ]),
        new(
            "Shiori.Identity.Domain",
            "src/Services/Identity/Shiori.Identity.Domain/Shiori.Identity.Domain.csproj",
            []),
        new(
            "Shiori.Identity.Infrastructure",
            "src/Services/Identity/Shiori.Identity.Infrastructure/Shiori.Identity.Infrastructure.csproj",
            [
                "Shiori.Identity.Application",
                "Shiori.Identity.Domain"
            ]),

        new(
            "Shiori.Catalog.Api",
            "src/Services/Catalog/Shiori.Catalog.Api/Shiori.Catalog.Api.csproj",
            [
                "Shiori.Catalog.Application",
                "Shiori.Catalog.Infrastructure"
            ]),
        new(
            "Shiori.Catalog.Application",
            "src/Services/Catalog/Shiori.Catalog.Application/Shiori.Catalog.Application.csproj",
            [
                "Shiori.Catalog.Domain"
            ]),
        new(
            "Shiori.Catalog.Domain",
            "src/Services/Catalog/Shiori.Catalog.Domain/Shiori.Catalog.Domain.csproj",
            []),
        new(
            "Shiori.Catalog.Infrastructure",
            "src/Services/Catalog/Shiori.Catalog.Infrastructure/Shiori.Catalog.Infrastructure.csproj",
            [
                "Shiori.Catalog.Application",
                "Shiori.Catalog.Domain"
            ]),

        new(
            "Shiori.Tracking.Api",
            "src/Services/Tracking/Shiori.Tracking.Api/Shiori.Tracking.Api.csproj",
            [
                "Shiori.Tracking.Application",
                "Shiori.Tracking.Infrastructure"
            ]),
        new(
            "Shiori.Tracking.Application",
            "src/Services/Tracking/Shiori.Tracking.Application/Shiori.Tracking.Application.csproj",
            [
                "Shiori.Tracking.Domain"
            ]),
        new(
            "Shiori.Tracking.Domain",
            "src/Services/Tracking/Shiori.Tracking.Domain/Shiori.Tracking.Domain.csproj",
            []),
        new(
            "Shiori.Tracking.Infrastructure",
            "src/Services/Tracking/Shiori.Tracking.Infrastructure/Shiori.Tracking.Infrastructure.csproj",
            [
                "Shiori.Tracking.Application",
                "Shiori.Tracking.Domain"
            ])
    ];
}