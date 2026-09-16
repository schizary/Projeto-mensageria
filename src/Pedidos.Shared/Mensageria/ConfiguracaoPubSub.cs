using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Grpc.Core;

namespace Pedidos.Shared.Mensageria;

public static class ConfiguracaoPubSub
{
    private const int MaximoTentativas = 10;
    private static readonly TimeSpan IntervaloTentativas = TimeSpan.FromSeconds(3);
    private const int PrazoConfirmacaoSegundos = 60;

    // as bibliotecas do Google detectam o emulador pela variável PUBSUB_EMULATOR_HOST.
    public static void ConfigurarEmulador(OpcoesPubSub opcoes)
    {
        if (string.IsNullOrWhiteSpace(opcoes.HostEmulador))
            return;

        Environment.SetEnvironmentVariable("PUBSUB_EMULATOR_HOST", opcoes.HostEmulador);
    }

    public static async Task GarantirTopicoAssinaturaAsync(
        OpcoesPubSub opcoes,
        Action<string> registrarLog,
        CancellationToken cancelamento)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                await CriarTopicoAssinaturaAsync(opcoes, registrarLog, cancelamento);
                return;
            }
            catch (RpcException erro)
                when (erro.StatusCode == StatusCode.Unavailable && tentativa < MaximoTentativas)
            {
                registrarLog($"Pub/Sub indisponível (tentativa {tentativa}/{MaximoTentativas}), aguardando...");
                await Task.Delay(IntervaloTentativas, cancelamento);
            }
        }
    }

    private static async Task CriarTopicoAssinaturaAsync(
        OpcoesPubSub opcoes,
        Action<string> registrarLog,
        CancellationToken cancelamento)
    {
        var apiPublicacao = await new PublisherServiceApiClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.BuildAsync(cancelamento);

        var apiAssinatura = await new SubscriberServiceApiClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.BuildAsync(cancelamento);

        try
        {
            await apiPublicacao.CreateTopicAsync(opcoes.NomeTopico, cancelamento);
            registrarLog($"Tópico criado: {opcoes.NomeTopico}");
        }
        catch (RpcException erro) when (erro.StatusCode == StatusCode.AlreadyExists)
        {
            // tópico já existe, nada a fazer.
        }

        try
        {
            await apiAssinatura.CreateSubscriptionAsync(
                opcoes.NomeAssinatura,
                opcoes.NomeTopico,
                pushConfig: null,
                ackDeadlineSeconds: PrazoConfirmacaoSegundos,
                cancelamento);

            registrarLog($"Assinatura criada: {opcoes.NomeAssinatura}");
        }
        catch (RpcException erro) when (erro.StatusCode == StatusCode.AlreadyExists)
        {
            // assinatura já existe, nada a fazer.
        }
    }
}
