namespace CardPicker2.IntegrationTests.Browser;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NonParallelBrowserCollection
{
    public const string Name = "NonParallelBrowser";
}
