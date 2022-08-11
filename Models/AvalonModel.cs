using EventAvalon.Database;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventAvalon.Models
{
    [BsonCollection("avalons")]
    public class AvalonModel : Document
    {
        [BsonElement("discordId")]
        public ulong DiscordId { get; set; }

        [BsonElement("commandChannelId")]
        public ulong CommandChannelId { get; set; }

        [BsonElement("managers")]
        public IList<ulong> Managers { get; set; }

        [BsonElement("avalons")]
        public IList<Avalon> Avalons { get; set; }
    }

    public class Avalon
    {
        [BsonElement("channelId")]
        public ulong ChannelId { get; set; }

        [BsonElement("messageId")]
        public ulong MessageId { get; set; }

        [BsonElement("manager")]
        public ulong Manager { get; set; }

        [BsonElement("lastId")]
        public long LastId { get; set; }

        [BsonElement("info")]
        public string Info { get; set; }

        [BsonElement("isPaused")]
        public bool IsPaused { get; set; }

        [BsonElement("isStopped")]
        public bool IsStopped { get; set; }

        [BsonElement("data")]
        public IList<AvalonData> Data { get; set; }

        [BsonElement("members")]
        public IList<AvalonMember> Members { get; set; }
    }

    public class AvalonData
    {
        [BsonElement("role")]
        public string Role { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("slug")]
        public string Slug { get; set; }

        [BsonElement("limit")]
        public int Limit { get; set; }
    }

    public class AvalonMember
    {
        [BsonElement("id")]
        public long Id { get; set; }

        [BsonElement("role")]
        public string Role { get; set; }

        [BsonElement("userId")]
        public ulong UserId { get; set; }

        [BsonElement("nickname")]
        public string Nickname { get; set; }
    }
}
