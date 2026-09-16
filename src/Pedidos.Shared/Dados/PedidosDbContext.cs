using Microsoft.EntityFrameworkCore;

namespace Pedidos.Shared.Dados;

public class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Vendedor> Vendedores => Set<Vendedor>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();

    // os nomes das colunas viram snake_case automaticamente (UseSnakeCaseNamingConvention).
    protected override void OnModelCreating(ModelBuilder modelo)
    {
        ConfigurarCliente(modelo);
        ConfigurarVendedor(modelo);
        ConfigurarCategoria(modelo);
        ConfigurarProduto(modelo);
        ConfigurarPedido(modelo);
        ConfigurarItemPedido(modelo);
        ConfigurarPagamento(modelo);
        ConfigurarEnvio(modelo);
        ConfigurarMetadados(modelo);
    }

    private static void ConfigurarCliente(ModelBuilder modelo)
    {
        modelo.Entity<Cliente>(cliente =>
        {
            cliente.ToTable("cliente");
            cliente.Property(c => c.Id).ValueGeneratedNever();
        });
    }

    private static void ConfigurarVendedor(ModelBuilder modelo)
    {
        modelo.Entity<Vendedor>(vendedor =>
        {
            vendedor.ToTable("seller");
            vendedor.Property(v => v.Id).ValueGeneratedNever();
        });
    }

    private static void ConfigurarCategoria(ModelBuilder modelo)
    {
        modelo.Entity<Categoria>(categoria =>
        {
            categoria.ToTable("categoria");

            // categoria e subcategoria moram na mesma tabela, por auto-relacionamento.
            categoria
                .HasOne(c => c.Parent)
                .WithMany()
                .HasForeignKey(c => c.ParentId);
        });
    }

    private static void ConfigurarProduto(ModelBuilder modelo)
    {
        modelo.Entity<Produto>(produto => produto.ToTable("produto"));
    }

    private static void ConfigurarPedido(ModelBuilder modelo)
    {
        modelo.Entity<Pedido>(pedido =>
        {
            pedido.ToTable("pedido");
            pedido.HasKey(p => p.Uuid);

            pedido.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId);
            pedido.HasOne(p => p.Seller).WithMany().HasForeignKey(p => p.SellerId);

            pedido.HasMany(p => p.Itens).WithOne().HasForeignKey(i => i.OrderUuid);
            pedido.HasOne(p => p.Pagamento).WithOne().HasForeignKey<Pagamento>(pg => pg.OrderUuid);
            pedido.HasOne(p => p.Envio).WithOne().HasForeignKey<Envio>(e => e.OrderUuid);
            pedido.HasOne(p => p.Metadata).WithOne().HasForeignKey<MetadadosPedido>(m => m.OrderUuid);
        });
    }

    private static void ConfigurarItemPedido(ModelBuilder modelo)
    {
        modelo.Entity<ItemPedido>(item =>
        {
            item.ToTable("item_pedido");
            item.HasKey(i => new { i.OrderUuid, i.Id });
            item.Property(i => i.Id).ValueGeneratedNever();
            item.Property(i => i.UnitPrice).HasPrecision(12, 2);

            item.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
            item.HasOne(i => i.Category).WithMany().HasForeignKey(i => i.CategoryId);
            item.HasOne(i => i.SubCategory).WithMany().HasForeignKey(i => i.SubCategoryId);
        });
    }

    private static void ConfigurarPagamento(ModelBuilder modelo)
    {
        modelo.Entity<Pagamento>(pagamento =>
        {
            pagamento.ToTable("pagamento");
            pagamento.HasKey(p => p.OrderUuid);
        });
    }

    private static void ConfigurarEnvio(ModelBuilder modelo)
    {
        modelo.Entity<Envio>(envio =>
        {
            envio.ToTable("envio");
            envio.HasKey(e => e.OrderUuid);
        });
    }

    private static void ConfigurarMetadados(ModelBuilder modelo)
    {
        modelo.Entity<MetadadosPedido>(metadados =>
        {
            metadados.ToTable("metadata_pedido");
            metadados.HasKey(m => m.OrderUuid);
        });
    }
}
