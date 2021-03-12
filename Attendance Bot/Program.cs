using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace TrackerBot
{
	class Program
	{

		private DiscordSocketClient client;
		private CommandService commands = new CommandService(new CommandServiceConfig
		{
			LogLevel = LogSeverity.Info,
			CaseSensitiveCommands = false
		});
		private IServiceProvider services;


		static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

		public async Task MainAsync()
		{
			client = new DiscordSocketClient();
			client.Log += Log;
			services = new Initialize(commands, client).BuildServiceProvider();

			var token = File.ReadAllText("token.txt"); //Taken from a local file called token.txt, don't want to publish my token :)

			CommandHandler commandHandler = new CommandHandler(services, client, commands);
			await commandHandler.InstallCommandsAsync();

			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			client.Ready += Done;

			await Task.Delay(-1);
		}

		private async Task Done()
		{
			var chnl = client.GetChannel(783853270229450772) as IMessageChannel;
			if (chnl != null) await chnl.SendMessageAsync("Attendance Tracker has opened.");
		}

		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}
	}

	/**
	 * Taken from https://docs.stillu.cc/guides/commands/intro.html
	 */
	public class CommandHandler
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly IServiceProvider _services;

		public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService commands)
		{
			_services = services;
			_commands = commands;
			_client = client;
		}

		public async Task InstallCommandsAsync()
		{
			_client.MessageReceived += HandleCommandAsync;

			await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _services);
		}

		private async Task HandleCommandAsync(SocketMessage messageParam)
		{
			var message = messageParam as SocketUserMessage;
			if (message == null) return;

			//Console.WriteLine("Received input: " + message.ToString());

			int argPos = 0;

			if (!(message.HasCharPrefix('!', ref argPos)) || message.Author.IsBot) return;

			//Console.WriteLine("Parsing Command.");

			var context = new SocketCommandContext(_client, message);

			await _commands.ExecuteAsync(
				context: context,
				argPos: argPos,
				services: _services
				);



		}
	}

	public class Initialize
	{
		private readonly CommandService commands;
		private readonly DiscordSocketClient client;

		public Initialize(CommandService comm = null, DiscordSocketClient cli = null)
		{
			commands = comm ?? new CommandService();
			client = cli ?? new DiscordSocketClient();
		}

		public IServiceProvider BuildServiceProvider() => new ServiceCollection()
			.AddSingleton(client)
			.AddSingleton(commands)
			.BuildServiceProvider();
	}
}
