using System.Globalization;

namespace Pedidos.Api.Modelos;

// aceita "2025-10-01" (dia inteiro) ou data/hora ISO-8601 ("2025-10-01T10:00:00Z").
// o fim vira um limite exclusivo, então end_date=2025-10-31 inclui o dia 31 inteiro.
public static class PeriodoDatas
{
    public static bool TentarInterpretar(
        string? inicioInformado,
        string? fimInformado,
        out DateTimeOffset? inicio,
        out DateTimeOffset? fimExclusivo,
        out string? erro)
    {
        inicio = null;
        fimExclusivo = null;
        erro = null;

        if (!string.IsNullOrWhiteSpace(inicioInformado))
        {
            if (!TentarInterpretarValor(inicioInformado, out var data, out _))
            {
                erro = "start_date inválido. Use yyyy-MM-dd ou ISO-8601 (ex.: 2025-10-01T00:00:00Z).";
                return false;
            }

            inicio = data;
        }

        if (!string.IsNullOrWhiteSpace(fimInformado))
        {
            if (!TentarInterpretarValor(fimInformado, out var data, out var apenasData))
            {
                erro = "end_date inválido. Use yyyy-MM-dd ou ISO-8601 (ex.: 2025-10-31T23:59:59Z).";
                return false;
            }

            fimExclusivo = apenasData ? data.AddDays(1) : data.AddTicks(1);
        }

        if (inicio is not null && fimExclusivo is not null && inicio >= fimExclusivo)
        {
            erro = "start_date deve ser anterior ou igual a end_date.";
            return false;
        }

        return true;
    }

    private static bool TentarInterpretarValor(
        string textoOriginal,
        out DateTimeOffset valor,
        out bool apenasData)
    {
        var ehApenasData = DateOnly.TryParseExact(
            textoOriginal,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var data);

        if (ehApenasData)
        {
            apenasData = true;
            valor = new DateTimeOffset(data.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            return true;
        }

        apenasData = false;
        return DateTimeOffset.TryParse(
            textoOriginal,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out valor);
    }
}
