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



	}
}
