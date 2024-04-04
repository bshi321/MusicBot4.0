using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using global::DSharpPlus;
using global::DSharpPlus.CommandsNext;
using global::DSharpPlus.CommandsNext.Attributes;
using global::DSharpPlus.Entities;
using Lavalink4NET;
using DiscordDb.Models;
using DiscordDb.DataAccess;
using DSharpPlus.Interactivity.Extensions;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;
using Lavalink4NET.Clients;
using Microsoft.Extensions.Options;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.Tracks;
using Lavalink4NET.Rest.Entities;
using Lavalink4NET.Rest;
using Amazon.Auth.AccessControlPolicy;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.DependencyInjection;
//using static Microsoft.Extensions.Logging.LogLevel;

public class MusicCommands : BaseCommandModule
{
    public IAudioService AudioService { private get; set; }
    public List<string> queueAuthors = new List<string>();


    public sealed record class TrackData(TrackReference Reference) : ITrackQueueItem
    {
        public string Author;

        public int DashboardId;

        public string Title;

        public StreamProvider Provider;

        public ulong Requester;
        

    };
    public sealed class CustomPlayer : QueuedLavalinkPlayer
    {
        public new TrackData? CurrentItem => (TrackData?)base.CurrentItem;
        public CustomPlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties)
            : base(properties)
        {
           
        }
    }
    public sealed record class CustomPlayerOptions : QueuedLavalinkPlayerOptions
    {
    }

    // Create a player factory
    static ValueTask<CustomPlayer> CreatePlayerAsync(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(properties);

        return ValueTask.FromResult(new CustomPlayer(properties));
    }

    public static DataAccess db = new DataAccess();

    private async ValueTask<CustomPlayer?> GetPlayerAsync(CommandContext ctx, bool connectToVoiceChannel = true)
    {
        var channelBehavior = connectToVoiceChannel
            ? PlayerChannelBehavior.Join
            : PlayerChannelBehavior.None;

        var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior, MemberVoiceStateBehavior.RequireSame);
        var options = new CustomPlayerOptions
        {
            DisconnectOnStop = false, ClearQueueOnStop = false, SelfDeaf = true
        };
        var result = await AudioService.Players
            .RetrieveAsync<CustomPlayer, CustomPlayerOptions>(ctx.Guild.Id, ctx.Member.VoiceState.Channel.Id, CreatePlayerAsync, new OptionsWrapper<CustomPlayerOptions>(options), retrieveOptions)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                _ => "Unknown error.",
            };

            await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
            return null;
        }

        return result.Player;
    }



    [Command]

    public async Task Play(CommandContext ctx)
    {
       // await AudioService.StartAsync();
        var user = ctx.User;
        List<UserModel> userlist = await db.FindDiscordUser(user.Id);

        if (userlist.Count() == 0)
        {
            List<PlaylistModel> list = new List<PlaylistModel>();
            list.Add(new PlaylistModel() { title = "untitled", playlists = new List<SongModel>() });
            await db.CreateUser(new UserModel() { DiscordId = user.Id, isPremium = false, Playlist = list, currentPlaylist = 0 });
            await ctx.RespondAsync("**You have been successfully been added into the database\n\nRetype this command to get access to your playlist**");
        }
        else
        {
            var sameUser = userlist[0];
            
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

            var playlist = sameUser.Playlist[sameUser.currentPlaylist].playlists;
            if (playlist == null || playlist.Count() == 0)
            {
                await ctx.RespondAsync("**You currently have no songs in your playlist**");
                return;
            }
            if(sameUser.isPremium == true)
            {
                await player.Queue.ClearAsync();
                await player.SkipAsync();
                LavalinkTrack track;
                for (int i = 0; i < playlist.Count(); i++)
                {
                    var title = playlist[i].title;
                    var link = playlist[i].link;
                    var loadResult = await AudioService.Tracks.LoadTracksAsync(link, TrackSearchMode.YouTube).ConfigureAwait(false);
                    if (loadResult.IsFailed)
                    {
                        await ctx.RespondAsync($"Track search failed for {title}.");
                        return;
                    }
                    track = loadResult.Track;

                    TrackData data = new TrackData(new TrackReference(track))
                    {
                        Requester = ctx.Member.Id,
                        Provider = track.Provider.Value,
                        Author = track.Author,
                        Title = track.Title
                    };

                    if (player.CurrentTrack == null)
                    {
                        await ctx.RespondAsync($"Now playing **{track.Title}**!");
                        await player.PlayAsync(data).ConfigureAwait(false);
                    }
                    else
                    {
                        await player.PlayAsync(data).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                for (int i = 0; i < playlist.Count(); i++)
                {
                    LavalinkTrack track;
                    var title = playlist[i].title;
                    var link = playlist[i].link;
                    var loadResult = await AudioService.Tracks.LoadTracksAsync(link, TrackSearchMode.YouTube).ConfigureAwait(false);
                    if (loadResult.IsFailed)
                    {
                        await ctx.RespondAsync($"Track search failed for {title}.");
                        return;
                    }
                    track = loadResult.Track;

                    TrackData data = new TrackData(new TrackReference(track))
                    {
                        Requester = ctx.Member.Id,
                        Provider = track.Provider.Value,
                        Author = track.Author,
                        Title = track.Title
                    };

                    if (player.CurrentTrack == null)
                    {
                        await ctx.RespondAsync($"Now playing **{track.Title}**!");
                        await player.PlayAsync(data).ConfigureAwait(false);
                    }
                    else
                    {
                        await player.PlayAsync(data).ConfigureAwait(false);
                    }

                }
            }

        }
        return;
    }

    [Command]
    public async Task Play(CommandContext ctx, [RemainingText] string search)
    {
        Console.WriteLine("PLAYING SONG");
        if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
        {
            await ctx.RespondAsync("You are not in a voice channel.");
            return;
        }
      //  await AudioService.StartAsync();
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;


        var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);


        var loadResult = await AudioService.Tracks.LoadTrackAsync(search, TrackSearchMode.YouTube).ConfigureAwait(false);
        if (loadResult is null)
        {
            await ctx.RespondAsync($"Track search failed for {search}.");
            return;
        }
        var track = loadResult;
        if (player.CurrentTrack == null)
        {
            TrackData data = new TrackData(new TrackReference(track))
            {
                Requester = ctx.Member.Id,
                Provider = track.Provider.Value,
                Author = track.Author,
                Title = track.Title,
            };

            await player.PlayAsync(data).ConfigureAwait(false);
            //  await player.SetVolumeAsync(0.5f, false);
            await ctx.RespondAsync($"Now playing **{track.Title}**!");

        }
        else
        {
            
            TrackData data = new TrackData(new TrackReference(track))
            {
                Requester = ctx.Member.Id,
                Provider = track.Provider.Value,
                Author = track.Author,
                Title = track.Title,
            };
            
            await player.PlayAsync(data).ConfigureAwait(false);
            //  await player.SetVolumeAsync(0.5f, false);
            await ctx.RespondAsync($"**{track.Title}** has been queued!");
            
        }
        

    }
    [Command]
    public async Task Skip(CommandContext ctx)
    {
        if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
        {
            await ctx.RespondAsync("You are not in a voice channel.");
            return;
        }
        if (ctx.Member.Id == 388501645769834497)
        {
            await ctx.RespondAsync("You don't have permission to skip.");
            return;
        }
        //await AudioService.StartAsync();
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);
        if (player.CurrentTrack == null)
         {

             await ctx.RespondAsync("Nothing is playing currently.");
             return;
         }
         else
         {
            var context = (TrackData)player.CurrentItem;
            var id = context.Requester;
            var user = ctx.User;

            List<UserModel> userlist = await db.FindDiscordUser(id);

            if (userlist.Count() > 0)
            {
                var sameUser = userlist[0];
                if ( sameUser.isPremium == true && sameUser.DiscordId != ctx.Member.Id)
                {
                    await ctx.RespondAsync($"You cannot skip a **Premium Member's song**");
                    return;
                }
            }

            
             var track = player.CurrentTrack.Title;
             
             await player.SkipAsync();
             await ctx.RespondAsync($"**{track}** has been skipped");

             await Task.Delay(500);
           
             

         }
         
      
         
    }

    [Command]
    public async Task Queue(CommandContext ctx)
    {
        if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
        {
            await ctx.RespondAsync("You are not in a voice channel.");
            return;
        }
      //  await AudioService.StartAsync();
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);
    /*    if (player is null)
        {
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "I am not connected to a voice channel"
            }).SendAsync(ctx.Channel);
            return;
        }*/


        if (player.Queue.IsEmpty)
        {
            var des = "";
            if (player.CurrentTrack != null)
            {
                var currrentTrackContext = (TrackData)player.CurrentItem!;
                var currentTrack = await ctx.Client.GetUserAsync(currrentTrackContext.Requester);
                des = "**Currently playing: " + player.CurrentTrack.Title + ", by " + currentTrack.Username + "**" + "\n\n";
            }
            des += "There is nothing queued currently";
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                
                Title = des
            }).SendAsync(ctx.Channel);
            return;
        }
        var count = player.Queue.Count;
        var str = "";
        if (!(player.CurrentTrack == null))
        {
            var currrentTrackContext = (TrackData)player.CurrentItem!;
            var currentTrack = await ctx.Client.GetUserAsync(currrentTrackContext.Requester);
            str = "**Currently playing: " + player.CurrentTrack.Title + ", by " + currentTrack.Username + "**" + "\n\n";
        }
        for (int i = 0; i < count; i++)
        {
            var context = (TrackData)player.Queue[i]!;
            var user = await ctx.Client.GetUserAsync(context.Requester);
            str = str + (i + 1) + ") " + context.Title + ", **by " + user.Username + "**" + "\n\n";
        }
        var embed = new DiscordEmbedBuilder()
        {
            Title = "Queue",
            Description = str,
        };
        var msg = await new DiscordMessageBuilder().AddEmbed(embed).SendAsync(ctx.Channel);
        return;
    }

    [Command]
    public async Task Remove(CommandContext ctx)
    {
        if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
        {
            await ctx.RespondAsync("You are not in a voice channel.");
            return;
        }
       // await AudioService.StartAsync();
        /*if (ctx.Member.Id == 388501645769834497)
        {
            await ctx.RespondAsync("You are Maggie. You can't remove songs in the queue.");
            return;
        }*/
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);
       /* if (player is null)
        {
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "I am not connected to a voice channel"
            }).SendAsync(ctx.Channel);
            return;
        }*/


        if (player.Queue.IsEmpty)
        {
            var des = "";
            if (player.CurrentTrack != null)
            {
                var currrentTrackContext = (TrackData)player.CurrentItem!;
                var currentTrack = await ctx.Client.GetUserAsync(currrentTrackContext.Requester);
                des = "**Currently playing: " + player.CurrentTrack.Title + ", by " + currentTrack.Username + "**" + "\n\n";
            }
            des += "There is nothing queued currently";
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {

                Title = des
            }).SendAsync(ctx.Channel);
            return;
        }

        var count = player.Queue.Count;
        var str = "";
        if(!(player.CurrentTrack == null))
        {
            var currrentTrackContext = (TrackData)player.CurrentItem!;
            var currentTrack = await ctx.Client.GetUserAsync(currrentTrackContext.Requester);
            str = "**Currently playing: " + player.CurrentTrack.Title + ", by " + currentTrack.Username + "**" + "\n\n";
        }
        
        for (int i = 0; i < count; i++)
        {
            var context = (TrackData)player.Queue[i]!;
            var user = await ctx.Client.GetUserAsync(context.Requester);
            str = str + (i + 1) + ") " + context.Title + ", **by " + user.Username + "**" + "\n\n";
        }
        var embed = new DiscordEmbedBuilder()
        {
            Title = "Queue",
            Description = str,
        }.AddField("\n\u200b", "Select a number and type \"!remove [number]\" to remove a song from the queue!", true);
        var msg = await new DiscordMessageBuilder().AddEmbed(embed).SendAsync(ctx.Channel);
        return;
    }

    [Command]
    public async Task Remove(CommandContext ctx, [RemainingText] int num)
    {
        if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
        {
            await ctx.RespondAsync("You are not in a voice channel.");
            return;
        }
     //   await AudioService.StartAsync();
        /*if (ctx.Member.Id == 388501645769834497)
        {
            await ctx.RespondAsync("You are Maggie. You can't remove songs in the queue.");
            return;
        }*/
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);
        if (player is null)
        {
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "I am not connected to a voice channel"
            }).SendAsync(ctx.Channel);
            return;
        }


        if (player.Queue.IsEmpty)
        {
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "There is nothing queued currently"
            }).SendAsync(ctx.Channel);
            return;
        }
        var context = (TrackData)player.CurrentItem;
        var id = context.Requester;
        var user = ctx.User;

        List<UserModel> userlist = await db.FindDiscordUser(id);

        if (userlist.Count() > 0)
        {
            var sameUser = userlist[0];
            if (sameUser.isPremium == true && sameUser.DiscordId != ctx.Member.Id)
            {
                await ctx.RespondAsync($"You cannot skip a **Premium Member's song**");
                return;
            }
        }
        
        var index = num - 1;
        var track = (TrackData)player.Queue[index];
        await player.Queue.RemoveAtAsync(index);
        await ctx.RespondAsync($"Removed **{track.Title}** from the queue!");
        return;
    }

    [Command]

    public async Task disconnect(CommandContext ctx)
    {
     //   await AudioService.StartAsync();
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);
        await player.DisconnectAsync();
        if (ctx.Member.Id == 237384813189922816 && player is not null)
        {
            await player.DisconnectAsync();
        }
        return;
    }
    
    [Command]

    public async Task Playlist(CommandContext ctx)
    {
      //  await AudioService.StartAsync();
        var user = ctx.User;

        List<UserModel> userlist = await db.FindDiscordUser(user.Id);
        if (userlist.Count() == 0)
        {
            List<PlaylistModel> list = new List<PlaylistModel>();
            list.Add(new PlaylistModel() { title = "untitled", playlists = new List<SongModel>() });
            await db.CreateUser(new UserModel() { DiscordId = user.Id, isPremium = false, Playlist = list, currentPlaylist = 0 });
            await ctx.RespondAsync("**You have been successfully been added into the database\n\nRetype this command to get access to your playlist**");
        }
        else
        {
    
            var sameUser = userlist[0];

            var des = "";
            var currentPlaylist = sameUser.Playlist[sameUser.currentPlaylist];
            var playlist = currentPlaylist.playlists;
            if (playlist.Count() == 0)
            {
                des += "**You currently have no songs in your playlist**";
            }
            else
            {
                for (int i = 0; i < playlist.Count(); i++)
                {
                    des = des + (i + 1) + ") " + playlist[i].title + "\n\n";
                }
            }
            var add = "addsong-" + user.Id;
            var delete = "deletesong-" + user.Id;
            var change = "change-" + user.Id;
            var create = "create-" + user.Id;
            var listOfPlaylists = sameUser.Playlist;
            var options = new List<DiscordSelectComponentOption>();
            for (int i = 0; i < listOfPlaylists.Count(); i++)
            {
                if (sameUser.currentPlaylist == i)
                {
                    options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + ctx.User.Id, isDefault: true));
                }
                else
                {
                    options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + ctx.User.Id));
                }
            }
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = currentPlaylist.title,
                Description = des,
            }).AddComponents(new DiscordSelectComponent("dropdown", currentPlaylist.title, options))
                .AddComponents(new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Success, add, "Add Song"),
                        new DiscordButtonComponent(ButtonStyle.Danger, delete, "Remove Song"),
                        new DiscordButtonComponent(ButtonStyle.Primary, change, "Change Title"),
                        new DiscordButtonComponent(ButtonStyle.Secondary, create, "Create Playlist")
                    }).SendAsync(ctx.Channel);

        }
        return;
    }

    [Command]

    public async Task Premium(CommandContext ctx)
    {
        if (ctx.Member.Id == 237384813189922816)
        {
            var user = ctx.User;

            List<UserModel> userlist = await db.FindDiscordUser(user.Id);

            if (userlist.Count() == 0)
            {
                List<PlaylistModel> list = new List<PlaylistModel>();
                list.Add(new PlaylistModel() { title = "untitled", playlists = new List<SongModel>() });
                await db.CreateUser(new UserModel() { DiscordId = user.Id, isPremium = true, Playlist = list, currentPlaylist = 0 });
                await ctx.RespondAsync("**You have been successfully been added into the database as a Premium Member**");
            }
        }
        else
        {
            var mainuser = ctx.Client.GetUserAsync(237384813189922816);
            await ctx.RespondAsync($"Only {mainuser.Result.Mention} can use this command.");
        }

        return;
    }

    [Command]

    public async Task Premium(CommandContext ctx, DiscordMember user)
    {
        if (ctx.Member.Id == 237384813189922816)
        {

            List<UserModel> userlist = await db.FindDiscordUser(user.Id);

            if (userlist.Count() == 0)
            {
                List<PlaylistModel> list = new List<PlaylistModel>();
                list.Add(new PlaylistModel() { title = "untitled", playlists = new List<SongModel>() });
                await db.CreateUser(new UserModel() { DiscordId = user.Id, isPremium = true, Playlist = list, currentPlaylist = 0 });
                await ctx.RespondAsync("**" + user.Mention + " has been successfully been added into the database as a Premium Member**");
            }
            else
            {
                var sameUser = userlist[0];
                var boolean = !sameUser.isPremium;
                sameUser.isPremium = boolean;
                db.UpdateUser(sameUser); 
                if(boolean)
                {
                    await ctx.RespondAsync("**" + user.Mention + " has been successfully added to Premium Membership**");
                }
                else
                {
                    await ctx.RespondAsync("**" + user.Mention + " has been successfully removed from Premium Membership**");
                }
            }
        }
        else
        {
            var mainuser = ctx.Client.GetUserAsync(237384813189922816);
            await ctx.RespondAsync($"Only {mainuser.Result.Mention} can use this command.");
        }
        return;
    }

    [Command]

    public async Task Update(CommandContext ctx)
    {
        if(ctx.Member.Id == 237384813189922816)
        {
            var userlist = db.GetAllUsers().Result;
            for(int i = 0; i < userlist.Count(); i++)
            {
                var user = userlist[i];
                List<PlaylistModel> list = new List<PlaylistModel>();
                list.Add(new PlaylistModel() { title = "untitled", playlists = new List<SongModel>() });
                user = new UserModel() { Id = user.Id, isPremium = false, Playlist = list, currentPlaylist = 0, DiscordId = user.DiscordId };
                await db.UpdateUser(user);
                
            }
            await ctx.RespondAsync("The database has been updated");
        }
        else
        {
            var mainuser = ctx.Client.GetUserAsync(237384813189922816);
            await ctx.RespondAsync($"Only {mainuser.Result.Mention} can use this command.");
        }
    }



}

