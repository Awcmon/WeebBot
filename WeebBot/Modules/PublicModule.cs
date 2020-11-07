using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using WeebBot.Services;
using System.ServiceModel.Syndication;
using System.Xml;


namespace WeebBot.Modules
{
	public class PublicModule : ModuleBase<SocketCommandContext>
	{
		// Dependency Injection will fill this value in for us
		public NotificationService NotificationService { get; set; }

		[RequireUserPermission(GuildPermission.Administrator)]
		[Command("set")]
		public async Task SetAsync()
		{
			NotificationService.SetGuildChannel(Context.Channel);
			await ReplyAsync("Set this channel to be used for notifications.");
		}

		[Command("sub")]
		public async Task SubAsync([Remainder] string url)
		{
			XmlReader reader = XmlReader.Create(url);
			try
			{
				SyndicationFeed feed = SyndicationFeed.Load(reader);

				if (NotificationService.SubUserToFeed(Context.Guild.Id, Context.User.Id, url))
				{
					await ReplyAsync($"Subscribed {Context.User.Username} to {feed.Title.Text}");
				}
				else
				{
					await ReplyAsync($"Was not able to subscribe {Context.User.Username} to url. Perhaps you are already subscribed?");
				}
			}
			catch(System.Exception e)
			{
				await ReplyAsync($"Something went wrong. It probably wasn't a RSS feed url.");
			}
			finally
			{
				reader.Close();
			}
		}

		[Command("unsub")]
		public async Task UnsubAsync([Remainder] string url)
		{
			if(NotificationService.UnsubUserFromFeed(Context.Guild.Id, Context.User.Id, url))
			{
				await ReplyAsync($"Unsubscribed {Context.User.Username} from {url}");
			}
			else
			{
				await ReplyAsync($"Was not able to unsubscribe {Context.User.Username} from url.");
			}
		}

		[Command("list")]
		public async Task ListAsync()
		{
			await ReplyAsync(Context.User.Username + "\n" + NotificationService.ListFeeds(Context.Guild.Id, Context.User.Id));
		}

		[RequireOwner]
		[Command("listallfeeds")]
		public async Task ListAllFeedsAsync()
		{
			System.Console.WriteLine(NotificationService.ListAllFeeds());
			await ReplyAsync($"```\n{NotificationService.ListAllFeeds()}```");
		}

		[RequireOwner]
		[Command("forcenotify")]
		public async Task ForceNotify([Remainder] string feedId)
		{
			NotificationService.ForceNotify(feedId);
			await ReplyAsync($"Force notified feed: {feedId}");
		}
	}
}
