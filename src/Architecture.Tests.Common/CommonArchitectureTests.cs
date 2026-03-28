using System.Reflection;
using System.Xml.Linq;
using NetArchTest.Rules;
using Xunit;

namespace Architecture.Tests.Common;

public sealed class CommonArchitectureTests
{
    private static readonly string[] ContextNames =
    {
        "Catalog",
        "Cart",
        "Order",
        "Payment",
        "Shipping",
        "Warehouse",
        "Communication"
    };

    private static readonly string ModuleName = ResolveModuleName();
    private static readonly string RepositoryRoot = ResolveRepositoryRoot();

    [Fact]
    public void Module_WhenValidated_HasAllLayerProjects()
    {
        Assert.Contains(ModuleName, ContextNames);

        Assert.True(File.Exists(GetModuleProjectPath("Api")), $"Missing project: {ModuleName}.Api");
        Assert.True(File.Exists(GetModuleProjectPath("Application")), $"Missing project: {ModuleName}.Application");
        Assert.True(File.Exists(GetModuleProjectPath("Domain")), $"Missing project: {ModuleName}.Domain");
        Assert.True(File.Exists(GetModuleProjectPath("Infrastructure")), $"Missing project: {ModuleName}.Infrastructure");
    }

    [Fact]
    public void LayerProjectReferences_WhenValidated_FollowCommonRules()
    {
        var apiReferences = GetProjectReferences(GetModuleProjectPath("Api"));
        var applicationReferences = GetProjectReferences(GetModuleProjectPath("Application"));
        var domainReferences = GetProjectReferences(GetModuleProjectPath("Domain"));
        var infrastructureReferences = GetProjectReferences(GetModuleProjectPath("Infrastructure"));

        AssertContains(apiReferences, $"{ModuleName}.Application");
        AssertContains(apiReferences, $"{ModuleName}.Infrastructure");
        AssertAllowedOnly(apiReferences, $"{ModuleName}.Application", $"{ModuleName}.Infrastructure", "Shared.BuildingBlocks");

        AssertContains(applicationReferences, $"{ModuleName}.Domain");
        AssertAllowedOnly(applicationReferences, $"{ModuleName}.Domain", "Shared.BuildingBlocks");

        AssertAllowedOnly(domainReferences, "Shared.BuildingBlocks");

        AssertContains(infrastructureReferences, $"{ModuleName}.Application");
        AssertContains(infrastructureReferences, $"{ModuleName}.Domain");
        AssertAllowedOnly(infrastructureReferences, $"{ModuleName}.Application", $"{ModuleName}.Domain", "Shared.BuildingBlocks");

        AssertNoCrossContextReferences(apiReferences);
        AssertNoCrossContextReferences(applicationReferences);
        AssertNoCrossContextReferences(domainReferences);
        AssertNoCrossContextReferences(infrastructureReferences);
    }

    [Fact]
    public void LayerAssemblies_WhenValidated_RespectDependencyDirection()
    {
        var apiAssembly = LoadAssembly("Api");
        var applicationAssembly = LoadAssembly("Application");
        var domainAssembly = LoadAssembly("Domain");
        var infrastructureAssembly = LoadAssembly("Infrastructure");

        AssertNoDependency(domainAssembly, $"{ModuleName}.Api");
        AssertNoDependency(domainAssembly, $"{ModuleName}.Application");
        AssertNoDependency(domainAssembly, $"{ModuleName}.Infrastructure");
        AssertNoDependency(domainAssembly, "Microsoft.EntityFrameworkCore");
        AssertNoDependency(domainAssembly, "Microsoft.AspNetCore");
        AssertNoDependency(domainAssembly, "Wolverine");
        AssertNoDependency(domainAssembly, "Npgsql");

        AssertNoDependency(applicationAssembly, $"{ModuleName}.Api");
        AssertNoDependency(applicationAssembly, $"{ModuleName}.Infrastructure");

        AssertNoDependency(infrastructureAssembly, $"{ModuleName}.Api");

        AssertNoDependency(apiAssembly, $"{ModuleName}.Domain");

        AssertNoCrossContextAssemblyReference(apiAssembly);
        AssertNoCrossContextAssemblyReference(applicationAssembly);
        AssertNoCrossContextAssemblyReference(domainAssembly);
        AssertNoCrossContextAssemblyReference(infrastructureAssembly);
    }

    [Fact]
    public void RepositoryConventions_WhenValidated_AreConsistent()
    {
        var applicationAssembly = LoadAssembly("Application");
        var infrastructureAssembly = LoadAssembly("Infrastructure");

        var repositoryInterfaces = applicationAssembly
            .GetTypes()
            .Where(type =>
                type.IsInterface
                && type.Namespace is not null
                && type.Namespace.StartsWith($"{ModuleName}.Application.Abstractions.Repositories", StringComparison.Ordinal))
            .ToArray();

        Assert.All(repositoryInterfaces, repositoryInterface =>
        {
            Assert.StartsWith("I", repositoryInterface.Name, StringComparison.Ordinal);
            Assert.EndsWith("Repository", repositoryInterface.Name, StringComparison.Ordinal);
        });

        var repositoryImplementations = infrastructureAssembly
            .GetTypes()
            .Where(type =>
                type.IsClass
                && !type.IsAbstract
                && !type.IsNested
                && type.Namespace is not null
                && type.Namespace.StartsWith($"{ModuleName}.Infrastructure.Persistence.Repositories", StringComparison.Ordinal))
            .ToArray();

        Assert.All(repositoryImplementations, repositoryImplementation =>
        {
            Assert.EndsWith("Repository", repositoryImplementation.Name, StringComparison.Ordinal);

            var hasApplicationRepositoryInterface = repositoryImplementation
                .GetInterfaces()
                .Any(@interface =>
                    @interface.Namespace is not null
                    && @interface.Namespace.StartsWith($"{ModuleName}.Application.Abstractions.Repositories", StringComparison.Ordinal));

            Assert.True(
                hasApplicationRepositoryInterface,
                $"Repository implementation '{repositoryImplementation.FullName}' should implement an application repository interface.");
        });
    }

    [Fact]
    public void ApiEndpoints_WhenInspected_DoNotUseInfrastructureOrDirectEventPublishing()
    {
        var endpointsDirectoryPath = Path.Combine(RepositoryRoot, "src", $"{ModuleName}.Api", "Endpoints");
        if (!Directory.Exists(endpointsDirectoryPath))
        {
            return;
        }

        var endpointFiles = Directory.GetFiles(endpointsDirectoryPath, "*.cs", SearchOption.AllDirectories);
        foreach (var endpointFile in endpointFiles)
        {
            var source = File.ReadAllText(endpointFile);

            Assert.DoesNotContain("IDomainEventPublisher", source);
            Assert.DoesNotContain("Shared.BuildingBlocks.Contracts.IntegrationEvents", source);
            Assert.DoesNotMatch(@"using\s+[A-Za-z0-9_.]+\.Infrastructure", source);
            Assert.DoesNotContain("EntityFrameworkCore", source);
            Assert.DoesNotContain("Wolverine", source);
            Assert.DoesNotContain("Npgsql", source);
        }
    }

    [Fact]
    public void ProgramBootstrap_WhenInspected_DoesNotContainLowLevelTechnicalWiring()
    {
        var programPath = Path.Combine(RepositoryRoot, "src", $"{ModuleName}.Api", "Program.cs");
        Assert.True(File.Exists(programPath), $"Missing Program.cs for module '{ModuleName}'.");

        var source = File.ReadAllText(programPath);
        Assert.DoesNotContain("AddDbContext", source);
        Assert.DoesNotContain("UseNpgsql", source);
        Assert.DoesNotContain("UseWolverine", source);
        Assert.DoesNotContain("ListenToRabbitQueue", source);
        Assert.DoesNotContain("PublishMessage<", source);
        Assert.DoesNotContain("PersistMessagesWithPostgresql", source);
        Assert.DoesNotContain("GetConnectionString(", source);
        Assert.DoesNotContain("ConnectionStrings__", source);
    }

    private static void AssertNoDependency(Assembly assembly, string dependency)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOn(dependency).GetResult();
        Assert.True(
            result.IsSuccessful,
            $"Assembly '{assembly.GetName().Name}' should not depend on '{dependency}'.");
    }

    private static void AssertNoCrossContextAssemblyReference(Assembly assembly)
    {
        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();

        var forbiddenReferences = references
            .Where(reference => ContextNames.Any(context =>
                !string.Equals(context, ModuleName, StringComparison.Ordinal)
                && reference.StartsWith($"{context}.", StringComparison.Ordinal)))
            .ToArray();

        Assert.True(
            forbiddenReferences.Length == 0,
            $"Assembly '{assembly.GetName().Name}' has forbidden cross-context references: {string.Join(", ", forbiddenReferences)}");
    }

    private static void AssertNoCrossContextReferences(IReadOnlyCollection<string> references)
    {
        var forbiddenReferences = references
            .Where(reference => ContextNames.Any(context =>
                !string.Equals(context, ModuleName, StringComparison.Ordinal)
                && reference.StartsWith($"{context}.", StringComparison.Ordinal)))
            .ToArray();

        Assert.True(
            forbiddenReferences.Length == 0,
            $"Project references contain forbidden cross-context modules: {string.Join(", ", forbiddenReferences)}");
    }

    private static void AssertAllowedOnly(IEnumerable<string> references, params string[] allowed)
    {
        var notAllowed = references
            .Where(reference => !allowed.Contains(reference, StringComparer.Ordinal))
            .ToArray();

        Assert.True(
            notAllowed.Length == 0,
            $"Found disallowed project references: {string.Join(", ", notAllowed)}");
    }

    private static void AssertContains(IEnumerable<string> references, string expected)
    {
        Assert.Contains(expected, references);
    }

    private static IReadOnlyCollection<string> GetProjectReferences(string csprojPath)
    {
        var csprojDocument = XDocument.Load(csprojPath);
        return csprojDocument
            .Descendants()
            .Where(node => string.Equals(node.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFileNameWithoutExtension(value!))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static Assembly LoadAssembly(string layerName)
    {
        var assemblyName = $"{ModuleName}.{layerName}";

        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (FileNotFoundException)
        {
            var assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
            if (!File.Exists(assemblyPath))
            {
                throw;
            }

            return Assembly.LoadFrom(assemblyPath);
        }
    }

    private static string GetModuleProjectPath(string layerName)
    {
        return Path.Combine(RepositoryRoot, "src", $"{ModuleName}.{layerName}", $"{ModuleName}.{layerName}.csproj");
    }

    private static string ResolveModuleName()
    {
        var testAssemblyName = typeof(CommonArchitectureTests).Assembly.GetName().Name
            ?? throw new InvalidOperationException("Unable to resolve test assembly name.");

        if (!testAssemblyName.EndsWith(".Tests", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected test assembly name '{testAssemblyName}'.");
        }

        return testAssemblyName[..^".Tests".Length];
    }

    private static string ResolveRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            var docsDirectory = Path.Combine(currentDirectory.FullName, "docs");
            var srcDirectory = Path.Combine(currentDirectory.FullName, "src");

            if (Directory.Exists(docsDirectory) && Directory.Exists(srcDirectory))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root from test runtime directory.");
    }
}

