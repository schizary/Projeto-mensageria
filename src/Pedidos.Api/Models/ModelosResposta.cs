using Pedidos.Shared.Contratos;

namespace Pedidos.Api.Modelos;

public sealed class RespostaPaginada<T>
{
    public List<T> Data { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
}

public sealed class RespostaItensPedido
{
    public List<ItemPedidoDto> Items { get; init; } = [];
}

public sealed class RespostaResumoFinanceiro
{
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageOrderValue { get; init; }
    public Dictionary<string, int> ByStatus { get; init; } = [];
    public Dictionary<string, ResumoPorMetodoPagamento> ByPaymentMethod { get; init; } = [];
}

public sealed class ResumoPorMetodoPagamento
{
    public int Count { get; set; }
    public decimal Total { get; set; }
}
