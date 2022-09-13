/*
 * Json相关的成员
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace System.Text.Json.Serialization
{

    /// <summary>
    /// 用于将Guid类型Base64编码的Json转换器。
    /// </summary>
    public class GuidJsonConverter : JsonConverter<Guid>
    {
        public GuidJsonConverter()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="reader"><inheritdoc/></param>
        /// <param name="typeToConvert"><inheritdoc/></param>
        /// <param name="options"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TryGetBytesFromBase64(out var obj) ? new Guid(obj) : Guid.Empty;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="writer"><inheritdoc/></param>
        /// <param name="value"><inheritdoc/></param>
        /// <param name="options"><inheritdoc/></param>
        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) =>
            writer.WriteBase64StringValue(value.ToByteArray());
    }

    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("G"));
        }
    }
}
