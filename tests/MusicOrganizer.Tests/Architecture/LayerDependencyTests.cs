using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace MusicOrganizer.Tests.Architecture;

public class LayerDependencyTests
{
    private const string DomainNamespace = "MusicOrganizer.Domain";
    private const string SharedNamespace = "MusicOrganizer.Shared";
    private const string ApplicationNamespace = "MusicOrganizer.Application";
    private const string InfrastructureNamespace = "MusicOrganizer.Infrastructure";
    private const string CliNamespace = "MusicOrganizer.Cli";

    private static readonly Assembly DomainAssembly = Assembly.Load("MusicOrganizer.Domain");
    private static readonly Assembly SharedAssembly = Assembly.Load("MusicOrganizer.Shared");
    private static readonly Assembly ApplicationAssembly = Assembly.Load("MusicOrganizer.Application");
    private static readonly Assembly InfrastructureAssembly = Assembly.Load("MusicOrganizer.Infrastructure");

    [Fact]
    public void Domain_ShouldNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(SharedNamespace, ApplicationNamespace, InfrastructureNamespace, CliNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Shared_ShouldNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(SharedAssembly)
            .Should()
            .NotHaveDependencyOnAny(DomainNamespace, ApplicationNamespace, InfrastructureNamespace, CliNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrCli()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, CliNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnCli()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(CliNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void SanityCheck_Application_HasTypesDependingOnDomain()
    {
        // Proves NetArchTest actually detects real dependencies rather than passing vacuously:
        // if this ever fails, the rules above are silently no-ops, not enforcing anything.
        var typesDependingOnDomain = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveDependencyOn(DomainNamespace)
            .GetTypes();

        typesDependingOnDomain.Should().NotBeEmpty();
    }

    private static string FailureMessage(TestResult result) =>
        $"Layer boundary violated by: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
