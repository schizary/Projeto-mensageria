using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pedidos.Shared.Json;

public static class PadroesJson
{
    public static readonly JsonSerializerOptions Opcoes = Criar();

    public static JsonSerializerOptions Criar()
    {
        var opcoes = new JsonSerializerOptions();
        Aplicar(opcoes);
        return opcoes;
    }

    public static void Aplicar(JsonSerializerOptions opcoes)
    {
        opcoes.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        opcoes.PropertyNameCaseInsensitive = true;
        opcoes.Converters.Add(new ConversorDataUtc());
    }
}

// lê datas ISO-8601 e escreve sempre em UTC, no formato "2025-10-01T10:15:00Z".
public sealed class ConversorDataUtc : JsonConverter<DateTimeOffset>
{
    private const string FormatoSaida = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public override DateTimeOffset Read(
        ref Utf8JsonReader leitor,
        Type tipoDestino,
        JsonSerializerOptions opcoes)
    {
        if (leitor.TokenType != JsonTokenType.String)
            throw new JsonException("Data deve ser uma string ISO-8601.");

        var textoOriginal = leitor.GetString();
        var interpretou = DateTimeOffset.TryParse(
            textoOriginal,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var data);

        if (!interpretou)
            throw new JsonException($"Data inválida: '{textoOriginal}'");

        return data.ToUniversalTime();
    }

    public override void Write(
        Utf8JsonWriter escritor,
        DateTimeOffset valor,
        JsonSerializerOptions opcoes)
    {
        var textoUtc = valor.ToUniversalTime().ToString(FormatoSaida, CultureInfo.InvariantCulture);
        escritor.WriteStringValue(textoUtc);
    }
}
