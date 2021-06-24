using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using Data = Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Attendance_Bot
{
	public class Module : ModuleBase<SocketCommandContext>
	{
		readonly static string[] Scopes = { SheetsService.Scope.Spreadsheets };
		readonly static string ApplicationName = "Attendance Tracker";
		//This is the ID for my attendance google sheet, if you want to use a different one you should change this.
		// (Better practice would probably be to read this in with the token, but I figure you probably won't have permission
		//  to edit it anyway)
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

		//[Command("schedule")]
		//[Summary("Adds someone to the schedule for a specific time.")]
		//public async Task ScheduleAsync()
		//{
		//	await ReplyAsync("Command arguments invalid. Use \"!schedule @username time\"");
		//	return;
		//}

		//[Command("schedule")]
		//[Summary("Adds someone to the schedule for a specific time.")]
		//public async Task ScheduleAsync(string time)
		//{
		//	await ScheduleAsync(Context.User, time);
		//	return;
		//}

		[Command("schedule")]
		[Summary("Adds someone to the schedule for a specific time.")]
		public async Task ScheduleAsync(string sUser = null, [Remainder]string time = null)
		{
			Console.WriteLine("[DEBUG] user = " + sUser ?? "null");
			Console.WriteLine("[DEBUG] time = " + time ?? "null");

			if (time == null || time == "")
			{
				await ReplyAsync("Command arguments invalid. Use \"!schedule @username time\"");
				return;
			}

			sUser = sUser.Replace("<@!", "").Replace(">", "");
			Console.WriteLine("[DEBUG] User ID to find: " + sUser);
			SocketGuildUser user;
			user = Context.Guild.GetUser(ulong.Parse(sUser));
			if (user == null)
			{
				await ReplyAsync("There was an issue finding that user.");
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
				await ReplyAsync($"Your time ({target}) is in the past!");
				return;
			}

			//starts a new thread that calls the ScheduleThread method, so that the bot can keep doing stuff while waiting for users
			Thread childThread = new Thread(() => ScheduleThread(millisecondsUntil, user));
			childThread.Start();

			await ReplyAsync($"You're all set! {user.Nickname ?? user.Username} is now scheduled to be on at {target} or earlier.");

			//await ReplyAsync($"Once implemented, this will schedule {user.Mention} for {target}");
		}

		private async void ScheduleThread(int msUntil, SocketGuildUser user)
		{
			Thread.Sleep(msUntil);

			//This channel ID is the ID of a text channel me and my friends use in our server.
			//It's the channel I want the bot to shame them in for them being late.
			ulong channelID = 634800814170439706; 
			var channel = client.GetChannel(channelID) as IMessageChannel;

			//This is the channel ID of our voice channel. I only want to make fun of them
			//if they didn't actually get on, so I need to check that they're not connected.s
			var voiceChannel = client.GetChannel(775563983737847859) as IVoiceChannel;

			UserCredential credential;
			
			using (var stream = 
				new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore(credPath, true)).Result;
				Console.WriteLine($"Google Sheets credential file saved to {credPath}");
			}

			SheetsService service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName
			});

			//Sets a variable to represent the value input option we want (User entered; auto formats things like someone entered them into the sheet)
			var valIn = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
			//Sets a variable for the insert data option (overwrite; overwrites the empty row instead of inserting a new one)
			var dataIn = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.OVERWRITE;
			
			Data.ValueRange val = new Data.ValueRange();
			//We need this to be a 2D list since it's a google sheet, which is 2D
			IList<IList<Object>> list = new List<IList<Object>>();
			//We're making a new row that's formatted as "Username, Discord ID, Absent or Present, Short Date, Long Time".
			IList<Object> row = new List<Object>
			{
				user.Nickname ?? user.Username,
				user.ToString()
			};

			SocketGuildUser guildUser = user as SocketGuildUser;
			//if the user's voice channel is null or does not match our voice channel's ID, they're absent. Otherwise, they're in the voice channel and not absent.
			bool absent = guildUser.VoiceChannel == null ? true : guildUser.VoiceChannel.Id != voiceChannel.Id;

			if (absent)
			{
				//If the user is late, we @ them in the server and include an emoji of someone tapping a watch.
				//TODO (maybe): Randomize messages from a pool of potential responses
				if (channel != null) await channel.SendMessageAsync($"{user.Mention} <:lionLate:339569494899032076>");
				else Console.WriteLine("The channel was null.");
				//Add "Absent" to the row that will be inserted
				row.Add("Absent");
			}
			else
			{
				row.Add("Present");
			}

			row.Add(DateTime.Now.ToShortDateString());
			row.Add(DateTime.Now.ToLongTimeString());

			//add our row to the 2D list, and set the value range's values to be equal to that list.
			list.Add(row);
			val.Values = list;

			//creates a request for appending "val" (5x1 row) to the sheet in the A2 to E2 range, or whatever's under it, in the sheet "Attendance Record"
			var append = service.Spreadsheets.Values.Append(val, sheetID, "\'Attendance Record\'!A2:E2");
			append.ValueInputOption = valIn;
			append.InsertDataOption = dataIn;
			append.Execute();
		}
	}
}
