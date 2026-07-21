using System.Globalization;
using System.Text;

namespace AoraCare.Domain.Common;

public static class SlugHelper
{
    public static string CreateSlug(string name) =>
        RemoveDiacritics(name.Trim()).ToLower().Replace(' ', '-');

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
