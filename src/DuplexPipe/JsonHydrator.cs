namespace DuplexPipe
{
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;

    public sealed class JsonHydrator : IHydrator
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };
        private static readonly JsonSerializer Serializer = JsonSerializer.Create(JsonSettings);

        public Stream Dehydrate<T>(T payload)
        {
            MemoryStream ms = new MemoryStream();
            using (BsonWriter writer = new BsonWriter(ms))
            {
                Serializer.Serialize(writer, payload, typeof(T));
            }
            ms.Seek(0, SeekOrigin.Begin);

            return ms;
        }

        public object Hydrate(Stream stream)
        {
            object o;
            using (BsonReader reader = new BsonReader(stream))
            {
                o = Serializer.Deserialize(reader);
            }

            return o;
        }
    }
}