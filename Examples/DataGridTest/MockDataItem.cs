namespace DataGridTest;

public sealed record MockDataItem(
    int Id,
    string OrderNumber,
    DateTime OrderDate,
    string Customer,
    string Product,
    string Category,
    int Quantity,
    decimal UnitPrice,
    decimal Total,
    string Status,
    string City)
{
    private static readonly string[] Customers =
    [
        "Contoso", "Fabrikam", "Northwind", "Adventure Works", "Tailspin Toys",
        "Wide World Importers", "Litware", "Proseware", "Woodgrove Bank", "Humongous Insurance"
    ];

    private static readonly string[] Products =
    [
        "Surface Laptop", "Mechanical Keyboard", "4K Display", "USB-C Dock",
        "Noise-cancelling Headphones", "Ergonomic Mouse", "Web Camera", "Desk Lamp"
    ];

    private static readonly string[] Categories =
    [
        "Computer", "Accessory", "Display", "Office", "Audio"
    ];

    private static readonly string[] Statuses =
    [
        "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
    ];

    private static readonly string[] Cities =
    [
        "Shanghai", "Beijing", "Shenzhen", "Hangzhou", "Chengdu", "Nanjing", "Wuhan", "Suzhou"
    ];

    public static MockDataItem[] CreateMany(int count)
    {
        Random random = new(20260925);
        DateTime today = DateTime.Today;
        MockDataItem[] items = new MockDataItem[count];

        for (int index = 0; index < count; index++)
        {
            int quantity = random.Next(1, 25);
            decimal unitPrice = decimal.Round((decimal)(random.NextDouble() * 1990 + 10), 2);

            items[index] = new MockDataItem(
                index + 1,
                $"ORD-{index + 1:000000}",
                today.AddDays(-random.Next(0, 730)).AddMinutes(random.Next(0, 1440)),
                Customers[random.Next(Customers.Length)],
                Products[random.Next(Products.Length)],
                Categories[random.Next(Categories.Length)],
                quantity,
                unitPrice,
                quantity * unitPrice,
                Statuses[random.Next(Statuses.Length)],
                Cities[random.Next(Cities.Length)]);
        }

        return items;
    }
}
