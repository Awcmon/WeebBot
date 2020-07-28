using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using WeebBot.Services;


namespace WeebBot.Modules
{
	public class PublicModule : ModuleBase<SocketCommandContext>
	{
		// Dependency Injection will fill this value in for us
		public PictureService PictureService { get; set; }
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
			NotificationService.SubUserToFeed(Context.Guild.Id, Context.User.Id, url);
			await ReplyAsync($"Subscribed {Context.User.Username} to {url}.");
		}

		[Command("unsub")]
		public async Task UnsubAsync([Remainder] string url)
		{
			NotificationService.UnsubUserFromFeed(Context.Guild.Id, Context.User.Id, url);
			await ReplyAsync($"Unsubscribed {Context.User.Username} from {url}.");
		}

		[Command("list")]
		public async Task ListAsync()
		{
			await ReplyAsync(Context.User.Username + "\n" + NotificationService.ListFeeds(Context.Guild.Id, Context.User.Id));
		}
	}
}
