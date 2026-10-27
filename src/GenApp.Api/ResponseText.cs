namespace GenApp.Api;

// Texts in responses are trimmed (ADR 0002 decision 6) and an empty text is returned as "", never null
// (docs/api-design.md section 1).
internal static class ResponseText
{
    public static string Of(string? value) => value?.TrimEnd() ?? "";
}
