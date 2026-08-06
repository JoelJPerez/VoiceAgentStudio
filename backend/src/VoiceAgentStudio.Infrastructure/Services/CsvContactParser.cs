using VoiceAgentStudio.Application.Common.Interfaces;

namespace VoiceAgentStudio.Infrastructure.Services;

/// <summary>
/// Simple RFC-4180 CSV parser — no external packages needed.
/// Expected header (case-insensitive): FullName, PhoneNumber, Email, CustomContext
/// </summary>
public class CsvContactParser : ICsvContactParser
{
    public IEnumerable<ParsedContact> Parse(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream, leaveOpen: true);

        var headerLine = reader.ReadLine();
        if (headerLine is null) yield break;

        var headers = ParseCsvLine(headerLine)
            .Select(h => h.ToLowerInvariant().Trim())
            .ToArray();

        int idxName = IndexOf(headers, "fullname", "name");
        int idxPhone = IndexOf(headers, "phonenumber", "phone", "telefono");
        int idxEmail = IndexOf(headers, "email", "correo");
        int idxContext = IndexOf(headers, "customcontext", "context", "contexto");

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseCsvLine(line);

            yield return new ParsedContact
            {
                FullName = GetField(fields, idxName),
                PhoneNumber = GetField(fields, idxPhone),
                Email = GetField(fields, idxEmail),
                CustomContext = GetField(fields, idxContext)
            };
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else current.Append(c);
        }

        fields.Add(current.ToString().Trim());
        return fields.ToArray();
    }

    private static int IndexOf(string[] headers, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var idx = Array.IndexOf(headers, candidate);
            if (idx >= 0) return idx;
        }
        return -1;
    }

    private static string GetField(string[] fields, int index)
        => index >= 0 && index < fields.Length ? fields[index].Trim('"', ' ') : string.Empty;
}
