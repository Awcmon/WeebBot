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

		public RSSFeed(string feedUrl)
		{
			FeedUrl = feedUrl;
			SubscribedGuildUsers = new Dictionary<ulong, HashSet<ulong>>();

			Timer = new Timer(PrintFeedItems, null, 1000, 1000);

			Read();
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

		public void Read()
		{
			XmlReader reader = XmlReader.Create(FeedUrl);
			SyndicationFeed oldFeed = Feed;
			Feed = SyndicationFeed.Load(reader);
			reader.Close();

			if(oldFeed != Feed)
			{
				OnUpdated(new FeedUpdateArgs(Feed));
			}
		}

		protected void OnUpdated(FeedUpdateArgs e)
		{
			EventHandler<FeedUpdateArgs> handler = Updated;
			if(handler != null)
			{
				handler(this, e);
			}
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

		public FeedUpdateArgs(SyndicationFeed feed)
		{
			Feed = feed;
		}
	}
}
