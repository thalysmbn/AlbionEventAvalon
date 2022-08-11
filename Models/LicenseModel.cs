using EventAvalon.Database;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventAvalon.Models
{
    [BsonCollection("licenses")]
    public class LicenseModel : Document
    {
        [BsonElement("discordId")]
        public ulong DiscordId { get; set; }

        [BsonElement("adminId")]
        public ulong AdminId { get; set; }

        [BsonElement("expireAt")]
        public DateTime ExpireAt { get; set; }

        public bool IsValid => ExpireAt >= DateTime.Now;
    }
}
