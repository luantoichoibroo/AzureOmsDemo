
namespace AzureOmsDemo.Services;

public class OrderTotalCalculator
{
    public decimal Calculate(decimal price, int quantity)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price cannot be negative.");
        }

        return price * quantity;
    }
}
