namespace GenApp.Domain.Legacy;

public sealed class CommareaReader
{
    private readonly string _raw;
    private int _position;

    public CommareaReader(string raw) => _raw = raw;

    // PIC X(n): trailing spaces belong to the field width, leading spaces are data.
    public string Text(int length) => Next(length).TrimEnd(' ');

    // PIC 9(n): only digits are valid; spaces or signs mean the field was never set or is corrupt.
    public long Number(int length)
    {
        var field = Next(length);
        if (!field.All(char.IsAsciiDigit))
        {
            throw new FormatException($"Numeric field at position {_position - length} is not all digits: '{field}'.");
        }

        return long.Parse(field);
    }

    private string Next(int length)
    {
        var field = _raw.Substring(_position, length);
        _position += length;
        return field;
    }
}
