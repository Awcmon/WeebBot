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

		public NotificationService(IServiceProvider services)
		{
			_discord = services.GetRequiredService<DiscordSocketClient>();
			_services = services;

			List<RSSFeed> feeds = new List<RSSFeed>();
			List<Timer> timers = new List<Timer>();

			feeds.Add(new RSSFeed("https://mangadex.org/rss/BEWhGNQMDVpTU4CznK9Hskwfu52Pegva/manga_id/31915"));

			foreach(RSSFeed f in feeds)
			{
				timers.Add(new Timer(f.PrintFeedItems, null, 1000, 1000));
			}
		}


	}
}
