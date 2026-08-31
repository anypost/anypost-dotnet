using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anypost.Models;

namespace Anypost.Internal;

/// <summary>
/// Writes <see cref="Attachment"/> in the wire shape, where the content is always
/// a single base64 <c>content</c> field. The model carries two mutually exclusive
/// content properties — raw bytes, encoded here, or an already-encoded string,
/// written through verbatim — and <see cref="Attachment.WireContent"/> resolves
/// which one applies.
///
/// Attachments are request-only, so this converter never reads.
/// </summary>
internal sealed class AttachmentConverter : JsonConverter<Attachment>
{
    public override Attachment Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        throw new NotSupportedException("Attachment is request-only and is never deserialized.");

    public override void Write(Utf8JsonWriter writer, Attachment value, JsonSerializerOptions options)
    {
        // Resolved before the first write so an invalid attachment throws
        // without leaving a half-written object on the writer.
        var content = value.WireContent();

        writer.WriteStartObject();
        writer.WriteString("filename", value.Filename);
        writer.WriteString("content", content);
        if (value.ContentType is not null)
        {
            writer.WriteString("content_type", value.ContentType);
        }
        if (value.ContentId is not null)
        {
            writer.WriteString("content_id", value.ContentId);
        }
        writer.WriteEndObject();
    }
}
