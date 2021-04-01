using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Data = Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;

namespace Attendance_Bot
{
	public class Module : ModuleBase<SocketCommandContext>
	{
		readonly static string[] Scopes = { SheetsService.Scope.Spreadsheets };
		readonly static string ApplicationName = "Attendance Tracker";
		readonly static string sheetID = "1QzUN-oLblObDihNUoUB6PL1a-HOr4MdMwBl1d9wtrc0";
		private readonly DiscordSocketClient client;
		private readonly CommandService commands;
		private readonly IServiceProvider services;

		public Module(IServiceProvider _services, CommandService _commands, DiscordSocketClient _client)
		{
			client = _client;
			commands = _commands;
			services = _services;
		}

		[Command("test")]
		[Summary("Repeats a message. Used for testing.")]
		public async Task SayAsync([Remainder][Summary("The message to repeat.")] SocketUser user = null)
		{
			var userInfo = user ?? Context.Message.Author;
			await ReplyAsync($"Username: {userInfo.Username}");
		}

		[Command("schedule")]
		[Summary("Adds someone to the schedule for a specific time.")]
		public async Task ScheduleAsync(SocketUser user, string time)
		{
			if (user == null || time == null)
			{
				await ReplyAsync("Command arguments invalid. Use \"!schedule @username time\"");
				return;
			}

			DateTime target;

			try
			{
				target = DateTime.Parse(time);
			}
			catch (Exception e)
			{
				await ReplyAsync($"There was an issue with the time format. The error message is {e.Message}");
				return;
			}

			int millisecondsUntil = (int)(target - DateTime.Now).TotalMilliseconds;
			if (millisecondsUntil <= 0)
			{
				await ReplyAsync($"Your time is in the past dummy! {target} has already passed!");
				return;
			}

			Thread childThread = new Thread(() => ScheduleThread(millisecondsUntil, user));
			childThread.Start();

			await ReplyAsync($"You're all set! {user.Username} is now scheduled to be on at {target} or earlier.");

			//await ReplyAsync($"Once implemented, this will schedule {user.Mention} for {target}");
		}

		private async void ScheduleThread(int msUntil, SocketUser user)
		{
			Thread.Sleep(msUntil);

			//This channel ID is the ID of a text channel me and my friends use in our server.
			//It's the channel I want the bot to shame them in for them being late.
			ulong channelID = 634800814170439706; 
			var channel = client.GetChannel(channelID) as IMessageChannel;

			//This is the channel ID of our voice channel. I only want to make fun of them
			//if they didn't actually get on, so I need to check that they're not connected.
			var voiceChannel = client.GetChannel(775563983737847859) as IVoiceChannel;

			var usersInVC = voiceChannel.GetUsersAsync();
			for (usersInVC.)

			Console.WriteLine($"Child thread has received the channel. It is {channel}");
			if (channel != null) await channel.SendMessageAsync($"{user.Mention} <:lionLate:339569494899032076>");
			else Console.WriteLine("The channel was null.");
		}
	}
}
