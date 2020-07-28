using System;
using System.Collections.Generic;
using System.Data;
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

		public RSSFeed()
		{
			SubscribedGuildUsers = new Dictionary<ulong, HashSet<ulong>>();

			Timer = new Timer(Read, null, 5000, 3600000); //read every hour
		}

		public RSSFeed(string feedUrl) : this()
		{
			FeedUrl = feedUrl;
			
			//Read();
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

		public void Read(Object stateInfo)
		{
			XmlReader reader = XmlReader.Create(FeedUrl);
			SyndicationFeed oldFeed = Feed;
			Feed = SyndicationFeed.Load(reader);
			reader.Close();

			if(oldFeed != Feed)
			{
				OnUpdated(new FeedUpdateArgs(Feed, SubscribedGuildUsers));
			}
		}

		protected void OnUpdated(FeedUpdateArgs e)
		{
			Updated?.Invoke(this, e);
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
