﻿using System;
using System.Threading.Tasks;
using Cat.Configurations;
using Cat.Discord.Services;
using Cat.Services;
using Discord;
using Discord.WebSocket;
using IConnection = Cat.Discord.Interfaces.IConnection;

namespace Cat.Discord
{
    public class Connection : IConnection
    {
        private readonly DiscordShardedClient _client;
        private readonly IDiscordLogger _discordLogger;
        private readonly ILogger _logger;

        public Connection(DiscordShardedClient client, IDiscordLogger discordLogger, ILogger logger)
        {
            _client = client;
            _discordLogger = discordLogger;
            _logger = logger;
        }
        
        public async Task ConnectAsync()
        {
            _client.Log += _discordLogger.Log;
            _client.ShardLatencyUpdated += ShardLatencyUpdatedAsync;
            _client.ShardDisconnected += ShardDisconnectedAsync;

            await _client.LoginAsync(TokenType.Bot, ConfigData.Data.Token).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);

            await Task.Delay(ConfigData.Data.RestartTime * 60000).ConfigureAwait(false);
            await _client.StopAsync().ConfigureAwait(false);
        }

        private Task ShardDisconnectedAsync(Exception exception, DiscordSocketClient shard)
        {
            _logger.Log("Connection/Disconnected", $"Shard: {shard.ShardId} reason: {exception.Message}");
            return Task.CompletedTask;
        }

        private Task ShardLatencyUpdatedAsync(int oldPing, int updatePing, DiscordSocketClient shard)
        {
            if (updatePing >= 500) _logger.Log("Connection/Latency", $"Shard: {shard.ShardId} Latency: {updatePing}");
            return Task.CompletedTask;
        }
    }
}
