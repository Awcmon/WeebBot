using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading;
using System.Collections.Generic;

namespace WeebBot.Services
{
	public class NotificationService
	{
		private readonly DiscordSocketClient _discord;
		private readonly IServiceProvider _services;

		private Dictionary<string, RSSFeed> feeds;
		private Dictionary<ulong, ulong> channelOfGuild;

		public NotificationService(IServiceProvider services)
		{
			_discord = services.GetRequiredService<DiscordSocketClient>();
			_services = services;

			feeds = new Dictionary<string, RSSFeed>();

			AddFeed("https://mangadex.org/rss/BEWhGNQMDVpTU4CznK9Hskwfu52Pegva/manga_id/31915");
		}

		public bool AddFeed(string url)
		{
			return feeds.TryAdd(url, new RSSFeed(url));
		}

		public bool RemoveFeed(string url)
		{
			return feeds.Remove(url);
		}

		public bool SubUserToFeed(ulong guildId, ulong userId, string url)
		{
			if(feeds.ContainsKey(url))
			{
				return feeds[url].AddSubscribedGuildUser(guildId, userId);
			}
			return false;
		}

		public bool UnsubUserFromFeed(ulong guildId, ulong userId, string url)
		{
			if (feeds.ContainsKey(url))
			{
				return feeds[url].RemoveSubscribedGuildUser(guildId, userId);
			}
			return false;
		}

		public void SetGuildChannel(ISocketMessageChannel channel)
		{
			channelOfGuild[(channel as SocketGuildChannel).Guild.Id] = channel.Id;
			//System.IO.File.WriteAllText(channelsFileName, SerializeGuildChannelMap());
			//await channel.SendMessageAsync(String.Format("This channel will now be used for {0} posts.", _subredditName));
		}

	}
}
