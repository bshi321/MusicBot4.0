using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using global::DSharpPlus;
using global::DSharpPlus.Net;
using global::DSharpPlus.Net.WebSocket;
using global::DSharpPlus.CommandsNext;
using global::DSharpPlus.CommandsNext.Attributes;
using global::DSharpPlus.Entities;
using Lavalink4NET;
using Lavalink4NET.Decoding;
using Lavalink4NET.Events;
using Lavalink4NET.Filters;
using Lavalink4NET.Payloads;
using Lavalink4NET.Lyrics;
using Lavalink4NET.Tracking;
using Lavalink4NET.Statistics;
using Lavalink4NET.Cluster;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Lavalink4NET.DSharpPlus;
using Lavalink4NET.Logging;
using DiscordDb.Models;
using DiscordDb.DataAccess;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.Enums;

//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.DependencyInjection;
//using static Microsoft.Extensions.Logging.LogLevel;

public class MusicCommands : BaseCommandModule
{
    public IAudioService AudioService { private get; set; }
    public List<string> queueAuthors = new List<string>();
    public sealed class TrackContext {
        public ulong RequesterId { get; set; }

        public string OriginalQuery { get; set; }

    };
    public static DataAccess db = new DataAccess();
    static Dictionary<DiscordGuild, Queue<LavalinkTrack>> serverQueue = new Dictionary<DiscordGuild, Queue<LavalinkTrack>>();

    [Command]

    public async Task Play(CommandContext ctx)
    {
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
            var guildId = ctx.Guild.Id;
            var voiceChannelId = ctx.Member.VoiceState.Channel.Id;

            var player = AudioService.GetPlayer<QueuedLavalinkPlayer>(guildId);
            if (player is null)
            {
                player = await AudioService.JoinAsync<QueuedLavalinkPlayer>(guildId, voiceChannelId);
            }

            var playlist = sameUser.Playlist[sameUser.currentPlaylist].playlists;
            if (playlist == null || playlist.Count() == 0)
            {
                await ctx.RespondAsync("**You currently have no songs in your playlist**");
                return;
            }
            if(sameUser.isPremium == true)
            {
                player.Queue.Clear();
                await player.SkipAsync();
                LavalinkTrack track;
                for (int i = 0; i < playlist.Count(); i++)
                {
                    var title = playlist[i].title;
                    var link = playlist[i].link;
                    var loadResult = AudioService.LoadTracksAsync(link, SearchMode.YouTube);
                    if (loadResult.Result.LoadType == TrackLoadType.LoadFailed
                        || loadResult.Result.LoadType == TrackLoadType.NoMatches)
                    {
                        await ctx.RespondAsync($"Track search failed for {title}.");
                        return;
                    }
                    track = loadResult.Result.Tracks!.First();

                    track.Context = new TrackContext
                    {

                        RequesterId = ctx.Member.Id,
                    };
                    if (player.CurrentTrack == null)
                    {
                        await ctx.RespondAsync($"Now playing **{track.Title}**!");
                        await player.PlayTopAsync(track);
                    }
                    else
                    {
                        player.Queue.Add(track);
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
                    var loadResult = AudioService.LoadTracksAsync(link, SearchMode.YouTube);
                    if (loadResult.Result.LoadType == TrackLoadType.LoadFailed
                        || loadResult.Result.LoadType == TrackLoadType.NoMatches)
                    {
                        await ctx.RespondAsync($"Track search failed for {title}.");
                        return;
                    }
                    track = loadResult.Result.Tracks!.First();

                    track.Context = new TrackContext
                    {

                        RequesterId = ctx.Member.Id,
                    };

                    if (player.CurrentTrack == null)
                    {
                        await ctx.RespondAsync($"Now playing **{track.Title}**!");
                        await player.PlayTopAsync(track);
                    }
                    else
                    {
                        player.Queue.Add(track);
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
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;


        var player = AudioService.GetPlayer<QueuedLavalinkPlayer>(guildId);
        if(player is null)
        {
            player = await AudioService.JoinAsync<QueuedLavalinkPlayer>(guildId, voiceChannelId);
        }
        
        
        var loadResult = AudioService.LoadTracksAsync(search, SearchMode.YouTube);
        if(loadResult.Result.LoadType == TrackLoadType.LoadFailed
            || loadResult.Result.LoadType == TrackLoadType.NoMatches)
        {
            await ctx.RespondAsync($"Track search failed for {search}.");
            return;
        }
        var track = loadResult.Result.Tracks.First();
        if (player.CurrentTrack == null)
        {
            await player.PlayTopAsync(track);
            //  await player.SetVolumeAsync(0.5f, false);
            await ctx.RespondAsync($"Now playing **{track.Title}**!");
            track.Context = new TrackContext
            {
                OriginalQuery = search,
                RequesterId = ctx.Member.Id,
            };
        }
        else
        {
            await player.PlayAsync(track);
            //  await player.SetVolumeAsync(0.5f, false);
            await ctx.RespondAsync($"**{track.Title}** has been queued!");
            track.Context = new TrackContext
            {
                OriginalQuery = search,
                RequesterId = ctx.Member.Id,
            };
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
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = AudioService.GetPlayer<QueuedLavalinkPlayer>(guildId);
        if (player is null)
         {
             await ctx.RespondAsync("I am not connected to a voice channel in this server.");

             return;
         }
         if (player.CurrentTrack == null)
         {

             await ctx.RespondAsync("Nothing is playing currently.");
             return;
         }
         else
         {
            var context = (TrackContext)player.CurrentTrack.Context;
            var id = context.RequesterId;
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
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = AudioService.GetPlayer<QueuedLavalinkPlayer>(guildId);
        if(player is null)
        {
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "I am not connected to a voice channel"
            }).SendAsync(ctx.Channel);
            return;
        }


        if (player.Queue.IsEmpty)
        {
            var des = "";
            if (player.CurrentTrack != null)
            {
                var currrentTrackContext = (TrackContext)player.CurrentTrack.Context!;
                var currentTrack = await ctx.Client.GetUserAsync(currrentTrackContext.RequesterId);
                des = "**Currently playing: " + player.CurrentTrack.Title + ", by " + currentTrack.Username + "**" + "\n\n";
            }
            des += "There is nothing queued currently";
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                
                Title = des
            }).SendAsync(ctx.Channel);
            return;
        }
        var count = player.Queue.Tracks.Count;
        var str = "";
        if (!(player.CurrentTrack == null))
        {
            var currrentTrackContext = (TrackContext)player.CurrentTrack.Context!;
            var currentTrack = await ctx.Client.GetUserAsync(currrentTrackContext.RequesterId);
            str = "**Currently playing: " + player.CurrentTrack.Title + ", by " + currentTrack.Username + "**" + "\n\n";
        }
        for (int i = 0; i < count; i++)
        {
            var context = (TrackContext)player.Queue[i].Context!;
            var user = await ctx.Client.GetUserAsync(context.RequesterId);
            str = str + (i + 1) + ") " + player.Queue[i].Title + ", **by " + user.Username + "**" + "\n\n";
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
        /*if (ctx.Member.Id == 388501645769834497)
        {
            await ctx.RespondAsync("You are Maggie. You can't remove songs in the queue.");
            return;
        }*/
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = AudioService.GetPlayer<QueuedLavalinkPlayer>(guildId);
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
            var des = "";
            if (player.CurrentTrack != null)
            {
                var currrentTrackContext = (TrackContext)player.CurrentTrack.Context!;
                var currentTrack = await ctx.Client.GetUserAsync(currrentTrackContext.RequesterId);
                des = "**Currently playing: " + player.CurrentTrack.Title + ", by " + currentTrack.Username + "**" + "\n\n";
            }
            des += "There is nothing queued currently";
            var msg2 = await new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            {

                Title = des
            }).SendAsync(ctx.Channel);
            return;
        }

        var count = player.Queue.Tracks.Count;
        var str = "";
        if(!(player.CurrentTrack == null))
        {
            var currrentTrackContext = (TrackContext)player.CurrentTrack.Context!;
            var currentTrack = await ctx.Client.GetUserAsync(currrentTrackContext.RequesterId);
            str = "**Currently playing: " + player.CurrentTrack.Title + ", by " + currentTrack.Username + "**" + "\n\n";
        }
        
        for (int i = 0; i < count; i++)
        {
            var context = (TrackContext)player.Queue[i].Context!;
            var user = await ctx.Client.GetUserAsync(context.RequesterId);
            str = str + (i + 1) + ") " + player.Queue[i].Title + ", **by " + user.Username + "**" + "\n\n";
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
        /*if (ctx.Member.Id == 388501645769834497)
        {
            await ctx.RespondAsync("You are Maggie. You can't remove songs in the queue.");
            return;
        }*/
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = AudioService.GetPlayer<QueuedLavalinkPlayer>(guildId);
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
        var context = (TrackContext)player.CurrentTrack.Context;
        var id = context.RequesterId;
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
        var track = player.Queue[index];
        player.Queue.RemoveAt(index);
        await ctx.RespondAsync($"Removed **{track.Title}** from the queue!");
        return;
    }

    [Command]

    public async Task disconnect(CommandContext ctx)
    {
        var guildId = ctx.Guild.Id;
        var voiceChannelId = ctx.Member.VoiceState.Channel.Id;
        var player = AudioService.GetPlayer<QueuedLavalinkPlayer>(guildId);
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
        var user = ctx.User;
        ctx.RespondAsync("apples");
        List<UserModel> userlist = await db.FindDiscordUser(user.Id);
        ctx.RespondAsync("apples2");
        if (userlist.Count() == 0)
        {
            List<PlaylistModel> list = new List<PlaylistModel>();
            list.Add(new PlaylistModel() { title = "untitled", playlists = new List<SongModel>() });
            await db.CreateUser(new UserModel() { DiscordId = user.Id, isPremium = false, Playlist = list, currentPlaylist = 0 });
            await ctx.RespondAsync("**You have been successfully been added into the database\n\nRetype this command to get access to your playlist**");
        }
        else
        {
            var interactivity = ctx.Client.GetInteractivity();

            await ctx.RespondAsync("Respond with '**;add**' to create a new playlist\nOtherwise, respond with '**;view**' to view your playlists");
            var result = await ctx.Message.GetNextMessageAsync();
            if(!result.TimedOut)
            {
                var sameUser = userlist[0];
                if (result.Result.Content == ";add")
                {
                    sameUser.Playlist.Add(new PlaylistModel { title = "untitled", playlists = new List<SongModel>() });
                    db.UpdateUser(sameUser);
                    await ctx.RespondAsync("An untitled playlist has been added");
                }
                else if(result.Result.Content == ";view")
                {

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
                                new DiscordButtonComponent(ButtonStyle.Primary, change, "Change Title")
                            }).SendAsync(ctx.Channel);
                    async Task Check()
                    {

                    }
                    
                }
            }
            else
            {
                await ctx.RespondAsync("You did not respond in time");
            }










            
            
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

