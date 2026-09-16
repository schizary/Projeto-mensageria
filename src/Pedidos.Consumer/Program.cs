using Pedidos.Consumer;
using Pedidos.Shared.Dados;
using Pedidos.Shared.Mensageria;

var construtor = Host.CreateApplicationBuilder(args);

var opcoesPubSub = construtor.Configuration.GetSection("PubSub").Get<OpcoesPubSub>()
    ?? new OpcoesPubSub();

ConfiguracaoPubSub.ConfigurarEmulador(opcoesPubSub);

var stringConexao = construtor.Configuration.GetConnectionString("Pedidos")
    ?? throw new InvalidOperationException("ConnectionStrings:Pedidos não configurada.");

construtor.Services.AddSingleton(opcoesPubSub);
construtor.Services.AdicionarBancoPedidos(stringConexao);
construtor.Services.AddScoped<ServicoIngestaoPedidos>();
construtor.Services.AddHostedService<ConsumidorPedidosWorker>();

construtor.Build().Run();
