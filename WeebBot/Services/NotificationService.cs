using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace WeebBot.Services
{
	public class NotificationService
	{
		private readonly DiscordSocketClient _discord;
		private readonly IServiceProvider _services;

		private Dictionary<string, RSSFeed> feeds;
		private Dictionary<ulong, ulong> channelIDOfGuildID;
		private Dictionary<ulong, ISocketMessageChannel> channelOfGuildID;

		private const string feedsFileName = @"feeds";
		private const string channelsFileName = @"guildchannels";

		public NotificationService(IServiceProvider services)
		{
			_discord = services.GetRequiredService<DiscordSocketClient>();
			_services = services;

			//feeds = new Dictionary<string, RSSFeed>();

			if (!System.IO.File.Exists(channelsFileName))
			{
				channelIDOfGuildID = new Dictionary<ulong, ulong>();
			}
			else
			{
				channelIDOfGuildID = DeserializeGuildChannelMap(System.IO.File.ReadAllLines(channelsFileName));
			}

			DeserializeFeeds();

			_discord.Ready += OnReady;
			
			//AddFeed("https://mangadex.org/rss/BEWhGNQMDVpTU4CznK9Hskwfu52Pegva/manga_id/31915");
		}

		private Task OnReady()
		{
			UpdateChannelMap();

			return Task.CompletedTask;
		}

		public bool AddFeed(string url)
		{
			RSSFeed feed = new RSSFeed(url);
			bool ret = feeds.TryAdd(url, feed);
			if(ret)
			{
				feed.Updated += Notify;
			}
			return ret;
		}

		public bool RemoveFeed(string url)
		{
			if(feeds.ContainsKey(url))
			{
				feeds[url].Timer.Dispose();
				feeds[url].Updated -= Notify;
				return feeds.Remove(url);
			}
			return false;
		}

		public bool SubUserToFeed(ulong guildId, ulong userId, string url)
		{
			if(!feeds.ContainsKey(url))
			{
				AddFeed(url);
			}
			bool ret = feeds[url].AddSubscribedGuildUser(guildId, userId);
			SerializeFeeds();
			return ret;
		}

		public bool UnsubUserFromFeed(ulong guildId, ulong userId, string url)
		{
			if (feeds.ContainsKey(url))
			{
				bool ret = feeds[url].RemoveSubscribedGuildUser(guildId, userId);

				//if the feed no longer has subscribers, remove it from the feeds.
				if(!feeds[url].HasSubscribers())
				{
					RemoveFeed(url);
				}

				SerializeFeeds();
				return ret;
			}
			return false;
		}

		public string ListFeeds(ulong guildID, ulong userID)
		{
			string output = "```";
			foreach(string url in feeds.Keys)
			{
				if(feeds[url].SubscribedGuildUsers.ContainsKey(guildID) && feeds[url].SubscribedGuildUsers[guildID].Contains(userID))
				{
					output += $"{url}\n";
				}
			}
			output += "```";
			return output;
		}

		public void SetGuildChannel(ISocketMessageChannel channel)
		{
			channelIDOfGuildID[(channel as SocketGuildChannel).Guild.Id] = channel.Id;
			System.IO.File.WriteAllText(channelsFileName, SerializeGuildChannelMap());
			//await channel.SendMessageAsync(String.Format("This channel will now be used for {0} posts.", _subredditName));
		}

		private string SerializeGuildChannelMap()
		{
			string output = "";
			foreach (ulong k in channelIDOfGuildID.Keys)
			{
				output += k + "," + channelIDOfGuildID[k] + "\n";
			}
			return output;
		}

		private void SerializeFeeds()
		{
			string output = "";
			foreach(string url in feeds.Keys)
			{
				foreach(ulong guildID in feeds[url].SubscribedGuildUsers.Keys)
				{
					foreach (ulong userID in feeds[url].SubscribedGuildUsers[guildID])
					{
						output += $"{url};{guildID};{userID}\n";
					}
				}
			}
			File.WriteAllText(feedsFileName, output);
		}

		private void DeserializeFeeds()
		{
			feeds = new Dictionary<string, RSSFeed>();
			if (File.Exists(feedsFileName))
			{
				string[] lines = File.ReadAllLines(feedsFileName);
				foreach(string line in lines)
				{
					string[] args = line.Split(';');
					if(args.Length != 3) { Console.WriteLine("Feeds file is incorrect."); return; }

					SubUserToFeed(UInt64.Parse(args[1]), UInt64.Parse(args[2]), args[0]);
				}
			}
		}

		private Dictionary<ulong, ulong> DeserializeGuildChannelMap(string[] lines)
		{
			Dictionary<ulong, ulong> output = new Dictionary<ulong, ulong>();

			foreach (string l in lines)
			{
				if (l == "") { continue; }
				string[] explode = l.Split(',');
				if (explode.Length != 2) { continue; }
				output[UInt64.Parse(explode[0])] = UInt64.Parse(explode[1]);
			}

			return output;
		}

		public void UpdateChannelMap()
		{
			Console.WriteLine("Updating channel map.");

			channelOfGuildID = new Dictionary<ulong, ISocketMessageChannel>();

			foreach (var g in _discord.Guilds)
			{
				//if this guild hasn't set the channel yet, return
				if (!channelIDOfGuildID.ContainsKey(g.Id))
				{
					Console.WriteLine(String.Format("Guild \"{0}\"({1}) has not set a channel yet.", g.Name, g.Id));
					continue;
				}

				//find the channel this bot is supposed to post in and add it to the list
				Boolean added = false;
				foreach (var c in g.Channels)
				{
					if (c.Id == channelIDOfGuildID[g.Id])
					{
						Console.WriteLine(String.Format("Added channel \"{0}\"({1}) from guild \"{2}\"({3}).", c.Name, c.Id, g.Name, g.Id));
						added = true;
						channelOfGuildID.Add(g.Id, c as ISocketMessageChannel);
						break;
					}
				}
				if (!added)
				{
					Console.WriteLine(String.Format("Could not find the channel for Guild \"{0}\"({1}).", g.Name, g.Id));
				}
			}

			Console.WriteLine("Finished updating channels.");
		}

		private async void Notify(object sender, FeedUpdateArgs args)
		{
			UpdateChannelMap();

			foreach(ulong guildID in args.SubscribedGuildUsers.Keys)
			{
				string mentions = args.Feed.Items.First().Title.Text + "\n";
				foreach(ulong userID in args.SubscribedGuildUsers[guildID])
				{
					mentions += (channelOfGuildID[guildID] as SocketGuildChannel).GetUser(userID).Mention + " ";
				}
				//TODO: Maybe properly async this lmao
				await channelOfGuildID[guildID].SendMessageAsync(mentions + "\n" + args.Feed.Items.First()?.Links.First()?.GetAbsoluteUri());
			}
		}

	}
}
