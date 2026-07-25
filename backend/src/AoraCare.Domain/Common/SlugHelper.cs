using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AoraCare.Domain.Common;

public static class SlugHelper
{
    public static string CreateSlug(string name)
    {
        name = Regex.Replace(name, @"[\W_]", " ");
        name = Regex.Replace(name, @"\s+", " ");
        name = name.Trim();
        return RemoveDiacritics(name).ToLowerInvariant().Replace(' ', '-');
    }

    private static string RemoveDiacritics(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
