using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Pedidos.Shared.Dados;

public static class ExtensoesDados
{
    public static IServiceCollection AdicionarBancoPedidos(
        this IServiceCollection servicos,
        string stringConexao)
    {
        return servicos.AddDbContext<PedidosDbContext>(opcoes => opcoes
            .UseNpgsql(stringConexao)
            .UseSnakeCaseNamingConvention());
    }

    public static IQueryable<Pedido> IncluirGrafoCompleto(this IQueryable<Pedido> consulta)
    {
        return consulta
            .Include(pedido => pedido.Customer)
            .Include(pedido => pedido.Seller)
            .Include(pedido => pedido.Itens).ThenInclude(item => item.Product)
            .Include(pedido => pedido.Itens).ThenInclude(item => item.Category)
            .Include(pedido => pedido.Itens).ThenInclude(item => item.SubCategory)
            .Include(pedido => pedido.Pagamento)
            .Include(pedido => pedido.Envio)
            .Include(pedido => pedido.Metadata);
    }
}
