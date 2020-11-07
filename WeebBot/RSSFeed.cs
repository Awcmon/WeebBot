using Discord.Net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Xml;

namespace WeebBot
{
	class RSSFeed
	{
		public string FeedUrl { get; private set; }
		public SyndicationFeed Feed { get; private set; }

		public Timer Timer { get; private set; }

		public event EventHandler<FeedUpdateArgs> Updated;

		public Dictionary<ulong, HashSet<ulong>> SubscribedGuildUsers; //maps guild to users in that guild

		//private DateTimeOffset? lastPublishDateTime;
		private string lastItemTitle;

		public RSSFeed(string feedUrl, int delayOrder)
		{
			FeedUrl = feedUrl;

			lastItemTitle = null;

			SubscribedGuildUsers = new Dictionary<ulong, HashSet<ulong>>();

			Timer = new Timer(Read, null, 5000*(delayOrder+1), 300000); //read every 5 mins w/ 5 sec delay. Delay each new feed by 5 seconds
		}

		public bool AddSubscribedGuildUser(ulong guildId, ulong userId)
		{
			if(!SubscribedGuildUsers.ContainsKey(guildId))
			{
				SubscribedGuildUsers.Add(guildId, new HashSet<ulong>());
			}
			return SubscribedGuildUsers[guildId].Add(userId);
		}

		public bool RemoveSubscribedGuildUser(ulong guildId, ulong userId)
		{
			if (!SubscribedGuildUsers.ContainsKey(guildId))
			{
				return false;
			}
			return SubscribedGuildUsers[guildId].Remove(userId);
		}

		//Note: if the bot goes down, it will not give any notifications for anything new that came up while it was down.
		public void Read(Object stateInfo)
		{
			Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Updating {FeedUrl}");
			try
			{
				using (XmlReader reader = XmlReader.Create(FeedUrl))
				{
					Feed = SyndicationFeed.Load(reader);
					//reader.Close();

					if (lastItemTitle != null && lastItemTitle != Feed.Items.First().Title.Text)
					{
						OnUpdated(new FeedUpdateArgs(Feed, SubscribedGuildUsers));
					}
					lastItemTitle = Feed.Items.First().Title.Text;
				}
				//Console.WriteLine($"{DateTime.UtcNow.ToLongTimeString()} Updated {FeedUrl}: {Feed.Title.Text}");
			}
			catch (Exception e)
			{
				Console.WriteLine($"{DateTime.UtcNow.ToLongTimeString()} Exception when reading {FeedUrl} \n{e.ToString()}");
				Console.WriteLine($"{DateTime.UtcNow.ToLongTimeString()} Unable to update {FeedUrl}");
			}
		}

		protected void OnUpdated(FeedUpdateArgs e)
		{
			Updated?.Invoke(this, e);
		}

		public void ForceUpdated()
		{
			Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Force notifying {FeedUrl}");
			try
			{
				using (XmlReader reader = XmlReader.Create(FeedUrl))
				{
					Feed = SyndicationFeed.Load(reader);
					OnUpdated(new FeedUpdateArgs(Feed, SubscribedGuildUsers));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"{DateTime.UtcNow.ToLongTimeString()} Exception when reading {FeedUrl} \n{e.ToString()}");
				Console.WriteLine($"{DateTime.UtcNow.ToLongTimeString()} Unable to update {FeedUrl}");
			}
		}

		public bool HasSubscribers()
		{
			foreach(HashSet<ulong> userSet in SubscribedGuildUsers.Values)
			{
				foreach(ulong userId in userSet)
				{
					return true;
				}
			}
			return false;
		}

		public void PrintFeedItems(Object stateInfo)
		{
			foreach (SyndicationItem item in Feed.Items)
			{
				String subject = item.Title.Text;
				String summary = item.Summary.Text;
				DateTimeOffset date = item.PublishDate;
				Console.WriteLine($"{subject}\n{summary}\n{date}");
				foreach (SyndicationLink l in item.Links)
				{
					Console.WriteLine($"{l.GetAbsoluteUri()}");
				}
				Console.WriteLine($"\n");
			}
		}

		public string FeedMapToString(int tabs = 0)
		{
			string ret = "";

			ret += $"{new string('\t', tabs)}{FeedUrl}\n";
			
			foreach(ulong guildId in SubscribedGuildUsers.Keys)
			{
				ret += $"{new string('\t', tabs+1)}{guildId}\n";
				foreach(ulong userId in SubscribedGuildUsers[guildId])
				{
					ret += $"{new string('\t', tabs+2)}{userId}\n";
				}
			}

			return ret;
		}

	}

	public class FeedUpdateArgs : EventArgs
	{
		public SyndicationFeed Feed { get; set; }
		public Dictionary<ulong, HashSet<ulong>> SubscribedGuildUsers { get; set; } 

		public FeedUpdateArgs(SyndicationFeed feed, Dictionary<ulong, HashSet<ulong>> subscribedGuildUsers)
		{
			Feed = feed;
			SubscribedGuildUsers = subscribedGuildUsers;
		}
	}
}
