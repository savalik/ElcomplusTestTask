using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace NetworkProtocol
{
    public class ItemConverter : Newtonsoft.Json.Converters.CustomCreationConverter<IData>
    {
        public override IData Create(Type objectType)
        {
            throw new NotImplementedException();
        }

        public IData Create(Type objectType, JObject jObject)
        {
            var type = (string)jObject.Property("DataType");

            switch (type)
            {
                case nameof(Event):
                    return new Event();
                case nameof(Message):
                    return new Message();
                case nameof(UserList):
                    return new UserList();
            }

            throw new ApplicationException(string.Format("The data type {0} is not supported!", type));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load JObject from stream 
            JObject jObject = JObject.Load(reader);

            // Create target object based on JObject 
            var target = Create(objectType, jObject);

            // Populate the object properties 
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }
    }
}
