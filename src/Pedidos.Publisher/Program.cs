// ferramenta de apoio para a demonstração: publica pedidos no tópico do Pub/Sub.
//
//   dotnet run --project src/Pedidos.Publisher                          -> 10 pedidos aleatórios
//   dotnet run --project src/Pedidos.Publisher -- 200                   -> 200 pedidos aleatórios
//   dotnet run --project src/Pedidos.Publisher -- --file samples/pedido-exemplo.json

using System.Text.Json;
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Pedidos.Publisher;
using Pedidos.Shared.Json;
using Pedidos.Shared.Mensageria;

var opcoes = new OpcoesPubSub
{
    IdProjeto = LerVariavel("PUBSUB_PROJECT_ID", "projeto-mensageria"),
    IdTopico = LerVariavel("PUBSUB_TOPIC_ID", "pedidos"),
    IdAssinatura = LerVariavel("PUBSUB_SUBSCRIPTION_ID", "pedidos-consumer"),
    HostEmulador = LerVariavel("PUBSUB_EMULATOR_HOST", "localhost:8085")
};

ConfiguracaoPubSub.ConfigurarEmulador(opcoes);

var mensagens = MontarMensagens(args);

// o Pub/Sub só entrega mensagens para assinaturas que já existiam no momento da
// publicação, por isso garantimos tópico e assinatura antes de publicar.
await ConfiguracaoPubSub.GarantirTopicoAssinaturaAsync(opcoes, Console.WriteLine, CancellationToken.None);

var publicador = await new PublisherClientBuilder
{
    TopicName = opcoes.NomeTopico,
    EmulatorDetection = EmulatorDetection.EmulatorOrProduction
}.BuildAsync();

foreach (var mensagem in mensagens)
{
    var idMensagem = await publicador.PublishAsync(mensagem);
    Console.WriteLine($"Publicada mensagem {idMensagem}");
}

await publicador.ShutdownAsync(TimeSpan.FromSeconds(15));
Console.WriteLine($"{mensagens.Count} mensagem(ns) publicada(s) em {opcoes.NomeTopico}");

static List<string> MontarMensagens(string[] argumentos)
{
    if (argumentos.Length >= 2 && argumentos[0] == "--file")
        return [File.ReadAllText(argumentos[1])];

    var quantidade = argumentos.Length > 0 && int.TryParse(argumentos[0], out var informada) && informada > 0
        ? informada
        : 10;

    var gerador = new GeradorPedidos();
    var mensagensGeradas = new List<string>(quantidade);

    for (var i = 0; i < quantidade; i++)
        mensagensGeradas.Add(JsonSerializer.Serialize(gerador.GerarProximo(), PadroesJson.Opcoes));

    return mensagensGeradas;
}

static string LerVariavel(string nome, string valorPadrao) =>
    Environment.GetEnvironmentVariable(nome) is { Length: > 0 } valor ? valor : valorPadrao;
