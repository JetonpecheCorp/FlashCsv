using System.Buffers.Text;
using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests;

public readonly record struct Money(decimal Amount, string Currency);

public sealed class MoneyConverter : ICsvFieldReader<Money>, ICsvFieldWriter<Money>
{
    // Sérialisation directe vers le buffer destination UTF-8 : "100.50EUR"
    public static bool TryFormat(
        Money value,
        Span<byte> destination,
        out int bytesWritten,
        out CsvErrorCode errorCode)
    {
        errorCode = CsvErrorCode.None;

        // 1. Écriture du montant décimal
        if (!Utf8Formatter.TryFormat(value.Amount, destination, out int amountBytes))
        {
            bytesWritten = 0;
            errorCode = CsvErrorCode.RowTooLong; // Dépassement de capacité du buffer
            return false;
        }

        // 2. Écriture du code de devise (3 octets)
        byte[] currencyBytes = Encoding.UTF8.GetBytes(value.Currency);
        if (destination.Length - amountBytes < currencyBytes.Length)
        {
            bytesWritten = 0;
            errorCode = CsvErrorCode.RowTooLong;
            return false;
        }

        currencyBytes.CopyTo(destination.Slice(amountBytes));
        bytesWritten = amountBytes + currencyBytes.Length;
        return true;
    }

    // Désérialisation depuis les octets bruts UTF-8
    public static bool TryParse(
        ReadOnlySpan<byte> source,
        out Money value,
        out CsvErrorCode errorCode)
    {
        value = default;

        // On attend au minimum 1 chiffre + 3 lettres de devise (ex. "0EUR")
        if (source.Length < 4)
        {
            errorCode = CsvErrorCode.InvalidDecimal;
            return false;
        }

        int amountLength = source.Length - 3;
        ReadOnlySpan<byte> amountSpan = source.Slice(0, amountLength);
        ReadOnlySpan<byte> currencySpan = source.Slice(amountLength, 3);

        if (!Utf8Parser.TryParse(amountSpan, out decimal amount, out int bytesConsumed) || bytesConsumed != amountLength)
        {
            errorCode = CsvErrorCode.InvalidDecimal;
            return false;
        }

        string currency = Encoding.UTF8.GetString(currencySpan);
        value = new Money(amount, currency);
        errorCode = CsvErrorCode.None;
        return true;
    }
}
