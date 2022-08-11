using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EventAvalon.Configurations;
using EventAvalon.Database;
using EventAvalon.Extensions;
using EventAvalon.Handler;
using EventAvalon.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Services
{
    public class DiscordBOT : IHostedService
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly InteractionHandler _interactionHandler;
        private readonly PrefixHandler _prefixHandler;
        private readonly IMongoRepository<AvalonModel> _avalonRepository;
        private readonly IOptions<DiscordConfiguration> _discordConfiguration;

        public DiscordBOT(DiscordSocketClient discordSocketClient,
            InteractionService interactionService,
            InteractionHandler interactionHandler,
            PrefixHandler prefixHandler,
            ButtonExecutedHandler buttonExecutedHandler,
            ModalSubmittedHandler modalSubmittedHandler,
            IMongoRepository<AvalonModel> avalonRepository,
            IOptions<DiscordConfiguration> discordConfiguration)
        {
            _discordSocketClient = discordSocketClient;
            _discordConfiguration = discordConfiguration;
            _interactionHandler = interactionHandler;
            _prefixHandler = prefixHandler;
            _avalonRepository = avalonRepository;
            _discordSocketClient.Ready += async () =>
            {
                await interactionService.RegisterCommandsGloballyAsync(true);
            };
            _discordSocketClient.ButtonExecuted += buttonExecutedHandler.Executed;
            _discordSocketClient.ModalSubmitted += modalSubmittedHandler.Executed;
            _discordSocketClient.ReactionAdded += ReactionAdded;
            _discordSocketClient.ReactionRemoved += ReactionRemoved;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _interactionHandler.InitializeAsync();
            await _prefixHandler.InitializeAsync();
            await _discordSocketClient.LoginAsync(TokenType.Bot, _discordConfiguration.Value.Token);
            await _discordSocketClient.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordSocketClient.DisposeAsync();
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message,
            Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
        {

            try
            {
                if (react.UserId == _discordSocketClient.CurrentUser.Id) return;

                var storedMessage = _avalonRepository.AsQueryable().Where(x => x.Avalons.Any(x => x.MessageId == message.Id)).SingleOrDefault();
                if (storedMessage == null) return;

                var guild = _discordSocketClient.GetGuild(storedMessage.DiscordId);
                if (guild == null) return;

                var avalonModel = storedMessage.Avalons.SingleOrDefault(x => x.MessageId == message.Id);
                if (avalonModel == null) return;

                if (avalonModel.IsStopped) return;
                if (avalonModel.IsPaused) return;

                Console.WriteLine("aaaaaaaaa");

                if (avalonModel.Members.Any(x => x.UserId == react.UserId))
                {
                    var currentUserEmote = avalonModel.Members.SingleOrDefault(x => x.UserId == react.UserId);
                    if (currentUserEmote == null) return;

                    if (react.Emote.Name == currentUserEmote.Role)
                    {
                        var member = avalonModel.Members.SingleOrDefault(x => x.UserId == react.UserId);
                        if (member == null) return;
                        avalonModel.Members.Remove(member);
                    }
                }
                else
                {
                    var user = guild.GetUser(react.UserId);

                    avalonModel.Members.Add(new AvalonMember
                    {
                        Id = avalonModel.LastId,
                        UserId = user.Id,
                        Nickname = user.Nickname == null ? user.Username : user.Nickname,
                        Role = react.Emote.Name
                    });
                }

                avalonModel.LastId++;

                await _avalonRepository.ReplaceOneAsync(storedMessage);

                await react.Channel.ModifyMessageAsync(react.MessageId,
                    x => x.Embed = new EmbedBuilder().CreatePublicAvalonBuild(guild, avalonModel));
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> message,
            Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
        {

        }
    }
}
