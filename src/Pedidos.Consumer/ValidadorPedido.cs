using Pedidos.Shared.Contratos;

namespace Pedidos.Consumer;

public static class ValidadorPedido
{
    public static List<string> Validar(PedidoDto? pedido)
    {
        var erros = new List<string>();

        if (pedido is null)
        {
            erros.Add("payload vazio");
            return erros;
        }

        if (string.IsNullOrWhiteSpace(pedido.Uuid))
            erros.Add("uuid é obrigatório");

        if (pedido.CreatedAt == default)
            erros.Add("created_at é obrigatório");

        if (string.IsNullOrWhiteSpace(pedido.Status))
            erros.Add("status é obrigatório");

        if (pedido.Customer is null || pedido.Customer.Id <= 0 || string.IsNullOrWhiteSpace(pedido.Customer.Name))
            erros.Add("customer inválido (id e name são obrigatórios)");

        if (pedido.Seller is null || pedido.Seller.Id <= 0 || string.IsNullOrWhiteSpace(pedido.Seller.Name))
            erros.Add("seller inválido (id e name são obrigatórios)");

        ValidarItens(pedido, erros);

        if (pedido.Payment is not null && string.IsNullOrWhiteSpace(pedido.Payment.Method))
            erros.Add("payment.method é obrigatório quando payment é informado");

        return erros;
    }

    private static void ValidarItens(PedidoDto pedido, List<string> erros)
    {
        if (pedido.Items is null || pedido.Items.Count == 0)
        {
            erros.Add("items deve ter ao menos um item");
            return;
        }

        var idsDistintos = pedido.Items.Select(item => item.Id).Distinct().Count();
        if (idsDistintos != pedido.Items.Count)
            erros.Add("items com id duplicado");

        foreach (var item in pedido.Items)
            ValidarItem(item, erros);
    }

    private static void ValidarItem(ItemPedidoDto item, List<string> erros)
    {
        var produtoInvalido = item.Product is null
            || string.IsNullOrWhiteSpace(item.Product.Id)
            || string.IsNullOrWhiteSpace(item.Product.Title);

        if (produtoInvalido)
            erros.Add($"item {item.Id}: product inválido");

        if (item.Quantity <= 0)
            erros.Add($"item {item.Id}: quantity deve ser maior que zero");

        if (item.UnitPrice < 0)
            erros.Add($"item {item.Id}: unit_price não pode ser negativo");

        if (item.Category is not null && string.IsNullOrWhiteSpace(item.Category.Id))
            erros.Add($"item {item.Id}: category sem id");

        if (item.Category?.SubCategory is not null && string.IsNullOrWhiteSpace(item.Category.SubCategory.Id))
            erros.Add($"item {item.Id}: sub_category sem id");
    }
}
