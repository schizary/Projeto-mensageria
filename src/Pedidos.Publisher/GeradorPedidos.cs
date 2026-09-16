using Pedidos.Shared.Contratos;

namespace Pedidos.Publisher;

// gera pedidos aleatórios, porém coerentes, para popular a base na demonstração.
public sealed class GeradorPedidos
{
    private readonly Random _sorteio = new();

    private static readonly ClienteDto[] Clientes =
    [
        new() { Id = 7788, Name = "Maria Oliveira", Email = "maria@email.com", Document = "987.654.321-00" },
        new() { Id = 49494, Name = "João Pereira", Email = "joao@email.com", Document = "123.456.789-10" },
        new() { Id = 1001, Name = "Ana Souza", Email = "ana@email.com", Document = "111.222.333-44" },
        new() { Id = 1002, Name = "Carlos Lima", Email = "carlos@email.com", Document = "555.666.777-88" },
        new() { Id = 1003, Name = "Beatriz Santos", Email = "bia@email.com", Document = "999.888.777-66" }
    ];

    private static readonly VendedorDto[] Vendedores =
    [
        new() { Id = 55, Name = "Tech Store", City = "São Paulo", State = "SP" },
        new() { Id = 56, Name = "Casa & Cia", City = "Franca", State = "SP" },
        new() { Id = 57, Name = "Mega Esportes", City = "Belo Horizonte", State = "MG" }
    ];

    private static readonly ProdutoCatalogo[] Catalogo =
    [
        new("abc-1344", "televisao bonita", 2500.00m, "ELEC", "Eletrônicos", "TV", "Televisores"),
        new("abc-2001", "Smartphone X", 1999.90m, "ELEC", "Eletrônicos", "PHONE", "Smartphones"),
        new("abc-2002", "Notebook Pro 14", 4599.00m, "ELEC", "Eletrônicos", "NOTE", "Notebooks"),
        new("abc-3001", "Tênis de corrida", 399.90m, "SPORT", "Esportes", "SHOES", "Calçados"),
        new("abc-4001", "Cafeteira elétrica", 249.90m, "HOME", "Casa", "KITCHEN", "Cozinha")
    ];

    private static readonly string[] Canais = ["mobile_app", "web", "marketplace"];

    public PedidoDto GerarProximo()
    {
        var status = Sortear(StatusPedido.Todos);
        var metodoPagamento = Sortear(MetodosPagamento.Todos);
        var itens = GerarItens();

        return new PedidoDto
        {
            Uuid = GerarUuid(),
            CreatedAt = SortearDataCriacao(),
            Channel = Sortear(Canais),
            Total = itens.Sum(item => item.Total),
            Status = status,
            Customer = Sortear(Clientes),
            Seller = Sortear(Vendedores),
            Items = itens,
            Shipment = GerarEnvio(status),
            Payment = GerarPagamento(status, metodoPagamento),
            Metadata = new MetadadosDto
            {
                Source = "app",
                UserAgent = "Mozilla/5.0",
                IpAddress = $"10.0.0.{_sorteio.Next(1, 255)}"
            }
        };
    }

    private List<ItemPedidoDto> GerarItens()
    {
        return Catalogo
            .OrderBy(_ => _sorteio.Next())
            .Take(_sorteio.Next(1, 4))
            .Select(GerarItem)
            .ToList();
    }

    private ItemPedidoDto GerarItem(ProdutoCatalogo produto, int posicao)
    {
        var quantidade = _sorteio.Next(1, 4);

        return new ItemPedidoDto
        {
            Id = posicao + 1,
            Product = new ProdutoDto { Id = produto.Id, Title = produto.Titulo },
            UnitPrice = produto.Preco,
            Quantity = quantidade,
            Category = new CategoriaDto
            {
                Id = produto.IdCategoria,
                Name = produto.NomeCategoria,
                SubCategory = new SubCategoriaDto
                {
                    Id = produto.IdSubCategoria,
                    Name = produto.NomeSubCategoria
                }
            },
            Total = produto.Preco * quantidade
        };
    }

    private EnvioDto? GerarEnvio(string status)
    {
        if (status is not ("shipped" or "delivered"))
            return null;

        return new EnvioDto
        {
            Carrier = "Correios",
            Service = _sorteio.Next(2) == 0 ? "SEDEX" : "PAC",
            Status = status,
            TrackingCode = $"BR{_sorteio.Next(100_000_000, 999_999_999)}"
        };
    }

    private PagamentoDto GerarPagamento(string status, string metodo)
    {
        var statusPagamento = status switch
        {
            "created" => "pending",
            "canceled" => "refunded",
            _ => "approved"
        };

        return new PagamentoDto
        {
            Method = metodo,
            Status = statusPagamento,
            TransactionId = $"pay_{_sorteio.Next(100_000_000, 999_999_999)}"
        };
    }

    private static string GerarUuid()
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"ORD-{DateTime.UtcNow:yyyy}-{sufixo}";
    }

    private DateTimeOffset SortearDataCriacao()
    {
        return DateTimeOffset.UtcNow
            .AddDays(-_sorteio.Next(0, 90))
            .AddMinutes(-_sorteio.Next(0, 1440));
    }

    private T Sortear<T>(IReadOnlyList<T> opcoes) => opcoes[_sorteio.Next(opcoes.Count)];

    private sealed record ProdutoCatalogo(
        string Id,
        string Titulo,
        decimal Preco,
        string IdCategoria,
        string NomeCategoria,
        string IdSubCategoria,
        string NomeSubCategoria);
}
