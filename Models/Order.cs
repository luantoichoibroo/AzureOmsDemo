namespace AzureOmsDemo.Models;

public class Order
{
    public int Id { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "Pending";
    public Order(int id, string orderCode, decimal totalAmount, string status)
    {
        Id = id;
        OrderCode = orderCode;
        TotalAmount = totalAmount;
        Status = status;
    }
    public Order()
    {
        
    }
}
