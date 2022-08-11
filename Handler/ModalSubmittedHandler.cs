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
    public class ModalSubmittedHandler
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IMongoRepository<LicenseModel> _licenseModel;
        private readonly IMongoRepository<AvalonModel> _avalonRepository;

        public ModalSubmittedHandler(DiscordSocketClient discordSocketClient,
            IMongoRepository<LicenseModel> eventModel,
            IMongoRepository<AvalonModel> avalonRepository)
        {
            _discordSocketClient = discordSocketClient;
            _licenseModel = eventModel;
            _avalonRepository = avalonRepository;
        }

        public async Task Executed(SocketModal modal)
        {
            if (await _licenseModel.CheckLicense(modal))
            {
                var avalonModel = await _avalonRepository.FindOneAsync(x => x.DiscordId == modal.GuildId);
                if (avalonModel == null) return;
                if (modal.Channel.Id != avalonModel.CommandChannelId) return;

                var customId = modal.Data.CustomId;
                var components = modal.Data.Components.ToList();

                var message = customId.Split("#");

                var command = message[0];
                var messageId = message[1];

                var avalonDataModel = avalonModel.Avalons.SingleOrDefault(x => x.MessageId == ulong.Parse(messageId));
                if (avalonDataModel == null) return;

                var component = components.FirstOrDefault(x => x.CustomId == command);

                if (component == null) return;
                if (modal.GuildId == null) return;

                var guild = _discordSocketClient.GetGuild(modal.GuildId.Value);
                if (guild == null) return;

                var eventChannel = _discordSocketClient.GetChannel(avalonDataModel.ChannelId) as IMessageChannel;
                if (eventChannel == null) return;

                switch (command)
                {
                    case "info":
                        if (avalonDataModel.IsStopped) break;
                        if (avalonDataModel.Manager != modal.User.Id) break;
                        avalonDataModel.Info = component.Value;
                        break;
                }

                await modal.UpdateAsync(x => x.Embed = new EmbedBuilder().CreateAvalonBuild(avalonDataModel));

                if (avalonDataModel.MessageId != 0)
                    await eventChannel.ModifyMessageAsync(avalonDataModel.MessageId, x =>
                    {
                        if (avalonDataModel.Info != null)
                            x.Content = avalonDataModel.Info;
                        x.Embed = new EmbedBuilder().CreatePublicAvalonBuild(guild, avalonDataModel);
                    });

                await _avalonRepository.ReplaceOneAsync(avalonModel);
            }
        }
    }
}
