using FitCore.Application.Theming;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Theming;

public class BrandColorCalculatorTests
{
    [Theory]
    [InlineData("#B8451F")]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#3b82f6")]
    [InlineData("#FF5733")]
    public void IsValidHex_AcceptsValidHex(string hex)
    {
        BrandColorCalculator.IsValidHex(hex).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("B8451F")]
    [InlineData("#XYZ")]
    [InlineData("#B84")]
    [InlineData("#B8451F00")]
    [InlineData("red")]
    public void IsValidHex_RejectsInvalidHex(string? hex)
    {
        BrandColorCalculator.IsValidHex(hex).ShouldBeFalse();
    }

    [Fact]
    public void ContrastRatio_BlackOnWhite_Is21()
    {
        BrandColorCalculator.ContrastRatio("#000000", "#FFFFFF").ShouldBe(21.0, 0.01);
    }

    [Fact]
    public void ContrastRatio_SameColor_Is1()
    {
        BrandColorCalculator.ContrastRatio("#B8451F", "#B8451F").ShouldBe(1.0, 0.01);
    }

    [Fact]
    public void PickReadableForeground_DarkBackground_ReturnsLight()
    {
        BrandColorCalculator.PickReadableForeground("#1A1A1A").ShouldBe("#FFFFFF");
    }

    [Fact]
    public void PickReadableForeground_LightBackground_ReturnsDark()
    {
        BrandColorCalculator.PickReadableForeground("#FAF7F1").ShouldBe("#141210");
    }

    [Fact]
    public void PickReadableForeground_Terracotta_ReturnsLight()
    {
        // Terracotta #B8451F is dark enough that white text reads better than black.
        BrandColorCalculator.PickReadableForeground("#B8451F").ShouldBe("#FFFFFF");
    }

    [Fact]
    public void PassesWcagAa_TerracottaWithLight_IsTrue()
    {
        // #B8451F and #FAF7F1 should pass AA (4.5:1)
        BrandColorCalculator.PassesWcagAa("#B8451F", "#FAF7F1").ShouldBeTrue();
    }

    [Fact]
    public void Darken_ProducesDarkerHex()
    {
        var darkened = BrandColorCalculator.Darken("#B8451F", 0.2);
        // 0xB8 * 0.8 = 0x93, 0x45 * 0.8 = 0x37, 0x1F * 0.8 = 0x18
        darkened.ShouldBe("#933718");
    }
}



