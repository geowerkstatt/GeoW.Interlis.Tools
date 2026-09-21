using Geowerkstatt.Interlis.Compiler.AST;

namespace Geowerkstatt.Interlis.Compiler;

public class InterlisEnvironmentTest
{
    private static ModelDef Model(string name) => new() { Name = name };

    [Test]
    public async Task MergeFromAddsModelsOfTheSameVersionOnce()
    {
        var environment = new InterlisEnvironment { Version = 2.4, Content = { { "A", Model("A") } } };
        var other = new InterlisEnvironment { Version = 2.4, Content = { { "A", Model("A") }, { "B", Model("B") } } };

        environment.MergeFrom(other);

        await Assert.That(environment.Content.Keys).IsEquivalentTo(["A", "B"]);
    }

    [Test]
    public async Task MergeFromRejectsAnotherVersion()
    {
        var environment = new InterlisEnvironment { Version = 2.4 };
        var other = new InterlisEnvironment { Version = 2.3, Content = { { "B", Model("B") } } };

        await Assert.That(() => environment.MergeFrom(other)).Throws<InvalidOperationException>();
        await Assert.That(environment.Content.Count).IsEqualTo(0);
    }
}
