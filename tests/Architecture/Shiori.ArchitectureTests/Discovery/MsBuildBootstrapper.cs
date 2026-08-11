using Microsoft.Build.Locator;

namespace Shiori.ArchitectureTests.Discovery;

internal static class MsBuildBootstrapper
{
    private static readonly object SyncRoot = new();

    private static bool _initialized;

    public static void EnsureRegistered()
    {
        if (_initialized)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            if (!MSBuildLocator.IsRegistered)
            {
                if (!MSBuildLocator.CanRegister)
                {
                    throw new InvalidOperationException(
                        "MSBuild cannot be registered because Microsoft.Build " +
                        "assemblies were loaded before MSBuildLocator.");
                }

                MSBuildLocator.RegisterDefaults();
            }

            _initialized = true;
        }
    }
}