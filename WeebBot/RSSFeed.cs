﻿using System;
using System.Collections.Generic;
using System.Data;
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

		public RSSFeed(string feedUrl)
		{
			FeedUrl = feedUrl;

			SubscribedGuildUsers = new Dictionary<ulong, HashSet<ulong>>();

			Timer = new Timer(Read, null, 5000, 300000); //read every 5 mins w/ 5 sec delay
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
			XmlReader reader = XmlReader.Create(FeedUrl);
			SyndicationFeed oldFeed = Feed;
			Feed = SyndicationFeed.Load(reader);
			reader.Close();

			if(oldFeed != null && oldFeed.Items.First().Title != Feed.Items.First().Title)
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
