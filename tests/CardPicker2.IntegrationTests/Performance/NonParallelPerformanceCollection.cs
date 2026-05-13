namespace CardPicker2.IntegrationTests.Performance;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NonParallelPerformanceCollection
{
    public const string Name = "NonParallelPerformance";
}
