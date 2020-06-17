using System;
using System.Data;
using System.ServiceModel.Syndication;
using System.Xml;

namespace WeebBot
{
	class RSSFeed
	{
		public string FeedUrl { get; private set; }
		public SyndicationFeed Feed { get; private set; }

		public RSSFeed(string feedUrl)
		{
			FeedUrl = feedUrl;
			Read();
		}

		public void Read()
		{
			XmlReader reader = XmlReader.Create(FeedUrl);
			Feed = SyndicationFeed.Load(reader);
			reader.Close();
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
}
