using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using EventAvalon.Database;
using EventAvalon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventAvalon.Extensions
{
    public static class AlbionExtensions
    {
        public static MessageComponent CreateAvalonComponentBuilder(this ComponentBuilder component, Avalon eventDataModel)
        {
            component
                    .WithButton("Start/Pause", $"startOrPause#{eventDataModel.MessageId}", ButtonStyle.Success, new Emoji("⏰"), disabled: eventDataModel.IsStopped)
                    .WithButton("Edit", $"edit#{eventDataModel.MessageId}", ButtonStyle.Secondary, new Emoji("📝"), disabled: eventDataModel.IsStopped)
                    .WithButton("Stop Event", $"stop#{eventDataModel.MessageId}", ButtonStyle.Danger, disabled: eventDataModel.IsStopped);
            return component.Build();
        }

        public static Embed CreateAvalonBuild(this EmbedBuilder embed, Avalon eventDataModel)
        {
            embed.Color = eventDataModel.IsStopped ? Color.Red : eventDataModel.IsPaused ? Color.Orange : Color.Green;
            embed.Description = $"Manager: <@{eventDataModel.Manager}> \n";
            embed.AddField("Channel", $"<#{eventDataModel.ChannelId}>", true);
            embed.AddField("Users:", $"{eventDataModel.Members.Count} / {eventDataModel.Data.Sum(x => x.Limit)}", true);
            return embed.Build();
        }

        public static Embed CreatePublicAvalonBuild(this EmbedBuilder embed, SocketGuild guild, Avalon eventDataModel)
        {
            var builder = new EmbedBuilder()
            {
                //Optional color
                Color = eventDataModel.IsStopped ? Color.Red : eventDataModel.IsPaused ? Color.Orange : Color.Green,
                Description = $"Manager: <@{eventDataModel.Manager}> \n"
            };

            var users = guild.Users;
            // {string.Join(",", members.Select(kv => kv.Value).ToArray())}
            foreach (var log in eventDataModel.Data)
            {
                var geralBuilder = new StringBuilder();
                var roleMembers = eventDataModel.Members.Where(x => x.Role == log.Role);

                var index = 0;
                var members = new Dictionary<int, string>();
                foreach (var item in roleMembers.OrderBy(x => x.Id))
                {
                    if (users.Any(x => x.Id == item.UserId))
                        members.Add(index++, item.Nickname);
                    else
                        eventDataModel.Members.Remove(item);
                }
                foreach (var member in members)
                {
                    Console.WriteLine($"{member.Key} {member.Value}");
                    if (member.Key == log.Limit)
                        geralBuilder.Append("**Reservas**:\n");
                    geralBuilder.Append($"{member.Value}\n");
                }
                if (members.Count() <= 0) geralBuilder.Append("-\n");
                builder.AddField($"{log.Role} {log.Title} ( x{log.Limit} )", $"{geralBuilder}", true);
            }
            return builder.Build();
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, SocketModal modal)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == modal.GuildId);
            if (model == null)
            {
                await modal.RespondAsync("Not found.");
                return false;
            }
            if (!model.IsValid)
            {
                await modal.RespondAsync($"License expired: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, SocketMessageComponent component)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == component.GuildId);
            if (model == null)
            {
                await component.RespondAsync("Not found.");
                return false;
            }
            if (!model.IsValid)
            {
                await component.RespondAsync($"License expired: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, SocketInteractionContext context)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == context.Guild.Id);
            if (model == null)
            {
                await context.Interaction.RespondAsync("Not found.");
                return false;
            }
            if (!model.IsValid)
            {
                await context.Interaction.RespondAsync($"License expired: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }
    }
}
