using System.Reflection;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using AiTodo.Infrastructure;

namespace AiTodo.Api.IntegrationTests.Architecture;

/// <summary>
/// The dependency rules of the solution (CLAUDE.md): Api -> Application -> Domain;
/// Infrastructure -> Application/Domain; Domain knows nobody; Application knows neither
/// Infrastructure nor MongoDB.
/// </summary>
public class DependencyRulesTests
{
    private static readonly Assembly Domain = typeof(Todo).Assembly;
    private static readonly Assembly Application = typeof(TodoResponse).Assembly;
    private static readonly Assembly Infrastructure = typeof(DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static string[] References(Assembly assembly)
        => assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();

    private static bool IsProject(string name) => name.StartsWith("AiTodo.", StringComparison.Ordinal);

    private static bool IsFrameworkOrBcl(string name)
        => name is "System.Runtime" or "System.Collections" or "System.Linq" or "System.Memory"
            or "System.Threading" or "System.Threading.Tasks" or "System.Runtime.InteropServices"
            or "System.Diagnostics.Debug" or "System.Linq.Expressions" or "System.Collections.Concurrent"
            or "System.Text.RegularExpressions" or "System.Console" or "netstandard" or "mscorlib"
            or "System.Private.CoreLib" || name.StartsWith("System.", StringComparison.Ordinal);

    [Fact]
    public void Control_the_inspection_does_see_MongoDB_in_Infrastructure_and_FastEndpoints_in_Application()
    {
        // Positive controls: if these fail, the "does not know X" rules below could pass vacuously.
        Assert.Contains(References(Infrastructure), name => name.StartsWith("MongoDB", StringComparison.Ordinal));
        Assert.Contains(References(Application), name => name.StartsWith("FastEndpoints", StringComparison.Ordinal));
        Assert.Contains(References(Api), name => name.StartsWith("FastEndpoints", StringComparison.Ordinal));
        Assert.Contains("AiTodo.Domain", References(Application));
    }

    [Fact]
    public void Domain_references_no_other_project_and_no_package()
    {
        var offending = References(Domain).Where(name => !IsFrameworkOrBcl(name)).ToArray();

        Assert.Empty(offending);
    }

    [Fact]
    public void Domain_does_not_know_serialization_or_database_technology()
    {
        var references = References(Domain);

        Assert.DoesNotContain(references, name => name.StartsWith("MongoDB", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("FastEndpoints", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.Extensions", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_depends_only_on_Domain_among_the_projects()
    {
        var projects = References(Application).Where(IsProject).ToArray();

        Assert.Equal(["AiTodo.Domain"], projects);
    }

    [Fact]
    public void Application_knows_neither_Infrastructure_nor_MongoDB_nor_the_web_stack()
    {
        var references = References(Application);

        Assert.DoesNotContain(references, name => name.StartsWith("MongoDB", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain("AiTodo.Infrastructure", references);
        Assert.DoesNotContain("AiTodo.Api", references);
    }

    [Fact]
    public void Application_uses_only_the_command_bus_abstractions_of_FastEndpoints()
    {
        var fastEndpoints = References(Application).Where(n => n.StartsWith("FastEndpoints", StringComparison.Ordinal)).ToArray();

        Assert.All(fastEndpoints, name => Assert.Equal("FastEndpoints.Messaging.Core", name));
    }

    [Fact]
    public void Infrastructure_depends_on_Application_and_Domain_but_never_on_Api()
    {
        var projects = References(Infrastructure).Where(IsProject).ToArray();

        Assert.Contains("AiTodo.Application", projects);
        Assert.Contains("AiTodo.Domain", projects);
        Assert.DoesNotContain("AiTodo.Api", projects);
    }

    [Fact]
    public void Api_does_not_know_MongoDB()
    {
        // Infrastructure is referenced only for composition (AddInfrastructure) in Program.cs.
        Assert.DoesNotContain(References(Api), name => name.StartsWith("MongoDB", StringComparison.Ordinal));
    }

    [Fact]
    public void Api_references_Application_and_uses_Infrastructure_only_for_composition()
    {
        var projects = References(Api).Where(IsProject).ToArray();

        Assert.Contains("AiTodo.Application", projects);
        Assert.Contains("AiTodo.Infrastructure", projects);
    }

    [Fact]
    public void The_aggregate_is_encapsulated_private_constructor_and_private_setters()
    {
        var type = typeof(Todo);

        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.All(
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => Assert.True(property.SetMethod is null || !property.SetMethod.IsPublic,
                $"{property.Name} must not have a public setter"));
    }

    [Fact]
    public void Persistence_types_are_not_exposed_publicly_by_Infrastructure()
    {
        var publicTypes = Infrastructure.GetExportedTypes().Select(t => t.Name).ToArray();

        Assert.DoesNotContain("TodoRepository", publicTypes);
        Assert.DoesNotContain("TodoReadRepository", publicTypes);
        Assert.DoesNotContain("TodoReadModel", publicTypes);
    }
}
