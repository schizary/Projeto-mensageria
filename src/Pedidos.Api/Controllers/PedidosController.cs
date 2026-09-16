using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos.Api.Modelos;
using Pedidos.Shared.Contratos;
using Pedidos.Shared.Dados;

namespace Pedidos.Api.Controladores;

[ApiController]
[Route("orders")]
[Produces("application/json")]
public class PedidosController(PedidosDbContext banco) : ControllerBase
{
    private const int TamanhoMaximoPagina = 100;

    [HttpGet]
    [ProducesResponseType<RespostaPaginada<PedidoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RespostaPaginada<PedidoDto>>> Listar(
        [FromQuery(Name = "customer.id")] long? idCliente,
        [FromQuery(Name = "product.id")] string? idProduto,
        [FromQuery(Name = "status")] string? status,
        [FromQuery(Name = "seller.id")] long? idVendedor,
        [FromQuery(Name = "page")] int pagina = 1,
        [FromQuery(Name = "page_size")] int tamanhoPagina = 20,
        [FromQuery(Name = "order")] string ordenacao = "desc",
        CancellationToken cancelamento = default)
    {
        if (pagina < 1)
            return Problem(detail: "page deve ser maior ou igual a 1.", statusCode: 400);

        if (tamanhoPagina < 1 || tamanhoPagina > TamanhoMaximoPagina)
            return Problem(detail: $"page_size deve estar entre 1 e {TamanhoMaximoPagina}.", statusCode: 400);

        ordenacao = ordenacao.Trim().ToLowerInvariant();
        if (ordenacao is not ("asc" or "desc"))
            return Problem(detail: "order deve ser 'asc' ou 'desc'.", statusCode: 400);

        var consulta = FiltrarPedidos(idCliente, idProduto, status, idVendedor);
        var totalItens = await consulta.CountAsync(cancelamento);

        consulta = ordenacao == "asc"
            ? consulta.OrderBy(pedido => pedido.CreatedAt).ThenBy(pedido => pedido.Uuid)
            : consulta.OrderByDescending(pedido => pedido.CreatedAt).ThenByDescending(pedido => pedido.Uuid);

        var pedidos = await consulta
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .IncluirGrafoCompleto()
            .AsSplitQuery()
            .ToListAsync(cancelamento);

        return new RespostaPaginada<PedidoDto>
        {
            Data = pedidos.Select(pedido => pedido.ConverterPedidoDto()).ToList(),
            Page = pagina,
            PageSize = tamanhoPagina,
            TotalItems = totalItens,
            TotalPages = (int)Math.Ceiling(totalItens / (double)tamanhoPagina)
        };
    }

    [HttpGet("{uuid}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> ObterPorUuid(string uuid, CancellationToken cancelamento)
    {
        var pedido = await banco.Pedidos
            .AsNoTracking()
            .IncluirGrafoCompleto()
            .AsSplitQuery()
            .FirstOrDefaultAsync(pedido => pedido.Uuid == uuid, cancelamento);

        if (pedido is null)
            return Problem(detail: $"Pedido '{uuid}' não encontrado.", statusCode: 404);

        return pedido.ConverterPedidoDto();
    }

    [HttpGet("{uuid}/items")]
    [ProducesResponseType<RespostaItensPedido>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespostaItensPedido>> ObterItens(
        string uuid,
        CancellationToken cancelamento)
    {
        var pedidoExiste = await banco.Pedidos.AnyAsync(pedido => pedido.Uuid == uuid, cancelamento);
        if (!pedidoExiste)
            return Problem(detail: $"Pedido '{uuid}' não encontrado.", statusCode: 404);

        var itens = await banco.ItensPedido
            .AsNoTracking()
            .Include(item => item.Product)
            .Include(item => item.Category)
            .Include(item => item.SubCategory)
            .Where(item => item.OrderUuid == uuid)
            .OrderBy(item => item.Id)
            .ToListAsync(cancelamento);

        return new RespostaItensPedido
        {
            Items = itens.Select(item => item.ConverterItemDto()).ToList()
        };
    }

    [HttpGet("financial-summary")]
    [ProducesResponseType<RespostaResumoFinanceiro>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RespostaResumoFinanceiro>> ResumoFinanceiro(
        [FromQuery(Name = "seller.id")] long? idVendedor,
        [FromQuery(Name = "start_date")] string? dataInicial,
        [FromQuery(Name = "end_date")] string? dataFinal,
        CancellationToken cancelamento = default)
    {
        var periodoValido = PeriodoDatas.TentarInterpretar(
            dataInicial, dataFinal, out var inicio, out var fimExclusivo, out var erro);

        if (!periodoValido)
            return Problem(detail: erro, statusCode: 400);

        IQueryable<Pedido> consulta = banco.Pedidos.AsNoTracking();

        if (idVendedor is not null)
            consulta = consulta.Where(pedido => pedido.SellerId == idVendedor.Value);

        if (inicio is not null)
            consulta = consulta.Where(pedido => pedido.CreatedAt >= inicio.Value);

        if (fimExclusivo is not null)
            consulta = consulta.Where(pedido => pedido.CreatedAt < fimExclusivo.Value);

        // o total de cada pedido é somado no banco (unit_price * quantity).
        var linhas = await consulta
            .Select(pedido => new LinhaResumo(
                pedido.Status,
                pedido.Pagamento != null ? pedido.Pagamento.Method : "unknown",
                pedido.Itens.Sum(item => item.UnitPrice * item.Quantity)))
            .ToListAsync(cancelamento);

        var receitaTotal = linhas.Sum(linha => linha.Total);

        return new RespostaResumoFinanceiro
        {
            TotalOrders = linhas.Count,
            TotalRevenue = receitaTotal,
            AverageOrderValue = CalcularTicketMedio(receitaTotal, linhas.Count),
            ByStatus = ContarPorStatus(linhas),
            ByPaymentMethod = ResumirPorMetodoPagamento(linhas)
        };
    }

    private IQueryable<Pedido> FiltrarPedidos(
        long? idCliente,
        string? idProduto,
        string? status,
        long? idVendedor)
    {
        IQueryable<Pedido> consulta = banco.Pedidos.AsNoTracking();

        if (idCliente is not null)
            consulta = consulta.Where(pedido => pedido.CustomerId == idCliente.Value);

        if (!string.IsNullOrWhiteSpace(idProduto))
            consulta = consulta.Where(pedido => pedido.Itens.Any(item => item.ProductId == idProduto));

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusNormalizado = status.Trim().ToLowerInvariant();
            consulta = consulta.Where(pedido => pedido.Status == statusNormalizado);
        }

        if (idVendedor is not null)
            consulta = consulta.Where(pedido => pedido.SellerId == idVendedor.Value);

        return consulta;
    }

    private static decimal CalcularTicketMedio(decimal receitaTotal, int quantidadePedidos)
    {
        if (quantidadePedidos == 0)
            return 0m;

        return Math.Round(receitaTotal / quantidadePedidos, 2, MidpointRounding.AwayFromZero);
    }

    private static Dictionary<string, int> ContarPorStatus(List<LinhaResumo> linhas)
    {
        var contagem = StatusPedido.Todos.ToDictionary(status => status, _ => 0);

        foreach (var linha in linhas)
            contagem[linha.Status] = contagem.GetValueOrDefault(linha.Status) + 1;

        return contagem;
    }

    private static Dictionary<string, ResumoPorMetodoPagamento> ResumirPorMetodoPagamento(
        List<LinhaResumo> linhas)
    {
        var resumo = MetodosPagamento.Todos
            .ToDictionary(metodo => metodo, _ => new ResumoPorMetodoPagamento());

        foreach (var linha in linhas)
        {
            if (!resumo.TryGetValue(linha.Metodo, out var resumoMetodo))
                resumo[linha.Metodo] = resumoMetodo = new ResumoPorMetodoPagamento();

            resumoMetodo.Count++;
            resumoMetodo.Total += linha.Total;
        }

        return resumo;
    }

    private sealed record LinhaResumo(string Status, string Metodo, decimal Total);
}
