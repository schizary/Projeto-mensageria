using Pedidos.Shared.Dados;
using Pedidos.Shared.Json;

var construtor = WebApplication.CreateBuilder(args);

var stringConexao = construtor.Configuration.GetConnectionString("Pedidos")
    ?? throw new InvalidOperationException("ConnectionStrings:Pedidos não configurada.");

construtor.Services.AdicionarBancoPedidos(stringConexao);

construtor.Services
    .AddControllers()
    .AddJsonOptions(opcoes => PadroesJson.Aplicar(opcoes.JsonSerializerOptions));

construtor.Services.AddEndpointsApiExplorer();
construtor.Services.AddSwaggerGen();

var aplicacao = construtor.Build();

aplicacao.UseSwagger();
aplicacao.UseSwaggerUI();
aplicacao.MapControllers();

aplicacao.Run();
