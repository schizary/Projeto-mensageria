namespace Pedidos.Shared.Contratos;

// os nomes das propriedades definem o json do payload (snake_case) e por isso
// permanecem em inglês, exatamente como no contrato combinado com o marketplace.

public sealed class PedidoDto
{
    public string Uuid { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string? Channel { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public ClienteDto Customer { get; set; } = new();
    public VendedorDto Seller { get; set; } = new();
    public List<ItemPedidoDto> Items { get; set; } = [];
    public EnvioDto? Shipment { get; set; }
    public PagamentoDto? Payment { get; set; }
    public MetadadosDto? Metadata { get; set; }
}

public sealed class ClienteDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Document { get; set; }
}

public sealed class VendedorDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? City { get; set; }
    public string? State { get; set; }
}

public sealed class ItemPedidoDto
{
    public int Id { get; set; }
    public ProdutoDto Product { get; set; } = new();
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public CategoriaDto? Category { get; set; }
    public decimal Total { get; set; }
}

public sealed class ProdutoDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
}

public sealed class CategoriaDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public SubCategoriaDto? SubCategory { get; set; }
}

public sealed class SubCategoriaDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class EnvioDto
{
    public string? Carrier { get; set; }
    public string? Service { get; set; }
    public string? Status { get; set; }
    public string? TrackingCode { get; set; }
}

public sealed class PagamentoDto
{
    public string Method { get; set; } = "";
    public string? Status { get; set; }
    public string? TransactionId { get; set; }
}

public sealed class MetadadosDto
{
    public string? Source { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}

public static class StatusPedido
{
    public static readonly string[] Todos = ["created", "paid", "shipped", "delivered", "canceled"];
}

public static class MetodosPagamento
{
    public static readonly string[] Todos = ["pix", "credit_card", "boleto"];
}
