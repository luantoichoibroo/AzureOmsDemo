using AzureOmsDemo.Services;
using Xunit;

namespace AzureOmsDemo.Tests;

public class OrderTotalCalculatorTests
{
    [Fact]
    public void Calculate_ValidPriceAndQuantity_ReturnsCorrectTotal()
    {
        // Arrange
        var calculator = new OrderTotalCalculator();
        decimal price = 100m;
        int quantity = 2;

        // Act
        decimal actual = calculator.Calculate(price, quantity);

        // Assert
        Assert.Equal(200m, actual);
    }


    [Fact]
    public void Calculate_NegativePrice_ThrowsException()
    {
        // Arrange
        var calculator = new OrderTotalCalculator();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            calculator.Calculate(-100m, 2);
        });
    }

}