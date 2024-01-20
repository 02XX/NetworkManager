using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace MNet
{
    public class Package
    {
        public string type = "MNet.Message";
        public Message message = new Message();
        public Package(string type, Message message)
        {
            this.type = type;
            this.message = message;
        }
        public Package(){}
    }
    public class MessageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Package));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // 创建一个临时的 JObject，然后用其读取实际的 JSON 数据
            var jsonObject = JObject.Load(reader);
            string? type = jsonObject["type"]?.Value<string>();
            if (type == null)
            {
                throw new SerializerException("反序列化失败,type为空");
            }

            Type? messageType = Type.GetType(type);
            if (messageType == null)
            {
                throw new SerializerException($"反序列化失败,无法找到类型 {type}");
            }

            Message? message = jsonObject["message"]?.ToObject(messageType, serializer) as Message;
            if (message == null)
            {
                throw new SerializerException("反序列化失败,message为空");
            }
            // 最后返回一个新创建的Package对象
            return new Package(type, message);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            Package? package = value as Package;
            if (package == null)
            {
                throw new SerializerException("序列化失败,package为空");
            }

            JObject jsonObject = new JObject();

            jsonObject["type"] = package.type;

            if (package.message != null)
            {
                jsonObject["message"] = JToken.FromObject(package.message, serializer);
            }
            else
            {
                throw new SerializerException("序列化失败,message为空");
            }
            jsonObject.WriteTo(writer);
        }
    }
}