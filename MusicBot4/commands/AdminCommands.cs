using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Net;


public class AdminCommands : BaseCommandModule
{
    /*[Command]*/
    public async Task Role(CommandContext ctx, DiscordRole role)
    {
        if (ctx.User.Id == 237384813189922816)
        {
           var pos = ctx.Guild.Roles.Values.ElementAt(0).Position;
            await role.ModifyPositionAsync(pos);
            return;
        }
        else
        {
            return;
        }
    }

    /*[Command]*/
    public async Task CreateRole(CommandContext ctx, [RemainingText] string name)
    {
        if (ctx.User.Id == 237384813189922816)
        {
            var newrole = await ctx.Guild.CreateRoleAsync(name, Permissions.Administrator, DiscordColor.Yellow, hoist: true, mentionable: true);
            await ctx.RespondAsync($"Created {newrole.Mention} role");
            await ctx.Member.GrantRoleAsync(newrole);
            await ctx.RespondAsync($"Granted {ctx.Member.DisplayName} the {newrole.Mention} role");
            return;

        }
        else
        {
            return;
        }
    }

    /*[Command]*/
    public async Task CreateRole2(CommandContext ctx, string name, DiscordMember user)
    {
        if (ctx.User.Id == 237384813189922816)
        {
            var newrole = await ctx.Guild.CreateRoleAsync(name, hoist: true, mentionable: true);
            await ctx.RespondAsync($"Created {newrole.Mention} role");
            await user.GrantRoleAsync(newrole);
            await ctx.RespondAsync($"Granted {user.DisplayName} the {newrole.Mention} role");
            return;

        }
        else
        {
            return;
        }
    }

    /*[Command]*/
    public async Task RemoveRole(CommandContext ctx, DiscordRole role)
    {
        if (ctx.User.Id == 237384813189922816)
        {
            await ctx.RespondAsync($"Removed the {role.Mention} role");
            await role.DeleteAsync();
            return;

        }
        else
        {
            return;
        }
    }
    /*[Command]*/
    public async Task GiveRole(CommandContext ctx, DiscordRole role, DiscordMember member)
    {
        if(member.Id == 237384813189922816)
        {
            await member.GrantRoleAsync(role);
            await ctx.RespondAsync($"Gave {member.Username} the {role} role");
            return;
        }
        return;
    }
    
   /* [Command]
    public async Task gif(CommandContext ctx)
    {
        await ctx.RespondAsync("Type !gif [What you want to search]");
        return;
    }

    [Command]
    public async Task gif(CommandContext ctx, [RemainingText] string text)
    {
        var tenor = Programs.tenor;
        var gifSearchResult = tenor.Search(text, 250, "0").GifResults;
        var rand = Programs.rand;
        var index = rand.Next(0, gifSearchResult.Length);
        var gif = gifSearchResult[index];
        await ctx.Channel.SendMessageAsync(gif.ItemUrl.OriginalString);
        return;
    }

    [Command]

    public async Task power(CommandContext ctx)
    {
        var tenor = Programs.tenor;
        var gifSearchResult = tenor.Search("ssj", 100, "0").GifResults;
        var rand = Programs.rand;
        var index = rand.Next(0, gifSearchResult.Length);
        var gif = gifSearchResult[index];
        await ctx.Channel.SendMessageAsync(gif.ItemUrl.OriginalString);
        return;
    }
   */
    [Command]

    public async Task wipe(CommandContext ctx)
    {
        if(ctx.User.Id != 237384813189922816)
        {
            await ctx.Channel.SendMessageAsync("TOO MUCH POWEREREERERE1!1");
            return;
        }
        var msg = ctx.Channel.GetMessagesAsync(150).Result;
        await Task.Delay(1000);
        await ctx.Channel.SendMessageAsync("Deleting Messages...");
        for (int i = 0; i < msg.Count; i++)
        {
            if (msg[i].Author.Id == 924375553090408488)
            {

                await msg[i].DeleteAsync();
                await Task.Delay(250);
            }

        }
        await ctx.Channel.SendMessageAsync("Finished Deleting Messages. <@550540315220770839>");
        return;
    }

    [Command]

    public async Task unmute(CommandContext ctx, DiscordMember mem)
    {
        if(mem.Id == 924375553090408488)
        {
            if(mem.IsMuted)
            {
                await mem.SetMuteAsync(false);
                await ctx.RespondAsync("Soft Rice has been unmuted");
                return;
            }
            else
            {
                await ctx.RespondAsync("Soft Rice is already unmuted");
                return;
            }
        }
        else
        {
            await ctx.RespondAsync("The User is not Soft Rice");
            return;
        }
        return;
    }

    
    
}    

