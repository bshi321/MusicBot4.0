using System;
using System.Threading.Tasks;
using global::DSharpPlus;
using global::DSharpPlus.CommandsNext;
using Lavalink4NET;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.DependencyInjection;
using TenorSharp;
using DiscordDb.Models;
using DiscordDb.DataAccess;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Entities;
using Lavalink4NET.Rest;
using Lavalink4NET.DSharpPlus;

public class Programs
{
    static void Main(string[] args)
    {

        RunAsync().GetAwaiter().GetResult();
    }
    private static ServiceProvider BuildServiceProvider() => new ServiceCollection()
        .AddSingleton<DiscordClient>()
        .AddSingleton(new DiscordConfiguration
        {
            Token = "API_Key",
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents |DiscordIntents.GuildVoiceStates,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
        })
        .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
        .AddSingleton<IAudioService, LavalinkNode>()
        .AddSingleton(new LavalinkNodeOptions
        {
            RestUri = "http://localhost:2333/",
            WebSocketUri = "ws://localhost:2333/",
            Password = "youshallnotpass",
            
        })
        .AddSingleton<InactivityTrackingOptions>()
        .AddSingleton<InactivityTrackingService>()
        .AddSingleton<TenorClient>()
        .AddSingleton(new TenorConfiguration
        {
            ApiKey = "API_Key",  
        })
        .BuildServiceProvider();
    public static TenorClient tenor = BuildServiceProvider().GetRequiredService<TenorClient>();
    public static Random rand = new Random();
    public static DataAccess db = new DataAccess();
    static async Task RunAsync()
    {

        var provider = BuildServiceProvider();
        var discord = provider.GetRequiredService<DiscordClient>();
        var audioService = provider.GetRequiredService<IAudioService>();
        provider.GetRequiredService<InactivityTrackingService>().BeginTracking();
        discord.UseInteractivity(new InteractivityConfiguration()
        {
            Timeout = TimeSpan.FromSeconds(30),
        });
        var commandNext = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new string[] { "!" },
            Services = provider,
        });

        commandNext.RegisterCommands<MusicCommands>();
        commandNext.RegisterCommands<AdminCommands>();
        
        discord.MessageCreated += async (sender, args) =>
        {
            if(!args.Message.Author.IsBot)
            {
                if (args.Message.Content.ToLower().Equals("hehe"))
                {
                    await args.Channel.SendMessageAsync("https://media.discordapp.net/attachments/903540558499291176/1045201127320334366/VideoCapture_20221118-223939.jpg");
                }
                else if (args.Message.Content.ToLower().Equals("not hehe"))
                {
                    await args.Channel.SendMessageAsync("https://media.discordapp.net/attachments/903540558499291176/1045201127525847070/VideoCapture_20221118-224004.jpg");
                }
                else if (args.Message.Content.ToLower().Equals("f"))
                {
                    await args.Channel.SendMessageAsync("███████╗\n██╔════╝\n█████╗░░\n██╔══╝░░\n██║░░░░░\n╚═╝░░░░░");
                }
                else if (args.Message.Content.ToLower().Equals("hehehe"))
                {
                    await args.Channel.SendMessageAsync("https://tenor.com/view/lizard-laughing-laughinglizard-hehehe-gif-5215392");
                }
                else if (args.Message.Content.ToLower().Equals("hololive"))
                {
                    var gifSearchResult = tenor.Search("hololive", 250, "0").GifResults;
                    var index = rand.Next(0, gifSearchResult.Length);
                    var gif = gifSearchResult[index];
                    await args.Channel.SendMessageAsync(gif.ItemUrl.OriginalString);

                }
                else if (args.Message.Content.ToLower().Equals("rules"))
                {
                    var gifSearchResult = tenor.Search("dragon ball rules", 250, "0").GifResults;
                    var index = rand.Next(0, gifSearchResult.Length);
                    var gif = gifSearchResult[index];
                    await args.Channel.SendMessageAsync(gif.ItemUrl.OriginalString);

                }
                else if (args.Message.Content.ToLower().StartsWith("rule "))
                {
                    var index = args.Message.Content.Split(" ")[1];
                    var gifSearch = 0;
                    if (Int32.TryParse(index, out gifSearch))
                    {
                        var gifSearchResult = tenor.Search("rule " + gifSearch, 250, "0").GifResults;
                        var gif = gifSearchResult[0];
                        await args.Channel.SendMessageAsync(gif.ItemUrl.OriginalString);
                    }

                }
                else if (args.Message.Content.ToLower().Equals("twitch"))
                {
                    await args.Channel.SendMessageAsync("https://www.twitch.tv/isliceurrice");

                }
                else if (args.Message.Content.ToLower().Equals("knee slapper"))
                {
                    await args.Channel.SendMessageAsync("https://tenor.com/view/knee-slap-spongebob-laugh-laughing-gif-15767803");
                }
                else if (args.Message.Content.ToLower().Equals("mag") || args.Message.Content.ToLower().Equals("madge") || args.Message.Content.ToLower().Equals("maggie"))
                {
                    await args.Channel.SendMessageAsync("https://tenor.com/view/anger-getting-on-my-nerves-inside-out-angry-pixar-gif-15286533");
                }
                else if ((args.Message.Content.ToLower().Equals(":0") || args.Message.Content.ToLower().Equals(";0") || args.Message.Content.ToLower().Equals("wtf") || args.Message.Content.ToLower().Equals(">:)")) && args.Author.Id == 388501645769834497)
                {
                    await args.Channel.SendMessageAsync("https://tenor.com/view/anger-getting-on-my-nerves-inside-out-angry-pixar-gif-15286533");
                }
                else if (args.Message.Content.ToLower().Equals("redditor"))
                {
                    await args.Channel.SendMessageAsync("https://media.discordapp.net/attachments/750413774300774422/976927871778033694/oval_darren.gif");
                }
            }
            
        };
        discord.ComponentInteractionCreated += async (sender, args) =>
        {
            if (args.Id.Contains("deletesong"))
            {
                var userId = args.Id.Split("-")[1];

                if (args.User.Id.ToString() == userId)
                {
                    var str = "";
                    var modal = new DiscordInteractionResponseBuilder().WithTitle("Delete a Song")
                    .WithCustomId("deletemodal")
                    .AddComponents(new TextInputComponent(label: "Delete (Use a digit: 1,2,3,4,5,6,7,8,9)", customId: "text", value: str));
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);

                }
            }
            if (args.Id.Contains("addsong"))
            {
                var userId = args.Id.Split("-")[1];

                if (args.User.Id.ToString() == userId)
                {
                    var str = "";
                    var modal = new DiscordInteractionResponseBuilder().WithTitle("Add a Song")
                    .WithCustomId("addmodal")
                    .AddComponents(new TextInputComponent(label: "Add", customId: "text", value: str));
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);

                }

            }
            if (args.Id.Contains("change"))
            {
                var userId = args.Id.Split("-")[1];

                if (args.User.Id.ToString() == userId)
                {
                    var str = "";
                    var modal = new DiscordInteractionResponseBuilder().WithTitle("Change Playlist Title")
                    .WithCustomId("changetitle")
                    .AddComponents(new TextInputComponent(label: "Change your playlist title", customId: "text", value: str));
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);

                }

            }
            if (args.Id == "dropdown" && args.Values[0].StartsWith("option-"))
            {
                var index = args.Values[0].Split("-")[1];
                var userId = args.Values[0].Split("-")[2];
                if (args.User.Id.ToString() == userId)
                {
                    async Task Playlist()
                    {
                        List<UserModel> userlist = await db.FindDiscordUser(args.User.Id);
                        var indexInt = Int32.Parse(index);
                        var sameUser = userlist[0];
                        var playlist = sameUser.Playlist[indexInt].playlists;
                        var des = "";
                        var currentPlaylist = sameUser.Playlist[indexInt];
                        sameUser.currentPlaylist = indexInt;
                        


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
                        var add = "addsong-" + args.Interaction.User.Id;
                        var delete = "deletesong-" + args.Interaction.User.Id;
                        var change = "change-" + args.Interaction.User.Id;
                        var listOfPlaylists = sameUser.Playlist;
                        var options = new List<DiscordSelectComponentOption>();
                        for (int i = 0; i < listOfPlaylists.Count(); i++)
                        {
                            if(sameUser.currentPlaylist == i)
                            {
                                options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + args.User.Id, isDefault: true));
                            }
                            else
                            {
                                options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + args.User.Id));
                            }
                        }
                        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                        {
                            Title = currentPlaylist.title,
                            Description = des,
                        }).AddComponents(new DiscordSelectComponent("dropdown", currentPlaylist.title, options))
                        .AddComponents(new DiscordComponent[]
                        {
                                new DiscordButtonComponent(ButtonStyle.Success, add, "Add Song"),
                                new DiscordButtonComponent(ButtonStyle.Danger, delete, "Remove Song"),
                                new DiscordButtonComponent(ButtonStyle.Primary, change, "Change Title")

                        }));
                        await db.UpdateUser(sameUser);

                    }
                    Playlist();
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                }
            }
        };
        discord.ModalSubmitted += async (sender, args) =>
        {


            if (args.Interaction.Data.CustomId == "changetitle")
            {
                async Task Playlist()
                {
                    List<UserModel> userlist = await db.FindDiscordUser(args.Interaction.User.Id);
                    var sameUser = userlist[0];
                    var playlist = sameUser.Playlist[sameUser.currentPlaylist].playlists;
                    var des = "";
                    var currentPlaylist = sameUser.Playlist[sameUser.currentPlaylist];
                    var values = args.Values["text"];
                    currentPlaylist.title = values;

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
                    var add = "addsong-" + args.Interaction.User.Id;
                    var delete = "deletesong-" + args.Interaction.User.Id;
                    var change = "change-" + args.Interaction.User.Id;
                    var listOfPlaylists = sameUser.Playlist;
                    var options = new List<DiscordSelectComponentOption>();
                    for (int i = 0; i < listOfPlaylists.Count(); i++)
                    {
                        if (sameUser.currentPlaylist == i)
                        {
                            options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + args.Interaction.User.Id, isDefault: true));
                        }
                        else
                        {
                            options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + args.Interaction.User.Id));
                        }
                    }
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = currentPlaylist.title,
                        Description = des,
                    }).AddComponents(new DiscordSelectComponent("dropdown", currentPlaylist.title, options))
                    .AddComponents(new DiscordComponent[]
                    {
                                    new DiscordButtonComponent(ButtonStyle.Success, add, "Add Song"),
                                    new DiscordButtonComponent(ButtonStyle.Danger, delete, "Remove Song"),
                                    new DiscordButtonComponent(ButtonStyle.Primary, change, "Change Title")

                    }));
                    await db.UpdateUser(sameUser);

                }
                Playlist();
                await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            }
            

            if (args.Interaction.Data.CustomId == "addmodal")
            {
                async Task Playlist()
                {
                    List<UserModel> userlist = await db.FindDiscordUser(args.Interaction.User.Id);
                    var modelInteraction = args.Interaction;
                    var values = args.Values["text"];
                    var sameUser = userlist[0];
                    var currentPlaylist = sameUser.Playlist[sameUser.currentPlaylist];
                    var loadResult = audioService.LoadTracksAsync(values, SearchMode.YouTube);
                    if (loadResult.Result.LoadType == TrackLoadType.LoadFailed
                        || loadResult.Result.LoadType == TrackLoadType.NoMatches)
                    {
                        await discord.SendMessageAsync(args.Interaction.Channel, $"Track search failed for {values}.");
                        return;
                    }
                    var track = loadResult.Result.Tracks!.First();
                    SongModel song = new SongModel() { title = track.Title, link = track.Source };
                    currentPlaylist.playlists.Add(song);
                    await db.UpdateUser(sameUser);
                    await discord.SendMessageAsync(args.Interaction.Channel, $"**{track.Title}** has been added to your playlist!");
                    var playlist = currentPlaylist.playlists;
                    var title = "" + args.Interaction.User.Username + "'s Playlist\n\n\n";
                    var des = "";
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
                    var add = "addsong-" + args.Interaction.User.Id;
                    var delete = "deletesong-" + args.Interaction.User.Id;
                    var change = "change-" + args.Interaction.User.Id;
                    var listOfPlaylists = sameUser.Playlist;
                    var options = new List<DiscordSelectComponentOption>();
                    for (int i = 0; i < listOfPlaylists.Count(); i++)
                    {
                        if (sameUser.currentPlaylist == i)
                        {
                            options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + args.Interaction.User.Id, isDefault: true));
                        }
                        else
                        {
                            options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + args.Interaction.User.Id));
                        }
                    }
                    await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = currentPlaylist.title,
                        Description = des,
                    }).AddComponents(new DiscordSelectComponent("dropdown", currentPlaylist.title, options))
                    .AddComponents(new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Success, add, "Add Song"),
                        new DiscordButtonComponent(ButtonStyle.Danger, delete, "Remove Song"),
                        new DiscordButtonComponent(ButtonStyle.Primary, change, "Change Title")
                    }));
                }
                Playlist();
                await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            }
            if (args.Interaction.Data.CustomId == "deletemodal")
            {
                async Task Playlist()
                {
                    var values = args.Values["text"];
                    var isInt = true;
                    for(int i = 0; i < values.Length; i++)
                    {
                        if (((char)values[i] < 48 || (char)values[i] > 57)) isInt = false;
                        else if ((char)values[i] == 48 && i == 0) isInt = false;
                    }
                    if(isInt) 
                    {
                        List<UserModel> userlist = await db.FindDiscordUser(args.Interaction.User.Id);
                        var modelInteraction = args.Interaction;
                    
                        var sameUser = userlist[0];
                        var num = Int32.Parse(values);
                        var currentPlaylist = sameUser.Playlist[sameUser.currentPlaylist];
                        var playlist = currentPlaylist.playlists;
                        var title = "" + args.Interaction.User.Username + "'s Playlist\n\n\n";
                        var des = "";
                        if (playlist.Count() == 0)
                        {
                            des += "**You currently have no songs in your playlist**";
                        }
                        else if(playlist.Count() < num)
                        {
                            des += "**Input number too large**";
                        }
                        else
                        {
                            var titles = currentPlaylist.playlists[num - 1].title;
                            currentPlaylist.playlists.RemoveAt(num - 1);
                            await db.UpdateUser(sameUser);
                            await discord.SendMessageAsync(args.Interaction.Channel, "**" + titles + "** has been removed from your playlist");
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
                            var add = "addsong-" + args.Interaction.User.Id;
                            var delete = "deletesong-" + args.Interaction.User.Id;
                            var change = "change-" + args.Interaction.User.Id;
                            var listOfPlaylists = sameUser.Playlist;
                            var options = new List<DiscordSelectComponentOption>();
                            for (int i = 0; i < listOfPlaylists.Count(); i++)
                            {
                                if (sameUser.currentPlaylist == i)
                                {
                                    options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + args.Interaction.User.Id, isDefault: true));
                                }
                                else
                                {
                                    options.Add(new DiscordSelectComponentOption(listOfPlaylists[i].title, "option-" + (i) + "-" + args.Interaction.User.Id));
                                }
                            }
                            await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                            {
                                Title = currentPlaylist.title,
                                Description = des,
                            }).AddComponents(new DiscordSelectComponent("dropdown", currentPlaylist.title, options))
                            .AddComponents(new DiscordComponent[]
                            {
                                new DiscordButtonComponent(ButtonStyle.Success, add, "Add Song"),
                                new DiscordButtonComponent(ButtonStyle.Danger, delete, "Remove Song"),
                                new DiscordButtonComponent(ButtonStyle.Primary, change, "Change Title")
                            }));
                        }
                        
                    }
                    else
                    {
                        await discord.SendMessageAsync(args.Interaction.Channel, "Input a valid digit");
                    }
                    
                }
                Playlist();
                await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            }
        };
        
        await discord.ConnectAsync();
        await audioService.InitializeAsync();

        await Task.Delay(-1);
    }
}