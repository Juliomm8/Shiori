namespace Shiori.ArchitectureTests.Baseline;

internal enum PackageMatchMode
{
    Family,
    Prefix
}

internal sealed record ForbiddenPackageFamily(
    string PackageId,
    string Category,
    PackageMatchMode MatchMode = PackageMatchMode.Family)
{
    public bool Matches(string candidatePackageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePackageId);

        return MatchMode switch
        {
            PackageMatchMode.Family =>
                candidatePackageId.Equals(
                    PackageId,
                    StringComparison.OrdinalIgnoreCase) ||
                candidatePackageId.StartsWith(
                    PackageId + ".",
                    StringComparison.OrdinalIgnoreCase),

            PackageMatchMode.Prefix =>
                candidatePackageId.StartsWith(
                    PackageId,
                    StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }
}

internal static class PackageBoundaryPolicy
{
    public const string AspNetCoreFrameworkReference =
        "Microsoft.AspNetCore.App";

    public static IReadOnlyList<ForbiddenPackageFamily>
        DomainAndApplicationForbiddenPackages
    { get; } =
    [
        // Persistence
        new(
            "Microsoft.EntityFrameworkCore",
            "Persistence / Entity Framework Core"),
        new(
            "Npgsql",
            "Persistence / PostgreSQL"),
        new(
            "MongoDB",
            "Persistence / MongoDB"),

        // Messaging
        new(
            "RabbitMQ",
            "Broker / RabbitMQ"),

        // Gateway technology
        new(
            "Yarp",
            "Gateway / YARP"),

        // Identity infrastructure
        new(
            "OpenIddict",
            "Identity infrastructure / OpenIddict"),

        // HTTP transport
        new(
            "Microsoft.AspNetCore",
            "HTTP transport / ASP.NET Core"),
        new(
            "Microsoft.Extensions.Http",
            "HTTP client infrastructure"),

        // HTTP / provider adapter libraries
        new(
            "Refit",
            "HTTP adapter infrastructure"),
        new(
            "RestSharp",
            "HTTP adapter infrastructure"),
        new(
            "Flurl",
            "HTTP adapter infrastructure"),
        new(
            "GraphQL.Client",
            "HTTP / GraphQL adapter infrastructure"),
        new(
            "StrawberryShake",
            "HTTP / GraphQL adapter infrastructure"),

        // Provider-specific packages.
        new(
            "AniList",
            "External provider adapter",
            PackageMatchMode.Prefix),
        new(
            "MangaDex",
            "External provider adapter",
            PackageMatchMode.Prefix)
    ];

    public static IReadOnlyList<ForbiddenPackageFamily>
        GatewayForbiddenPackages
    { get; } =
    [
        // Gateway owns no persistence.
        new(
            "Microsoft.EntityFrameworkCore",
            "Persistence / Entity Framework Core"),
        new(
            "Npgsql",
            "Persistence / PostgreSQL"),
        new(
            "MongoDB",
            "Persistence / MongoDB"),

        // Gateway must not become a broker orchestrator.
        new(
            "RabbitMQ",
            "Broker / RabbitMQ"),

        // Identity implementation belongs to Identity.
        new(
            "OpenIddict",
            "Identity infrastructure / OpenIddict"),

        // External provider integrations belong to Catalog.
        new(
            "Refit",
            "Provider / HTTP adapter"),
        new(
            "RestSharp",
            "Provider / HTTP adapter"),
        new(
            "Flurl",
            "Provider / HTTP adapter"),
        new(
            "GraphQL.Client",
            "Provider / GraphQL adapter"),
        new(
            "StrawberryShake",
            "Provider / GraphQL adapter"),
        new(
            "AniList",
            "External provider adapter",
            PackageMatchMode.Prefix),
        new(
            "MangaDex",
            "External provider adapter",
            PackageMatchMode.Prefix)
    ];

    public static bool IsDomainOrApplicationProject(
        string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        return projectName.EndsWith(
                   ".Domain",
                   StringComparison.Ordinal) ||
               projectName.EndsWith(
                   ".Application",
                   StringComparison.Ordinal);
    }

    public static ForbiddenPackageFamily?
        FindDomainOrApplicationViolation(
            string packageId)
    {
        return DomainAndApplicationForbiddenPackages
            .FirstOrDefault(rule => rule.Matches(packageId));
    }

    public static ForbiddenPackageFamily?
        FindGatewayViolation(
            string packageId)
    {
        return GatewayForbiddenPackages
            .FirstOrDefault(rule => rule.Matches(packageId));
    }

    public static bool TryMatchProductionImplementationPackage(
        string packageId,
        out string? productionProjectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        foreach (var productionProject in
                 ProductionProjectRegistry.Projects)
        {
            if (packageId.Equals(
                    productionProject.Name,
                    StringComparison.OrdinalIgnoreCase) ||
                packageId.StartsWith(
                    productionProject.Name + ".",
                    StringComparison.OrdinalIgnoreCase))
            {
                productionProjectName =
                    productionProject.Name;

                return true;
            }
        }

        productionProjectName = null;

        return false;
    }
}