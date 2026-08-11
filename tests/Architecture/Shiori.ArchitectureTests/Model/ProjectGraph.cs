namespace Shiori.ArchitectureTests.Model;

internal sealed class ProjectGraph
{
    private readonly IReadOnlyDictionary<string, ProjectNode> _nodes;

    public ProjectGraph(IEnumerable<ProjectNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var nodeArray = nodes.ToArray();

        if (nodeArray.Length == 0)
        {
            throw new InvalidOperationException(
                "Cannot create a production project graph with zero nodes.");
        }

        var duplicateNames = nodeArray
            .GroupBy(
                node => node.Name,
                StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            throw new InvalidOperationException(
                "Duplicate project names were found in the production graph:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    duplicateNames.Select(name => $" - {name}")));
        }

        _nodes = nodeArray.ToDictionary(
            node => node.Name,
            StringComparer.Ordinal);

        ValidateReferencesTargetKnownNodes();
    }

    public ProjectNode GetProject(string projectName)
    {
        if (!_nodes.TryGetValue(projectName, out var project))
        {
            throw new InvalidOperationException(
                $"Expected project '{projectName}' was not found in the production graph.");
        }

        return project;
    }

    public IReadOnlyList<string> FindCycle()
    {
        var states = _nodes.Keys.ToDictionary(
            projectName => projectName,
            _ => VisitState.NotVisited,
            StringComparer.Ordinal);

        var currentPath = new List<string>();

        foreach (var projectName in _nodes.Keys
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            if (states[projectName] != VisitState.NotVisited)
            {
                continue;
            }

            var cycle = Visit(
                projectName,
                states,
                currentPath);

            if (cycle.Count > 0)
            {
                return cycle;
            }
        }

        return [];
    }

    private IReadOnlyList<string> Visit(
        string projectName,
        IDictionary<string, VisitState> states,
        List<string> currentPath)
    {
        states[projectName] = VisitState.Visiting;
        currentPath.Add(projectName);

        var project = _nodes[projectName];

        foreach (var dependency in project.ProjectReferences
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            if (states[dependency] == VisitState.Visiting)
            {
                var cycleStartIndex =
                    currentPath.IndexOf(dependency);

                return currentPath
                    .Skip(cycleStartIndex)
                    .Append(dependency)
                    .ToArray();
            }

            if (states[dependency] == VisitState.NotVisited)
            {
                var cycle = Visit(
                    dependency,
                    states,
                    currentPath);

                if (cycle.Count > 0)
                {
                    return cycle;
                }
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        states[projectName] = VisitState.Visited;

        return [];
    }

    private void ValidateReferencesTargetKnownNodes()
    {
        var invalidReferences = _nodes.Values
            .SelectMany(project =>
                project.ProjectReferences
                    .Where(reference => !_nodes.ContainsKey(reference))
                    .Select(reference =>
                        $"{project.Name} -> {reference}"))
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        if (invalidReferences.Length > 0)
        {
            throw new InvalidOperationException(
                "The production graph contains references to unknown projects:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    invalidReferences.Select(reference => $" - {reference}")));
        }
    }

    private enum VisitState
    {
        NotVisited,
        Visiting,
        Visited
    }
}