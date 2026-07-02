using DotnetClaude.Core.Models;
using Xunit;

namespace DotnetClaude.Core.Tests;

public class ModelCatalogTests
{
    [Fact]
    public void Default_IsFirstModelInList()
    {
        Assert.Equal(ModelCatalog.Models[0], ModelCatalog.Default);
    }

    [Theory]
    [InlineData("claude-opus-4-8")]
    [InlineData("claude-sonnet-5")]
    [InlineData("claude-haiku-4-5")]
    [InlineData("claude-fable-5")]
    public void GetByWireId_ReturnsMatchingModel(string wireId)
    {
        var model = ModelCatalog.GetByWireId(wireId);
        Assert.Equal(wireId, model.WireId);
    }

    [Fact]
    public void GetByWireId_UnknownId_FallsBackToDefault()
    {
        var model = ModelCatalog.GetByWireId("not-a-real-model");
        Assert.Equal(ModelCatalog.Default, model);
    }

    [Fact]
    public void AllModels_HavePositivePricingAndContextWindow()
    {
        foreach (var model in ModelCatalog.Models)
        {
            Assert.True(model.InputPricePerMillionTokens > 0, $"{model.WireId} input price");
            Assert.True(model.OutputPricePerMillionTokens > 0, $"{model.WireId} output price");
            Assert.True(model.ContextWindowTokens > 0, $"{model.WireId} context window");
        }
    }

    [Fact]
    public void CacheWritePrice_IsPointTwoFiveTimesInputPrice()
    {
        var model = ModelCatalog.Default;
        Assert.Equal(model.InputPricePerMillionTokens * 1.25m, model.CacheWritePricePerMillionTokens);
    }

    [Fact]
    public void CacheReadPrice_IsOneTenthOfInputPrice()
    {
        var model = ModelCatalog.Default;
        Assert.Equal(model.InputPricePerMillionTokens * 0.1m, model.CacheReadPricePerMillionTokens);
    }
}
