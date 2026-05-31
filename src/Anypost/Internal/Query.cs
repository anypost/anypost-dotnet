using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Anypost.Internal;

/// <summary>
/// Builds a URL query string, skipping null and empty values. Keys and values are
/// percent-encoded.
/// </summary>
internal sealed class Query
{
    private readonly List<KeyValuePair<string, string>> _pairs = new();

    internal void Add(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _pairs.Add(new KeyValuePair<string, string>(key, value!));
        }
    }

    internal void Add(string key, int? value)
    {
        if (value.HasValue)
        {
            _pairs.Add(new KeyValuePair<string, string>(key, value.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    /// <summary>Returns the encoded query string including a leading <c>?</c>, or "" when empty.</summary>
    internal string Build()
    {
        if (_pairs.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("?");
        for (var i = 0; i < _pairs.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(_pairs[i].Key))
                   .Append('=')
                   .Append(Uri.EscapeDataString(_pairs[i].Value));
        }

        return builder.ToString();
    }
}
