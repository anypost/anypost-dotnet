using System.Text;

namespace Anypost.Internal;

internal static class PathUtil
{
    private const string Unreserved =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.~";

    /// <summary>
    /// Percent-encodes a single path segment, escaping everything outside the
    /// RFC 3986 unreserved set (so <c>@</c>, <c>/</c>, <c>*</c>, and the like are
    /// encoded). Matches the path encoding of the other Anypost SDKs.
    /// </summary>
    internal static string Encode(string segment)
    {
        var bytes = Encoding.UTF8.GetBytes(segment);
        var builder = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            var c = (char)b;
            if (b < 0x80 && Unreserved.IndexOf(c) >= 0)
            {
                builder.Append(c);
            }
            else
            {
                builder.Append('%').Append(b.ToString("X2"));
            }
        }

        return builder.ToString();
    }
}
