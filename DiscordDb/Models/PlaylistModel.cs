using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DiscordDb.Models;

public class PlaylistModel
{
    public string title { get; set; }

    public List<SongModel> playlists { get; set; }
}
