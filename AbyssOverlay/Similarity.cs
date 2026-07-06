using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AbyssOverlay;

public static class Similarity
{
    private static readonly Regex NonWord = new("[^\\w\\s/+-]+", RegexOptions.Compiled);
    private static readonly Regex MultiSpace = new("\\s+", RegexOptions.Compiled);

    public static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return string.Empty;
        }

        var t = s.ToLowerInvariant().Replace("ё", "е");
        t = NonWord.Replace(t, " ");
        t = MultiSpace.Replace(t, " ").Trim();
        return t;
    }

    public static int Best(string target, string text)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (text.Contains(target, StringComparison.Ordinal))
        {
            return 100;
        }

        var r1 = TokenSetRatio(target, text);
        var r2 = PartialRatio(target, text);
        return Math.Max(r1, r2);
    }

    public static int TokenSetRatio(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return 0;
        }

        var aTokens = new HashSet<string>(a.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var bTokens = new HashSet<string>(b.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var inter = aTokens.Intersect(bTokens).ToList();
        if (inter.Count == 0)
        {
            return Ratio(a, b);
        }

        var interS = string.Join(' ', inter.OrderBy(x => x));
        var aRem = string.Join(' ', aTokens.Except(inter).OrderBy(x => x));
        var bRem = string.Join(' ', bTokens.Except(inter).OrderBy(x => x));
        var comboA = (interS + " " + aRem).Trim();
        var comboB = (interS + " " + bRem).Trim();
        return Math.Max(Ratio(interS, comboA), Math.Max(Ratio(interS, comboB), Ratio(comboA, comboB)));
    }

    public static int PartialRatio(string shorter, string longer)
    {
        if (string.IsNullOrWhiteSpace(shorter) || string.IsNullOrWhiteSpace(longer))
        {
            return 0;
        }

        if (shorter.Length > longer.Length)
        {
            (shorter, longer) = (longer, shorter);
        }

        if (longer.Contains(shorter, StringComparison.Ordinal))
        {
            return 100;
        }

        var best = 0;
        var sLen = shorter.Length;
        for (var i = 0; i <= longer.Length - sLen; i++)
        {
            var window = longer.Substring(i, sLen);
            var r = Ratio(shorter, window);
            if (r > best)
            {
                best = r;
                if (best >= 99)
                {
                    return best;
                }
            }
        }

        return best;
    }

    public static int Ratio(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return 0;
        }

        var la = a.Length;
        var lb = b.Length;
        var d = new int[la + 1, lb + 1];
        for (var i = 0; i <= la; i++) d[i, 0] = i;
        for (var j = 0; j <= lb; j++) d[0, j] = j;
        for (var i = 1; i <= la; i++)
        {
            for (var j = 1; j <= lb; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        var dist = d[la, lb];
        var maxLen = Math.Max(la, lb);
        if (maxLen == 0) return 100;
        var ratio = (1.0 - (double)dist / maxLen) * 100.0;
        return (int)Math.Round(ratio, MidpointRounding.AwayFromZero);
    }
}
