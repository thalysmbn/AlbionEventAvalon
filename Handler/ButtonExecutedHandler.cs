using Discord;
using Discord.WebSocket;
using EventAvalon.Database;
using EventAvalon.Extensions;
using EventAvalon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventAvalon.Handler
{
    public class ButtonExecutedHandler
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IMongoRepository<LicenseModel> _licenseModel;
        private readonly IMongoRepository<AvalonModel> _avalonModel;

        public ButtonExecutedHandler(DiscordSocketClient discordSocketClient,
            IMongoRepository<LicenseModel> eventModel,
            IMongoRepository<AvalonModel> avalonModel)
        {
            _discordSocketClient = discordSocketClient;
            _licenseModel = eventModel;
            _avalonModel = avalonModel;
        }
        public async Task Executed(SocketMessageComponent component)
        {
            try
            {
                if (await _licenseModel.CheckLicense(component))
                {
                    var avalonModel = await _avalonModel.FindOneAsync(x => x.DiscordId == component.GuildId);
                    if (_avalonModel == null) return;

                    var message = component.Data.CustomId.Split("#");

                    var command = message[0];
                    var value = message[1];

                    if (component.GuildId == null) return;

                    var guild = _discordSocketClient.GetGuild(component.GuildId.Value);
                    if (guild == null) return;

                    var avalon = avalonModel.Avalons.SingleOrDefault(x => x.MessageId == ulong.Parse(value));
                    if (avalon == null) return;

                    switch (command)
                    {
                        case "startOrPause":
                        case "stop":
                            if (avalon.IsStopped) break;
                            if (avalon.Manager != component.User.Id) break;
                            await Task.Run(async () =>
                            {
                                switch (command)
                                {
                                    case "stop":
                                        avalon.IsStopped = true;
                                        break;
                                    default:
                                        avalon.IsPaused = !avalon.IsPaused;
                                        break;
                                }
                                await component.UpdateAsync(x =>
                                {
                                    x.Embed = new EmbedBuilder().CreateAvalonBuild(avalon);
                                    x.Components = new ComponentBuilder().CreateAvalonComponentBuilder(avalon);
                                });
                                await _avalonModel.ReplaceOneAsync(avalonModel);
                            }).ContinueWith(async x =>
                            {
                                var eventChannel = _discordSocketClient.GetChannel(avalon.ChannelId) as IMessageChannel;
                                if (eventChannel == null) return;

                                await eventChannel.ModifyMessageAsync(avalon.MessageId, x =>
                                {
                                    x.Content = avalon.Info == null ? $"{avalon.MessageId}" : avalon.Info;
                                    x.Embed = new EmbedBuilder().CreatePublicAvalonBuild(guild, avalon);
                                });
                            });
                            break;
                        case "edit":
                            if (avalon.IsStopped) break;
                            if (avalon.Manager != component.User.Id) break;
                            await component.RespondWithModalAsync(new ModalBuilder()
                                .WithTitle("Event Avalon")
                                .WithCustomId($"info#{value}")
                                .AddTextInput("Info", "info", TextInputStyle.Paragraph, value: avalon.Info == null ? "" : avalon.Info)
                                .Build());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
