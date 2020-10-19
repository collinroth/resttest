using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestTest.Banking
{
    /// <summary>
    /// Allow JSON to give us a string when we were expecting a double.  This class will do the
    /// conversion.
    /// </summary>
    internal class DoubleJsonConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string stringValue = reader.GetString();
                if (double.TryParse(stringValue, out double value))
                {
                    return value;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDouble();
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
