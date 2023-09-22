using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordDb.Models;

public class UserModel
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]

    public string Id { get; set; }
    public ulong DiscordId { get; set; }
    public Boolean isPremium { get; set; }
    public List<PlaylistModel> Playlist { get; set; }

    public int currentPlaylist { get; set; }
}
