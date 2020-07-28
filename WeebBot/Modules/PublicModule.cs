﻿using System.IO;
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

		[Command("ping")]
		[Alias("pong", "hello")]
		public Task PingAsync()
			=> ReplyAsync("pong!");

		[Command("cat")]
		public async Task CatAsync()
		{
			// Get a stream containing an image of a cat
			var stream = await PictureService.GetCatPictureAsync();
			// Streams must be seeked to their beginning before being uploaded!
			stream.Seek(0, SeekOrigin.Begin);
			await Context.Channel.SendFileAsync(stream, "cat.png");
		}

		// Get info on a user, or the user who invoked the command if one is not specified
		[Command("userinfo")]
		public async Task UserInfoAsync(IUser user = null)
		{
			user = user ?? Context.User;

			await ReplyAsync(user.ToString());
		}

		// Ban a user
		[Command("ban")]
		[RequireContext(ContextType.Guild)]
		// make sure the user invoking the command can ban
		[RequireUserPermission(GuildPermission.BanMembers)]
		// make sure the bot itself can ban
		[RequireBotPermission(GuildPermission.BanMembers)]
		public async Task BanUserAsync(IGuildUser user, [Remainder] string reason = null)
		{
			await user.Guild.AddBanAsync(user, reason: reason);
			await ReplyAsync("ok!");
		}

		// [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
		[Command("echo")]
		public Task EchoAsync([Remainder] string text)
			// Insert a ZWSP before the text to prevent triggering other bots!
			=> ReplyAsync('\u200B' + text);

		// 'params' will parse space-separated elements into a list
		[Command("list")]
		public Task ListAsync(params string[] objects)
			=> ReplyAsync("You listed: " + string.Join("; ", objects));

		// Setting a custom ErrorMessage property will help clarify the precondition error
		[Command("guild_only")]
		[RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
		public Task GuildOnlyCommand()
			=> ReplyAsync("Nothing to see here!");

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
		//TODO: List
	}
}