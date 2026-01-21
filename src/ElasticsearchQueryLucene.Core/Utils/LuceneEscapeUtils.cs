using System.Text.RegularExpressions;

namespace ElasticsearchQueryLucene.Core.Utils;

public static class LuceneEscapeUtils
{
    private static readonly string[] SpecialChars = { "+", "-", "&&", "||", "!", "(", ")", "{", "}", "[", "]", "^", "\"", "~", "*", "?", ":", "\\", "/" };

    public static string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var escapedValue = value;
        // Escape backslash first
        escapedValue = escapedValue.Replace("\\", "\\\\");
        
        foreach (var c in SpecialChars)
        {
            if (c == "\\") continue; 
            escapedValue = escapedValue.Replace(c, "\\" + c);
        }

        return escapedValue;
    }

    public static string EscapeField(string field)
    {
        return field; // Usually fields don't need escaping in standard DSL, but can be added if needed
    }
}
