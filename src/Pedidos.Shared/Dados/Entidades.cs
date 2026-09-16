namespace Pedidos.Shared.Dados;

// as propriedades espelham as colunas do banco (ver database/schema.sql) e por isso
// permanecem em inglês; os nomes das classes seguem o domínio, em português.

public class Cliente
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Document { get; set; }
}

public class Vendedor
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? City { get; set; }
    public string? State { get; set; }
}

public class Categoria
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ParentId { get; set; }
    public Categoria? Parent { get; set; }
}

public class Produto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
}

public class Pedido
{
    public string Uuid { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string? Channel { get; set; }
    public string Status { get; set; } = "";
    public long CustomerId { get; set; }
    public Cliente Customer { get; set; } = null!;
    public long SellerId { get; set; }
    public Vendedor Seller { get; set; } = null!;
    public DateTimeOffset IndexedAt { get; set; }

    public List<ItemPedido> Itens { get; set; } = [];
    public Pagamento? Pagamento { get; set; }
    public Envio? Envio { get; set; }
    public MetadadosPedido? Metadata { get; set; }
}

public class ItemPedido
{
    public string OrderUuid { get; set; } = "";
    public int Id { get; set; }
    public string ProductId { get; set; } = "";
    public Produto Product { get; set; } = null!;
    public string? CategoryId { get; set; }
    public Categoria? Category { get; set; }
    public string? SubCategoryId { get; set; }
    public Categoria? SubCategory { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class Pagamento
{
    public string OrderUuid { get; set; } = "";
    public string Method { get; set; } = "";
    public string? Status { get; set; }
    public string? TransactionId { get; set; }
}

public class Envio
{
    public string OrderUuid { get; set; } = "";
    public string? Carrier { get; set; }
    public string? Service { get; set; }
    public string? Status { get; set; }
    public string? TrackingCode { get; set; }
}

public class MetadadosPedido
{
    public string OrderUuid { get; set; } = "";
    public string? Source { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}
