using Pedidos.Shared.Dados;

namespace Pedidos.Shared.Contratos;

// os campos "total" não existem no banco: são calculados aqui, a cada consulta.
public static class ConversorPedido
{
    public static PedidoDto ConverterPedidoDto(this Pedido pedido)
    {
        var itens = pedido.Itens
            .OrderBy(item => item.Id)
            .Select(ConverterItemDto)
            .ToList();

        return new PedidoDto
        {
            Uuid = pedido.Uuid,
            CreatedAt = pedido.CreatedAt,
            Channel = pedido.Channel,
            Total = itens.Sum(item => item.Total),
            Status = pedido.Status,
            Customer = ConverterClienteDto(pedido.Customer),
            Seller = ConverterVendedorDto(pedido.Seller),
            Items = itens,
            Shipment = ConverterEnvioDto(pedido.Envio),
            Payment = ConverterPagamentoDto(pedido.Pagamento),
            Metadata = ConverterMetadadosDto(pedido.Metadata)
        };
    }

    public static ItemPedidoDto ConverterItemDto(this ItemPedido item) => new()
    {
        Id = item.Id,
        Product = new ProdutoDto
        {
            Id = item.Product.Id,
            Title = item.Product.Title
        },
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
        Category = ConverterCategoriaDto(item.Category, item.SubCategory),
        Total = item.UnitPrice * item.Quantity
    };

    private static ClienteDto ConverterClienteDto(Cliente cliente) => new()
    {
        Id = cliente.Id,
        Name = cliente.Name,
        Email = cliente.Email,
        Document = cliente.Document
    };

    private static VendedorDto ConverterVendedorDto(Vendedor vendedor) => new()
    {
        Id = vendedor.Id,
        Name = vendedor.Name,
        City = vendedor.City,
        State = vendedor.State
    };

    private static CategoriaDto? ConverterCategoriaDto(Categoria? categoria, Categoria? subCategoria)
    {
        if (categoria is null)
            return null;

        return new CategoriaDto
        {
            Id = categoria.Id,
            Name = categoria.Name,
            SubCategory = subCategoria is null
                ? null
                : new SubCategoriaDto { Id = subCategoria.Id, Name = subCategoria.Name }
        };
    }

    private static EnvioDto? ConverterEnvioDto(Envio? envio)
    {
        if (envio is null)
            return null;

        return new EnvioDto
        {
            Carrier = envio.Carrier,
            Service = envio.Service,
            Status = envio.Status,
            TrackingCode = envio.TrackingCode
        };
    }

    private static PagamentoDto? ConverterPagamentoDto(Pagamento? pagamento)
    {
        if (pagamento is null)
            return null;

        return new PagamentoDto
        {
            Method = pagamento.Method,
            Status = pagamento.Status,
            TransactionId = pagamento.TransactionId
        };
    }

    private static MetadadosDto? ConverterMetadadosDto(MetadadosPedido? metadados)
    {
        if (metadados is null)
            return null;

        return new MetadadosDto
        {
            Source = metadados.Source,
            UserAgent = metadados.UserAgent,
            IpAddress = metadados.IpAddress
        };
    }
}
