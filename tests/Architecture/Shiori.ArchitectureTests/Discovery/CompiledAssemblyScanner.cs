using Mono.Cecil;

using Shiori.ArchitectureTests.Model;

namespace Shiori.ArchitectureTests.Discovery;

internal static class CompiledAssemblyScanner
{
    public static CompiledAssemblySnapshot Scan(
        CompiledAssemblyTarget target)
    {
        ArgumentNullException.ThrowIfNull(
            target);

        using var assembly =
            AssemblyDefinition.ReadAssembly(
                target.AssemblyPath,
                new ReaderParameters
                {
                    ReadSymbols = false,
                    ReadingMode = ReadingMode.Immediate
                });

        var module =
            assembly.MainModule;

        var actualAssemblyName =
            assembly.Name.Name;

        if (!actualAssemblyName.Equals(
                target.ExpectedAssemblyName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected assembly " +
                $"'{target.ExpectedAssemblyName}', but " +
                $"'{target.AssemblyPath}' contains " +
                $"'{actualAssemblyName}'.");
        }

        var assemblyReferences =
            module.AssemblyReferences
                .Select(reference => reference.Name)
                .ToHashSet(StringComparer.Ordinal);

        var typeReferences =
            module.GetTypeReferences()
                .Select(reference =>
                    new CompiledTypeReference(
                        TypeFullName:
                            reference.FullName,
                        ScopeAssemblyName:
                            GetScopeAssemblyName(reference),
                        Owner:
                            "<assembly metadata>"))
                .GroupBy(reference =>
                    (
                        reference.TypeFullName,
                        reference.ScopeAssemblyName,
                        reference.Owner
                    ))
                .Select(group => group.First())
                .OrderBy(
                    reference =>
                        reference.ScopeAssemblyName,
                    StringComparer.Ordinal)
                .ThenBy(
                    reference =>
                        reference.TypeFullName,
                    StringComparer.Ordinal)
                .ToArray();

        return new CompiledAssemblySnapshot(
            ProjectName:
                target.ProjectName,
            AssemblyName:
                actualAssemblyName,
            AssemblyPath:
                target.AssemblyPath,
            AssemblyReferences:
                assemblyReferences,
            TypeReferences:
                typeReferences);
    }

    public static IReadOnlyList<CompiledTypeReference>
        ScanPublicSurface(
            CompiledAssemblyTarget target)
    {
        using var assembly =
            AssemblyDefinition.ReadAssembly(
                target.AssemblyPath,
                new ReaderParameters
                {
                    ReadSymbols = false,
                    ReadingMode = ReadingMode.Immediate
                });

        var usages =
            new List<CompiledTypeReference>();

        foreach (var type in
                 EnumerateAllTypes(
                     assembly.MainModule)
                     .Where(IsExternallyVisible))
        {
            if (type.BaseType is not null)
            {
                AddTypeUsage(
                    usages,
                    $"{type.FullName} base type",
                    type.BaseType);
            }

            foreach (var interfaceImplementation in
                     type.Interfaces)
            {
                AddTypeUsage(
                    usages,
                    $"{type.FullName} implemented interface",
                    interfaceImplementation.InterfaceType);
            }

            AddGenericParameterConstraints(
                usages,
                $"{type.FullName} generic constraint",
                type.GenericParameters);

            foreach (var field in type.Fields
                         .Where(IsExternallyVisible))
            {
                AddTypeUsage(
                    usages,
                    field.FullName,
                    field.FieldType);
            }

            foreach (var property in type.Properties
                         .Where(IsExternallyVisible))
            {
                AddTypeUsage(
                    usages,
                    property.FullName,
                    property.PropertyType);
            }

            foreach (var eventDefinition in type.Events
                         .Where(IsExternallyVisible))
            {
                AddTypeUsage(
                    usages,
                    eventDefinition.FullName,
                    eventDefinition.EventType);
            }

            foreach (var method in type.Methods
                         .Where(IsExternallyVisible))
            {
                AddTypeUsage(
                    usages,
                    $"{method.FullName} return",
                    method.ReturnType);

                foreach (var parameter in
                         method.Parameters)
                {
                    AddTypeUsage(
                        usages,
                        $"{method.FullName} parameter " +
                        $"'{parameter.Name}'",
                        parameter.ParameterType);
                }

                AddGenericParameterConstraints(
                    usages,
                    $"{method.FullName} generic constraint",
                    method.GenericParameters);
            }
        }

        return usages
            .GroupBy(usage =>
                (
                    usage.Owner,
                    usage.TypeFullName,
                    usage.ScopeAssemblyName
                ))
            .Select(group => group.First())
            .OrderBy(
                usage => usage.Owner,
                StringComparer.Ordinal)
            .ThenBy(
                usage => usage.TypeFullName,
                StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<CompiledMethodTypeUsage>
        ScanMethodTypeUsages(
            CompiledAssemblyTarget target,
            bool skipModuleEntryPoint)
    {
        using var assembly =
            AssemblyDefinition.ReadAssembly(
                target.AssemblyPath,
                new ReaderParameters
                {
                    ReadSymbols = false,
                    ReadingMode = ReadingMode.Immediate
                });

        var module =
            assembly.MainModule;

        var entryPointToken =
            module.EntryPoint?.MetadataToken;

        var usages =
            new List<CompiledMethodTypeUsage>();

        foreach (var type in
                 EnumerateAllTypes(module))
        {
            foreach (var method in
                     type.Methods)
            {
                if (skipModuleEntryPoint &&
                    entryPointToken.HasValue &&
                    method.MetadataToken ==
                    entryPointToken.Value)
                {
                    continue;
                }

                AddMethodTypeUsage(
                    usages,
                    method,
                    method.ReturnType);

                foreach (var parameter in
                         method.Parameters)
                {
                    AddMethodTypeUsage(
                        usages,
                        method,
                        parameter.ParameterType);
                }

                foreach (var genericParameter in
                         method.GenericParameters)
                {
                    foreach (var constraint in
                             genericParameter.Constraints)
                    {
                        AddMethodTypeUsage(
                            usages,
                            method,
                            constraint.ConstraintType);
                    }
                }

                if (!method.HasBody)
                {
                    continue;
                }

                foreach (var variable in
                         method.Body.Variables)
                {
                    AddMethodTypeUsage(
                        usages,
                        method,
                        variable.VariableType);
                }

                foreach (var exceptionHandler in
                         method.Body.ExceptionHandlers)
                {
                    if (exceptionHandler.CatchType
                        is not null)
                    {
                        AddMethodTypeUsage(
                            usages,
                            method,
                            exceptionHandler.CatchType);
                    }
                }

                foreach (var instruction in
                         method.Body.Instructions)
                {
                    switch (instruction.Operand)
                    {
                        case TypeReference typeReference:
                            AddMethodTypeUsage(
                                usages,
                                method,
                                typeReference);
                            break;

                        case MethodReference methodReference:
                            AddMethodReferenceUsage(
                                usages,
                                method,
                                methodReference);
                            break;

                        case FieldReference fieldReference:
                            AddMethodTypeUsage(
                                usages,
                                method,
                                fieldReference.DeclaringType);

                            AddMethodTypeUsage(
                                usages,
                                method,
                                fieldReference.FieldType);
                            break;

                        case CallSite callSite:
                            AddMethodTypeUsage(
                                usages,
                                method,
                                callSite.ReturnType);

                            foreach (var parameter in
                                     callSite.Parameters)
                            {
                                AddMethodTypeUsage(
                                    usages,
                                    method,
                                    parameter.ParameterType);
                            }

                            break;
                    }
                }
            }
        }

        return usages
            .GroupBy(usage =>
                (
                    usage.MethodFullName,
                    usage.TypeFullName,
                    usage.ScopeAssemblyName
                ))
            .Select(group => group.First())
            .OrderBy(
                usage => usage.MethodFullName,
                StringComparer.Ordinal)
            .ThenBy(
                usage => usage.TypeFullName,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddMethodReferenceUsage(
        ICollection<CompiledMethodTypeUsage> usages,
        MethodDefinition ownerMethod,
        MethodReference referencedMethod)
    {
        AddMethodTypeUsage(
            usages,
            ownerMethod,
            referencedMethod.DeclaringType);

        AddMethodTypeUsage(
            usages,
            ownerMethod,
            referencedMethod.ReturnType);

        foreach (var parameter in
                 referencedMethod.Parameters)
        {
            AddMethodTypeUsage(
                usages,
                ownerMethod,
                parameter.ParameterType);
        }

        if (referencedMethod is
            GenericInstanceMethod genericMethod)
        {
            foreach (var genericArgument in
                     genericMethod.GenericArguments)
            {
                AddMethodTypeUsage(
                    usages,
                    ownerMethod,
                    genericArgument);
            }
        }
    }

    private static void AddMethodTypeUsage(
        ICollection<CompiledMethodTypeUsage> usages,
        MethodDefinition ownerMethod,
        TypeReference typeReference)
    {
        foreach (var expandedType in
                 ExpandTypeReference(
                     typeReference))
        {
            usages.Add(
                new CompiledMethodTypeUsage(
                    MethodFullName:
                        ownerMethod.FullName,
                    TypeFullName:
                        expandedType.FullName,
                    ScopeAssemblyName:
                        GetScopeAssemblyName(
                            expandedType)));
        }
    }

    private static void AddTypeUsage(
        ICollection<CompiledTypeReference> usages,
        string owner,
        TypeReference typeReference)
    {
        foreach (var expandedType in
                 ExpandTypeReference(
                     typeReference))
        {
            usages.Add(
                new CompiledTypeReference(
                    TypeFullName:
                        expandedType.FullName,
                    ScopeAssemblyName:
                        GetScopeAssemblyName(
                            expandedType),
                    Owner:
                        owner));
        }
    }

    private static void AddGenericParameterConstraints(
        ICollection<CompiledTypeReference> usages,
        string owner,
        IEnumerable<GenericParameter> genericParameters)
    {
        foreach (var genericParameter in
                 genericParameters)
        {
            foreach (var constraint in
                     genericParameter.Constraints)
            {
                AddTypeUsage(
                    usages,
                    owner,
                    constraint.ConstraintType);
            }
        }
    }

    private static IEnumerable<TypeReference>
        ExpandTypeReference(
            TypeReference typeReference)
    {
        yield return typeReference;

        if (typeReference is
            GenericInstanceType genericInstance)
        {
            foreach (var expanded in
                     ExpandTypeReference(
                         genericInstance.ElementType))
            {
                yield return expanded;
            }

            foreach (var genericArgument in
                     genericInstance.GenericArguments)
            {
                foreach (var expanded in
                         ExpandTypeReference(
                             genericArgument))
                {
                    yield return expanded;
                }
            }

            yield break;
        }

        if (typeReference is
            TypeSpecification typeSpecification)
        {
            foreach (var expanded in
                     ExpandTypeReference(
                         typeSpecification.ElementType))
            {
                yield return expanded;
            }
        }
    }

    private static string GetScopeAssemblyName(
        TypeReference typeReference)
    {
        return typeReference.Scope switch
        {
            AssemblyNameReference assemblyReference =>
                assemblyReference.Name,

            ModuleDefinition moduleDefinition
                when moduleDefinition.Assembly
                     is not null =>
                moduleDefinition.Assembly.Name.Name,

            ModuleReference moduleReference =>
                moduleReference.Name,

            _ =>
                typeReference.Scope?.Name ??
                string.Empty
        };
    }

    private static IEnumerable<TypeDefinition>
        EnumerateAllTypes(
            ModuleDefinition module)
    {
        foreach (var type in module.Types)
        {
            foreach (var expandedType in
                     EnumerateTypeAndNestedTypes(type))
            {
                yield return expandedType;
            }
        }
    }

    private static IEnumerable<TypeDefinition>
        EnumerateTypeAndNestedTypes(
            TypeDefinition type)
    {
        yield return type;

        foreach (var nestedType in
                 type.NestedTypes)
        {
            foreach (var expandedType in
                     EnumerateTypeAndNestedTypes(
                         nestedType))
            {
                yield return expandedType;
            }
        }
    }

    private static bool IsExternallyVisible(
        TypeDefinition type)
    {
        if (!type.IsNested)
        {
            return type.IsPublic;
        }

        var nestedVisibility =
            type.IsNestedPublic ||
            type.IsNestedFamily ||
            type.IsNestedFamilyOrAssembly;

        return nestedVisibility &&
               type.DeclaringType is not null &&
               IsExternallyVisible(
                   type.DeclaringType);
    }

    private static bool IsExternallyVisible(
        MethodDefinition method)
    {
        return method.IsPublic ||
               method.IsFamily ||
               method.IsFamilyOrAssembly;
    }

    private static bool IsExternallyVisible(
        FieldDefinition field)
    {
        return field.IsPublic ||
               field.IsFamily ||
               field.IsFamilyOrAssembly;
    }

    private static bool IsExternallyVisible(
        PropertyDefinition property)
    {
        return property.GetMethod is not null &&
               IsExternallyVisible(
                   property.GetMethod) ||
               property.SetMethod is not null &&
               IsExternallyVisible(
                   property.SetMethod);
    }

    private static bool IsExternallyVisible(
        EventDefinition eventDefinition)
    {
        return eventDefinition.AddMethod is not null &&
               IsExternallyVisible(
                   eventDefinition.AddMethod) ||
               eventDefinition.RemoveMethod is not null &&
               IsExternallyVisible(
                   eventDefinition.RemoveMethod);
    }
}