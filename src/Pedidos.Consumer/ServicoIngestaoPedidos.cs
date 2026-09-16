using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pedidos.Shared.Contratos;
using Pedidos.Shared.Dados;
using Pedidos.Shared.Json;

namespace Pedidos.Consumer;

public enum ResultadoIngestao
{
    Indexado,
    Invalido
}

// grava um pedido recebido da fila. é idempotente: se a mesma mensagem chegar duas vezes
// (o Pub/Sub garante entrega "pelo menos uma vez"), o pedido é atualizado, não duplicado.
public sealed class ServicoIngestaoPedidos(
    PedidosDbContext banco,
    ILogger<ServicoIngestaoPedidos> logger)
{
    public async Task<ResultadoIngestao> IngerirAsync(
        string idMensagem,
        string json,
        CancellationToken cancelamento)
    {
        PedidoDto? mensagem;
        try
        {
            mensagem = JsonSerializer.Deserialize<PedidoDto>(json, PadroesJson.Opcoes);
        }
        catch (JsonException erro)
        {
            logger.LogWarning(
                "Mensagem {IdMensagem} descartada: JSON inválido ({Erro})", idMensagem, erro.Message);
            return ResultadoIngestao.Invalido;
        }

        var erros = ValidadorPedido.Validar(mensagem);
        if (erros.Count > 0)
        {
            logger.LogWarning(
                "Mensagem {IdMensagem} descartada: {Erros}", idMensagem, string.Join("; ", erros));
            return ResultadoIngestao.Invalido;
        }

        var pedidoRecebido = mensagem!;
        var status = pedidoRecebido.Status.Trim().ToLowerInvariant();

        if (!StatusPedido.Todos.Contains(status))
            logger.LogWarning("Pedido {Uuid} com status fora do padrão: {Status}", pedidoRecebido.Uuid, status);

        await SalvarCadastrosAuxiliaresAsync(pedidoRecebido, cancelamento);

        var pedido = await BuscarPedidoAsync(pedidoRecebido.Uuid, cancelamento);
        var ehNovo = pedido is null;

        if (pedido is null)
        {
            pedido = new Pedido { Uuid = pedidoRecebido.Uuid };
            banco.Pedidos.Add(pedido);
        }

        pedido.CreatedAt = pedidoRecebido.CreatedAt.ToUniversalTime();
        pedido.Channel = pedidoRecebido.Channel;
        pedido.Status = status;
        pedido.CustomerId = pedidoRecebido.Customer.Id;
        pedido.SellerId = pedidoRecebido.Seller.Id;
        pedido.IndexedAt = DateTimeOffset.UtcNow;

        SincronizarItens(pedido, pedidoRecebido.Items);
        SincronizarPagamento(pedido, pedidoRecebido.Payment);
        SincronizarEnvio(pedido, pedidoRecebido.Shipment);
        SincronizarMetadados(pedido, pedidoRecebido.Metadata);

        // um único SaveChanges = uma única transação no banco.
        await banco.SaveChangesAsync(cancelamento);

        logger.LogInformation(
            "Pedido {Uuid} {Acao} (mensagem {IdMensagem})",
            pedidoRecebido.Uuid,
            ehNovo ? "indexado" : "atualizado",
            idMensagem);

        return ResultadoIngestao.Indexado;
    }

    private Task<Pedido?> BuscarPedidoAsync(string uuid, CancellationToken cancelamento)
    {
        return banco.Pedidos
            .Include(pedido => pedido.Itens)
            .Include(pedido => pedido.Pagamento)
            .Include(pedido => pedido.Envio)
            .Include(pedido => pedido.Metadata)
            .FirstOrDefaultAsync(pedido => pedido.Uuid == uuid, cancelamento);
    }

    private async Task SalvarCadastrosAuxiliaresAsync(PedidoDto pedido, CancellationToken cancelamento)
    {
        await SalvarClienteAsync(pedido.Customer, cancelamento);
        await SalvarVendedorAsync(pedido.Seller, cancelamento);

        foreach (var item in pedido.Items)
        {
            await SalvarProdutoAsync(item.Product, cancelamento);

            if (item.Category is null)
                continue;

            await SalvarCategoriaAsync(item.Category.Id, item.Category.Name, idCategoriaPai: null, cancelamento);

            if (item.Category.SubCategory is { } subCategoria)
                await SalvarCategoriaAsync(subCategoria.Id, subCategoria.Name, item.Category.Id, cancelamento);
        }
    }

    private async Task SalvarClienteAsync(ClienteDto dto, CancellationToken cancelamento)
    {
        var cliente = await banco.Clientes.FindAsync([dto.Id], cancelamento);

        if (cliente is null)
        {
            cliente = new Cliente { Id = dto.Id };
            banco.Clientes.Add(cliente);
        }

        cliente.Name = dto.Name;
        cliente.Email = dto.Email;
        cliente.Document = dto.Document;
    }

    private async Task SalvarVendedorAsync(VendedorDto dto, CancellationToken cancelamento)
    {
        var vendedor = await banco.Vendedores.FindAsync([dto.Id], cancelamento);

        if (vendedor is null)
        {
            vendedor = new Vendedor { Id = dto.Id };
            banco.Vendedores.Add(vendedor);
        }

        vendedor.Name = dto.Name;
        vendedor.City = dto.City;
        vendedor.State = dto.State;
    }

    private async Task SalvarProdutoAsync(ProdutoDto dto, CancellationToken cancelamento)
    {
        var produto = await banco.Produtos.FindAsync([dto.Id], cancelamento);

        if (produto is null)
        {
            produto = new Produto { Id = dto.Id };
            banco.Produtos.Add(produto);
        }

        produto.Title = dto.Title;
    }

    private async Task SalvarCategoriaAsync(
        string id,
        string nome,
        string? idCategoriaPai,
        CancellationToken cancelamento)
    {
        var categoria = await banco.Categorias.FindAsync([id], cancelamento);

        if (categoria is null)
        {
            categoria = new Categoria { Id = id };
            banco.Categorias.Add(categoria);
        }

        categoria.Name = nome;

        if (idCategoriaPai is not null)
            categoria.ParentId = idCategoriaPai;
    }

    private void SincronizarItens(Pedido pedido, List<ItemPedidoDto> itensRecebidos)
    {
        var idsRecebidos = itensRecebidos.Select(item => item.Id).ToHashSet();

        var itensRemovidos = pedido.Itens
            .Where(item => !idsRecebidos.Contains(item.Id))
            .ToList();

        foreach (var itemRemovido in itensRemovidos)
        {
            pedido.Itens.Remove(itemRemovido);
            banco.ItensPedido.Remove(itemRemovido);
        }

        foreach (var dto in itensRecebidos)
        {
            var item = pedido.Itens.FirstOrDefault(existente => existente.Id == dto.Id);

            if (item is null)
            {
                item = new ItemPedido { OrderUuid = pedido.Uuid, Id = dto.Id };
                pedido.Itens.Add(item);
            }

            item.ProductId = dto.Product.Id;
            item.CategoryId = dto.Category?.Id;
            item.SubCategoryId = dto.Category?.SubCategory?.Id;
            item.UnitPrice = dto.UnitPrice;
            item.Quantity = dto.Quantity;

            // o total não é gravado: a API calcula na hora da consulta.
        }
    }

    private void SincronizarPagamento(Pedido pedido, PagamentoDto? dto)
    {
        if (dto is null)
        {
            if (pedido.Pagamento is not null)
                banco.Remove(pedido.Pagamento);

            pedido.Pagamento = null;
            return;
        }

        pedido.Pagamento ??= new Pagamento { OrderUuid = pedido.Uuid };
        pedido.Pagamento.Method = dto.Method.Trim().ToLowerInvariant();
        pedido.Pagamento.Status = dto.Status;
        pedido.Pagamento.TransactionId = dto.TransactionId;
    }

    private void SincronizarEnvio(Pedido pedido, EnvioDto? dto)
    {
        if (dto is null)
        {
            if (pedido.Envio is not null)
                banco.Remove(pedido.Envio);

            pedido.Envio = null;
            return;
        }

        pedido.Envio ??= new Envio { OrderUuid = pedido.Uuid };
        pedido.Envio.Carrier = dto.Carrier;
        pedido.Envio.Service = dto.Service;
        pedido.Envio.Status = dto.Status;
        pedido.Envio.TrackingCode = dto.TrackingCode;
    }

    private void SincronizarMetadados(Pedido pedido, MetadadosDto? dto)
    {
        if (dto is null)
        {
            if (pedido.Metadata is not null)
                banco.Remove(pedido.Metadata);

            pedido.Metadata = null;
            return;
        }

        pedido.Metadata ??= new MetadadosPedido { OrderUuid = pedido.Uuid };
        pedido.Metadata.Source = dto.Source;
        pedido.Metadata.UserAgent = dto.UserAgent;
        pedido.Metadata.IpAddress = dto.IpAddress;
    }
}
