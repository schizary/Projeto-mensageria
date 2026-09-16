using Google.Cloud.PubSub.V1;

namespace Pedidos.Shared.Mensageria;

// preenchida a partir da seção "PubSub" do appsettings.json.
public sealed class OpcoesPubSub
{
    public string IdProjeto { get; set; } = "projeto-mensageria";
    public string IdTopico { get; set; } = "pedidos";
    public string IdAssinatura { get; set; } = "pedidos-consumer";

    // ex.: "localhost:8085". deixe vazio para usar o Google Pub/Sub real.
    public string? HostEmulador { get; set; }

    // cria tópico e assinatura se ainda não existirem (útil com o emulador).
    public bool CriarRecursosSeNaoExistirem { get; set; } = true;

    public TopicName NomeTopico => TopicName.FromProjectTopic(IdProjeto, IdTopico);

    public SubscriptionName NomeAssinatura =>
        SubscriptionName.FromProjectSubscription(IdProjeto, IdAssinatura);
}
