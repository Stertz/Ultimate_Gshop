namespace Ultimate_Gshop.Itens.Services;

internal static class ItemPayload
{
    internal static List<Sale> NormalizeSales(IEnumerable<Sale>? source)
    {
        var sales = source?.ToList() ?? [];
        if (sales.Count > 4) sales = sales.OrderByDescending(sale => sale.Price).Take(4).ToList();
        sales = sales.Select(sale => sale.Clone()).ToList();
        while (sales.Count < 4) sales.Add(new Sale());
        return sales;
    }
}
