using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EventAvalon.Database
{
    public abstract class Document : IDocument
    {
        [JsonIgnore]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        public Document()
        {
            Id = ObjectId.GenerateNewId();
        }
    }

}
