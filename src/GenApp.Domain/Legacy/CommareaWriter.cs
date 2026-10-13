using System.Text;

namespace GenApp.Domain.Legacy;

public sealed class CommareaWriter
{
    private readonly StringBuilder _buffer = new();

    // PIC X(n): MOVE pads on the right and truncates on the right.
    public void Text(string? value, int length)
    {
        var text = value ?? "";
        _buffer.Append(text.Length > length ? text[..length] : text.PadRight(length));
    }

    // PIC 9(n): MOVE drops the sign, pads with zeros on the left and truncates on the left.
    public void Number(long value, int length)
    {
        var digits = value.ToString().TrimStart('-');
        _buffer.Append(digits.Length > length ? digits[^length..] : digits.PadLeft(length, '0'));
    }

    public override string ToString() => _buffer.ToString();
}
