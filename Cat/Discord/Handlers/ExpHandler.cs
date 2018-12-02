﻿using System;
using System.Threading.Tasks;
using Cat.Discord.Interfaces;
using Cat.Persistence.Interfaces.UnitOfWork;
using Cat.Services;
using Discord.Commands;
using Discord.WebSocket;

namespace Cat.Discord.Handlers
{
    public class ExpHandler : IExpHandler
    {
        private DiscordShardedClient _client;
        private readonly ILogger _logger;

        public ExpHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Initialize(DiscordShardedClient client)
        {
            _client = client;
            _client.MessageReceived += MessageReceived;
            _client.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState state1, SocketVoiceState state2)
        {
            await UserVoiceStateUpdatedAsync(user, state1, state2).ConfigureAwait(false);
        }

        private async Task UserVoiceStateUpdatedAsync(SocketUser socketUser, SocketVoiceState oldState, SocketVoiceState newState)
        {
            if(!(socketUser is SocketGuildUser guildUser)) return;
            using (var unitOfWork = Unity.Resolve<IUnitOfWork>())
            {
                var user = await unitOfWork.UserInfos.GetOrAddUserInfoAsync(guildUser.Guild.Id, guildUser.Id).ConfigureAwait(false);
                if (oldState.VoiceChannel == null) user.LastVoiceStateUpdateReceived = DateTime.Now;
                if (newState.VoiceChannel == null)
                {
                    var timeDiff = (decimal) DateTime.Now.Subtract(user.LastVoiceStateUpdateReceived).TotalMilliseconds;
                    user.TimeConnected += timeDiff;
                    user.Xp += timeDiff / 5;
                }

                await unitOfWork.SaveAsync().ConfigureAwait(false);
            }
        }

        private async Task MessageReceived(SocketMessage message)
        {
            if (!(message is SocketUserMessage msg)) return;
            await MessageReceivedAsync(msg).ConfigureAwait(false);
        }

        private async Task MessageReceivedAsync(SocketUserMessage message)
        {
            var context = new ShardedCommandContext(_client, message);
            using (var unitOfWork = Unity.Resolve<IUnitOfWork>())
            {
                var user = await unitOfWork.UserInfos.GetOrAddUserInfoAsync(context.Guild.Id, context.User.Id).ConfigureAwait(false);
                Console.WriteLine(DateTime.Now.Subtract(user.LastMessageSend).TotalSeconds);
                if (!(DateTime.Now.Subtract(user.LastMessageSend).TotalSeconds > 4)) return;
                user.Xp += 2;
                user.LastMessageSend = DateTime.Now;
                Console.WriteLine(user.Xp);
                await unitOfWork.SaveAsync().ConfigureAwait(false);
            }
        }
    }
}