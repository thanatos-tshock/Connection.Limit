using System;
using System.Collections.Generic;

namespace Connection.Limit
{
	public class Limiter
	{
		private Dictionary<string, int> _registry = new Dictionary<string, int>();
		private Config _config;
		private DateTime? _lastCalibration;

		private long _blockedConnections = 0;
		public long BlockedConnections
		{
			get
			{
				return System.Threading.Interlocked.Read(ref _blockedConnections);
			}
		}

		private long _acceptedConnections = 0;
		public long AcceptedConnections
		{
			get
			{
				return System.Threading.Interlocked.Read(ref _acceptedConnections);
			}
		}

		public Limiter(Config config)
		{
			_config = config;
		}

		public bool Register(string ip)
		{
			bool allow = true;

			if (CanRecalibrate())
				Recalibrate();

			lock (_registry)
			{
				int number;
				if (_registry.TryGetValue(ip, out number))
				{
					allow = number < _config.MaxConnectionsPerIp;

					if (allow)
					{
						_registry[ip]++;
					}
				}
				else
				{
					_registry.Add(ip, 1);
				}
			}

			if (allow)
			{
				System.Threading.Interlocked.Increment(ref _acceptedConnections);
			}
			else
			{
				System.Threading.Interlocked.Increment(ref _blockedConnections);
			}

			return allow;
		}

		public void Unregister(string ip)
		{
			lock (_registry)
			{
				int number;
				if (_registry.TryGetValue(ip, out number))
				{
					if (number <= 0)
					{
						System.Diagnostics.Debug.WriteLine("Tried decreasing an ip limit when the number is already 0");
					}
					else
					{
						_registry[ip]--;
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("Tried decreasing an ip limit when there was no registration");
				}
			}
		}

		public bool CanRecalibrate() => _lastCalibration == null || (DateTime.Now - _lastCalibration.Value).TotalMilliseconds >= _config.RecalibrationTimeMilliseconds;

		public void Recalibrate()
		{
			System.Diagnostics.Debug.WriteLine("Recalibrating");
			lock (_registry)
			{
				_registry.Clear();

				foreach (var client in Terraria.Netplay.Clients)
				{
					if (client != null && client.IsActive)
					{
						var socket = client.Socket as TcpSocket;
						if (socket != null)
						{
							var ip = socket.IpAddress;
							int number;
							if (_registry.TryGetValue(ip, out number))
							{
								_registry[ip]++;
							}
							else
							{
								_registry.Add(ip, 1);
							}
						}
					}
				}

				_lastCalibration = DateTime.Now;
			}
		}
	}
}
