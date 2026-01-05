using System.Text;
using UnityEngine;

public static class TooltipTextUtils
{
    /// <summary>
    /// Inserts line breaks in the text so that no line exceeds maxLineLength characters.
    /// Tries to break at word boundaries; splits long words if necessary.
    /// </summary>
    public static string InsertLineBreaks(string text, int maxLineLength)
    {
        if (string.IsNullOrEmpty(text) || maxLineLength <= 0)
            return text ?? string.Empty;

        var sb = new StringBuilder();
        var words = text.Split(' ');
        int currentLineLength = 0;

        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
            {
                if (currentLineLength > 0)
                {
                    sb.Append(' ');
                    currentLineLength += 1;
                }
                continue;
            }

            if (word.Length > maxLineLength)
            {
                if (currentLineLength > 0)
                {
                    sb.Append('\n');
                    currentLineLength = 0;
                }

                int startIndex = 0;
                while (startIndex < word.Length)
                {
                    int remaining = word.Length - startIndex;
                    int take = Mathf.Min(maxLineLength, remaining);
                    sb.Append(word.Substring(startIndex, take));
                    startIndex += take;
                    if (startIndex < word.Length)
                    {
                        sb.Append('\n');
                    }
                }
                currentLineLength = word.Length - ((word.Length / maxLineLength) * maxLineLength);
                if (currentLineLength == maxLineLength)
                    currentLineLength = 0;
                int lastWordIndex = words.Length - 1;
                if (!word.Equals(words[lastWordIndex]))
                {
                    sb.Append(' ');
                    currentLineLength += 1;
                }
                continue;
            }

            if (currentLineLength == 0)
            {
                sb.Append(word);
                currentLineLength = word.Length;
            }
            else if (currentLineLength + 1 + word.Length <= maxLineLength)
            {
                sb.Append(' ').Append(word);
                currentLineLength += 1 + word.Length;
            }
            else
            {
                sb.Append('\n').Append(word);
                currentLineLength = word.Length;
            }
        }

        return sb.ToString();
    }
}
