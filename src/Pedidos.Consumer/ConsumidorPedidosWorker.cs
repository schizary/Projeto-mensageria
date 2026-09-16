using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Pedidos.Shared.Mensageria;

namespace Pedidos.Consumer;

// assina o tópico de pedidos no Google Pub/Sub e entrega cada mensagem para a ingestão.
public sealed class ConsumidorPedidosWorker(
    IServiceScopeFactory fabricaEscopos,
    OpcoesPubSub opcoes,
    ILogger<ConsumidorPedidosWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PrazoParada = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken cancelamento)
    {
        if (opcoes.CriarRecursosSeNaoExistirem)
        {
            await ConfiguracaoPubSub.GarantirTopicoAssinaturaAsync(
                opcoes,
                mensagem => logger.LogInformation("{Mensagem}", mensagem),
                cancelamento);
        }

        var assinante = await new SubscriberClientBuilder
        {
            SubscriptionName = opcoes.NomeAssinatura,
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.BuildAsync(cancelamento);

        using var registroParada = cancelamento.Register(
            () => _ = assinante.StopAsync(PrazoParada));

        logger.LogInformation("Consumindo mensagens de {Assinatura}", opcoes.NomeAssinatura);

        await assinante.StartAsync(ProcessarMensagemAsync);
    }

    private async Task<SubscriberClient.Reply> ProcessarMensagemAsync(
        PubsubMessage mensagem,
        CancellationToken cancelamento)
    {
        try
        {
            using var escopo = fabricaEscopos.CreateScope();
            var ingestao = escopo.ServiceProvider.GetRequiredService<ServicoIngestaoPedidos>();

            await ingestao.IngerirAsync(mensagem.MessageId, mensagem.Data.ToStringUtf8(), cancelamento);

            // mensagens inválidas também recebem Ack: reenviá-las nunca vai dar certo
            // (em produção, o ideal é configurar uma dead-letter topic).
            return SubscriberClient.Reply.Ack;
        }
        catch (Exception erro)
        {
            // erro transitório (banco fora do ar, conflito de concorrência...): o Pub/Sub reentrega.
            logger.LogError(erro, "Falha ao processar mensagem {IdMensagem}; será reentregue", mensagem.MessageId);
            return SubscriberClient.Reply.Nack;
        }
    }
}
