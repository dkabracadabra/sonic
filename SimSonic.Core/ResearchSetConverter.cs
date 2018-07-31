using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimSonic.Core
{
    public class ResearchSetConverter : JsonConverter
    {
        public override bool CanWrite
        {
            get { return false; }
        }
        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IResearchSet);
        }
        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var researchSet = default(IResearchSet);
            if (jsonObject.GetValue("Rect") != null)
                researchSet = new ResearchRect();
            else if (jsonObject.GetValue("Radius") != null)
                researchSet = new CentralRadialResearchSet();
            serializer.Populate(jsonObject.CreateReader(), researchSet);
            return researchSet;
        }
    }
}