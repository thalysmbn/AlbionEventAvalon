using Discord;
using Discord.Interactions;
using EventAvalon.Database;
using EventAvalon.Extensions;
using EventAvalon.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventAvalon.Modules
{
    public class AvalonInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IMongoRepository<LicenseModel> _licenseRepository;
        private readonly IMongoRepository<AvalonModel> _avalonRepository;

        public AvalonInteractionModule(IMongoRepository<LicenseModel> licenseRepository,
            IMongoRepository<AvalonModel> avalonRepository)
        {
            _licenseRepository = licenseRepository;
            _avalonRepository = avalonRepository;
        }

        [SlashCommand("build", "Build Raid Avalon")]
        [RequireRole("Manager")]
        public async Task Build(IMessageChannel channel)
        {
            var guild = Context.Guild;
            var avalonModel = await _avalonRepository.FindOneAsync(x => x.DiscordId == Context.Guild.Id);
            if (avalonModel == null)
            {
                await _avalonRepository.InsertOneAsync(new AvalonModel
                {
                    DiscordId = Context.Guild.Id,
                    CommandChannelId = channel.Id,
                    Managers = new List<ulong> { Context.User.Id },
                    Avalons = new List<Avalon>()
                });
                await RespondAsync("Event Avalon builded.");
            }
            else
            {
                avalonModel.CommandChannelId = channel.Id;

                await _avalonRepository.ReplaceOneAsync(avalonModel);

                await RespondAsync("Event Avalon sync.");
            }
        }

        [SlashCommand("create", "Create an Raid Avalon")]
        [RequireRole("Manager")]
        public async Task Create(IMessageChannel channel,
            int tankHammerLimit = 1,
            int tankMaceLimit = 1,
            int healerLimit = 1,
            int partyHealerLimit = 1,
            int greatArcaneLimit = 1,
            int arcaneOneHandLimit = 1,
            int ironRootLimit = 3,
            int shadowCallerLimit = 1,
            int xbowLimit = 4,
            int frostLimit = 2,
            int fireLimit = 1,
            int spiritHunterLimit = 1,
            int realmBreakerLimit = 1,
            int carvingSwordLimit = 1,
            int scoutLimit = 1)
        {
            if (await _licenseRepository.CheckLicense(Context))
            {
                var guild = Context.Guild;
                if (guild == null) return;

                var avalonModel = await _avalonRepository.FindOneAsync(x => x.DiscordId == guild.Id);
                if (avalonModel == null) return;
                if (Context.Channel.Id != avalonModel.CommandChannelId) return;

                if (!avalonModel.Managers.Contains(Context.User.Id)) return;

                try
                {
                    var message = await channel.SendMessageAsync($"...");

                    var avalonDataModel = new Avalon
                    {
                        ChannelId = channel.Id,
                        MessageId = message.Id,
                        Manager = Context.User.Id,
                        IsPaused = true,
                        IsStopped = false,
                        LastId = 0,
                        Members = new List<AvalonMember>(),
                        Data = new List<AvalonData>()
                    };
                    if (tankHammerLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "👑", Title = "Tank Hammer", Slug = "tankHammer", Limit = tankHammerLimit });
                    if (tankMaceLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🛡️", Title = "Tank Mace", Slug = "tankMace", Limit = tankMaceLimit });
                    if (healerLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "❤️", Title = "Healer", Slug = "healer", Limit = healerLimit });
                    if (partyHealerLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "💚", Title = "Party Healer", Slug = "partyHealer", Limit = partyHealerLimit });
                    if (greatArcaneLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🕐", Title = "Great Arcane", Slug = "grateArcane", Limit = greatArcaneLimit });
                    if (arcaneOneHandLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🔮", Title = "Arcane 1H", Slug = "arcaneOneHand", Limit = arcaneOneHandLimit });
                    if (ironRootLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🌿", Title = "Ironroot", Slug = "ironroot", Limit = ironRootLimit });
                    if (shadowCallerLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "💔", Title = "Shadowcaller", Slug = "shadowcaller", Limit = shadowCallerLimit });
                    if (xbowLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🏹", Title = "Xbow", Slug = "xbow", Limit = xbowLimit });
                    if (frostLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "❄️", Title = "Frost", Slug = "frost", Limit = frostLimit });
                    if (fireLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🔥", Title = "Fire", Slug = "fire", Limit = fireLimit });
                    if (spiritHunterLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "👻", Title = "Spirit Hunter", Slug = "spiritHunter", Limit = spiritHunterLimit });
                    if (realmBreakerLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🪓", Title = "Realmbreaker", Slug = "realmbreaker", Limit = realmBreakerLimit });
                    if (carvingSwordLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🗡️", Title = "Carving Sword", Slug = "sarvingSword", Limit = carvingSwordLimit });
                    if (scoutLimit != 0)
                        avalonDataModel.Data.Add(new() { Role = "🕵️‍♂️", Title = "Scout", Slug = "scout", Limit = scoutLimit });

                    avalonModel.Avalons.Add(avalonDataModel);

                    _ = Task.Run(async () =>
                    {
                        await message.ModifyAsync(x =>
                        {
                            x.Content = $"#{avalonDataModel.MessageId}";
                            x.Embed = new EmbedBuilder().CreatePublicAvalonBuild(guild, avalonDataModel);
                        });
                        await message.AddReactionsAsync(avalonDataModel.Data.Select(x => new Emoji(x.Role)));
                    });

                    await RespondAsync($"#{message.Id}",
                        embeds: new[] { new EmbedBuilder().CreateAvalonBuild(avalonDataModel) },
                        components: new ComponentBuilder().CreateAvalonComponentBuilder(avalonDataModel));

                    await _avalonRepository.ReplaceOneAsync(avalonModel);
                } catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
