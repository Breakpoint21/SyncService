using System;
using Newtonsoft.Json;

namespace Kyrodan.HiDrive.Serialization
{
    public class TimestampConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var t = (long)reader.Value;
            return DateTimeOffset.FromUnixTimeSeconds(t).DateTime;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var date = (DateTime) value;
            writer.WriteValue(new DateTimeOffset(date).ToUnixTimeSeconds());
        }
    }
}
