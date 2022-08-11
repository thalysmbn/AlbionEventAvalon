using Discord.Commands;
using Discord.WebSocket;
using EventAvalon.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventAvalon.Handler
{
    public class PrefixHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IOptions<DiscordConfiguration> _discordConfiguration;
        private readonly IServiceProvider _services;

        // Retrieve client and CommandService instance via ctor
        public PrefixHandler(DiscordSocketClient client,
            CommandService commands,
            IOptions<DiscordConfiguration> config,
            IServiceProvider serviceProvider)
        {
            _commands = commands;
            _client = client;
            _discordConfiguration = config;
            _services = serviceProvider;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;
            var socketGuildUser = message.Author as SocketGuildUser;
            //manage_message = socketGuildUser.GuildPermissions.ViewAuditLog;
            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasStringPrefix(_discordConfiguration.Value.Prefix, ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }
    }
}
