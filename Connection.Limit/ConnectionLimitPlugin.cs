using Newtonsoft.Json;
using OTAPI;
using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Connection.Limit
{
	[ApiVersion(2, 1)]
	public class ConnectionLimitPlugin : TerrariaPlugin
	{
		internal const String PluginName = "Connection.Limit";
		internal const String ConsoleTag = "[" + PluginName + "]: ";

		public override string Author => "thanatos";
		public override string Description => "Limits the amount of connections per ip";
		public override string Name => PluginName;
		public override Version Version => typeof(ConnectionLimitPlugin).Assembly.GetName().Version;

		private Config _config;
		private TcpSocket _socket;

		public ConnectionLimitPlugin(Main game) : base(game)
		{
			this.Order = 1000;
		}

		void LoadConfig()
		{
			string
				directory = Path.Combine("tshock", "connection_limit"),
				filepath = Path.Combine(directory, "config.json")
			;
			Directory.CreateDirectory(directory);

			if (File.Exists(filepath))
			{
				_config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filepath));
			}
			else
			{
				var config = new Config();
				File.WriteAllText(filepath, JsonConvert.SerializeObject(config, Formatting.Indented));
				_config = config;
			}

			_config.Verify();
		}

		public override void Initialize()
		{
			LoadConfig();

			if (!_config.Enabled)
			{
				Console.WriteLine($"{ConsoleTag} disabled by configuration file.");
				return;
			}

			Commands.ChatCommands.Add(new Command("connection.limit.toggle", (CommandArgs args) =>
			{
				this._config.Enabled = !this._config.Enabled;
				args.Player.SendInfoMessage($"{PluginName} is {(this._config.Enabled ? "enabled" : "disabled")}");
			}, new[]
			{
				"cl-toggle"
			}));
			Commands.ChatCommands.Add(new Command("connection.limit.stats", (CommandArgs args) =>
			{
				long accepted = _socket?.Limiter?.AcceptedConnections ?? 0L;
				long blocked = _socket?.Limiter?.BlockedConnections ?? 0L;
				Console.WriteLine($"{ConsoleTag} Accepted connections for current session: {accepted}");
				Console.WriteLine($"{ConsoleTag} Blocked connections for current session: {blocked}");
			}, new[]
			{
				"cl-stats"
			}));

			Hooks.World.IO.PreLoadWorld = (ref bool loadFromCloud) =>
			{
				Hooks.Net.Socket.Create = CreateSocket;
				return HookResult.Continue;
			};

			Console.WriteLine($"{ConsoleTag} initialised");
		}

		TcpSocket CreateSocket()
		{
			return _socket ?? (_socket = new TcpSocket(_config));
		}
	}
}
