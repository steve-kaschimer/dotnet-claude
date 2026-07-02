using DotnetClaude.Core.Models;
using DotnetClaude.Core.Services;
using Xunit;

namespace DotnetClaude.Core.Tests;

public class CostCalculatorTests
{
    private static readonly ChatModelInfo TestModel = ModelCatalog.GetByWireId("claude-opus-4-8");

    [Fact]
    public void EstimateCostUsd_InputAndOutputOnly_MatchesManualCalculation()
    {
        var cost = CostCalculator.EstimateCostUsd(TestModel, inputTokens: 1_000_000, outputTokens: 1_000_000, cacheCreationInputTokens: 0, cacheReadInputTokens: 0);

        var expected = TestModel.InputPricePerMillionTokens + TestModel.OutputPricePerMillionTokens;
        Assert.Equal(expected, cost);
    }

    [Fact]
    public void EstimateCostUsd_ZeroTokens_IsZero()
    {
        var cost = CostCalculator.EstimateCostUsd(TestModel, 0, 0, 0, 0);
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void EstimateCostUsd_CacheReadIsCheaperThanFullPriceInput()
    {
        var fullPriceCost = CostCalculator.EstimateCostUsd(TestModel, 1_000_000, 0, 0, 0);
        var cacheReadCost = CostCalculator.EstimateCostUsd(TestModel, 0, 0, 0, 1_000_000);

        Assert.True(cacheReadCost < fullPriceCost);
    }

    [Fact]
    public void EstimateCostUsd_CacheWriteIsMoreExpensiveThanFullPriceInput()
    {
        var fullPriceCost = CostCalculator.EstimateCostUsd(TestModel, 1_000_000, 0, 0, 0);
        var cacheWriteCost = CostCalculator.EstimateCostUsd(TestModel, 0, 0, 1_000_000, 0);

        Assert.True(cacheWriteCost > fullPriceCost);
    }
}
