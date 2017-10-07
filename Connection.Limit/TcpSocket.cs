using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Terraria;
using Terraria.Localization;
using Terraria.Net;
using Terraria.Net.Sockets;
using TShockAPI;

namespace Connection.Limit
{
	public class TcpSocket : ISocket
	{
		private TcpClient _connection;

		private TcpListener _listener;

		private SocketConnectionAccepted _listenerCallback;

		private RemoteAddress _remoteAddress;

		private bool _isListening;

		private Limiter _limiter;
		private Config _config;
		private string _ip;

		public Limiter Limiter => _limiter;

		public string IpAddress => _ip;
		
		public TcpSocket(Config config)
		{
			_config = config;
			this._connection = new TcpClient();
			this._connection.NoDelay = true;	
		}

		public TcpSocket(Config config, Limiter limiter, TcpClient tcpClient)
		{
			_config = config;
			_limiter = limiter;
			this._connection = tcpClient;
			this._connection.NoDelay = true;
			IPEndPoint iPEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
			this._remoteAddress = new TcpAddress(iPEndPoint.Address, iPEndPoint.Port);
			_ip = iPEndPoint.Address.ToString();
		}

		void ISocket.Close()
		{
			this._remoteAddress = null;
			this._connection.Close();

			if (!string.IsNullOrWhiteSpace(_ip))
			{
				_limiter?.Unregister(_ip);
			}
		}

		bool ISocket.IsConnected()
		{
			return this._connection != null && this._connection.Client != null && this._connection.Connected;
		}

		void ISocket.Connect(RemoteAddress address)
		{
			TcpAddress tcpAddress = (TcpAddress)address;
			this._connection.Connect(tcpAddress.Address, tcpAddress.Port);
			this._remoteAddress = address;
		}

		private void ReadCallback(IAsyncResult result)
		{
			Tuple<SocketReceiveCallback, object> tuple = (Tuple<SocketReceiveCallback, object>)result.AsyncState;

			try
			{
				tuple.Item1(tuple.Item2, this._connection.GetStream().EndRead(result));
			}
			catch (InvalidOperationException)
			{
				// This is common behaviour during client disconnects
				((ISocket)this).Close();
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		private void SendCallback(IAsyncResult result)
		{
			object[] expr_0B = (object[])result.AsyncState;
			LegacyNetBufferPool.ReturnBuffer((byte[])expr_0B[1]);
			Tuple<SocketSendCallback, object> tuple = (Tuple<SocketSendCallback, object>)expr_0B[0];
			try
			{
				this._connection.GetStream().EndWrite(result);
				tuple.Item1(tuple.Item2);
			}
			catch (Exception)
			{
				((ISocket)this).Close();
			}
		}

		void ISocket.SendQueuedPackets()
		{
		}

		void ISocket.AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
		{
			byte[] array = LegacyNetBufferPool.RequestBuffer(data, offset, size);
			this._connection.GetStream().BeginWrite(array, 0, size, new AsyncCallback(this.SendCallback), new object[]
			{
				new Tuple<SocketSendCallback, object>(callback, state),
				array
			});
		}

		void ISocket.AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state)
		{
			this._connection.GetStream().BeginRead(data, offset, size, new AsyncCallback(this.ReadCallback), new Tuple<SocketReceiveCallback, object>(callback, state));
		}

		bool ISocket.IsDataAvailable()
		{
			return this._connection.GetStream().DataAvailable;
		}

		RemoteAddress ISocket.GetRemoteAddress()
		{
			return this._remoteAddress;
		}

		bool ISocket.StartListening(SocketConnectionAccepted callback)
		{
			IPAddress any = IPAddress.Any;
			string ipString;
			if (Program.LaunchParameters.TryGetValue("-ip", out ipString) && !IPAddress.TryParse(ipString, out any))
			{
				any = IPAddress.Any;
			}
			this._isListening = true;
			this._listenerCallback = callback;
			if (this._listener == null)
			{
				this._listener = new TcpListener(any, Netplay.ListenPort);
			}
			try
			{
				this._listener.Start();
			}
			catch (Exception)
			{
				return false;
			}

			_limiter = new Limiter(_config);

			new Thread(this.ListenLoop).Start();
			return true;
		}

		void ISocket.StopListening()
		{
			this._isListening = false;
		}

		private void ListenLoop(object unused)
		{
			while (this._isListening && !Netplay.disconnect)
			{
				try
				{
					var client = this._listener.AcceptTcpClient();
					var ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

					if(_config.Enabled && !_limiter.Register(ip))
					{
						client.Close();
						continue;
					}

					ISocket socket = new TcpSocket(_config, _limiter, client);
					Console.WriteLine($"{ConnectionLimitPlugin.ConsoleTag} {Language.GetTextValue("Net.ClientConnecting", socket.GetRemoteAddress())}");
					this._listenerCallback(socket);
				}
				catch (Exception)
				{
				}
			}
			this._listener.Stop();

			// currently vanilla will stop listening when the slots are full, however it appears that this Netplay.IsListening
			// flag is still set, making the server loop beleive it's still listening when it's actually not.
			// clearing this flag when we actually have stopped will allow the ServerLoop to start listening again when
			// there are enough slots available.
			Netplay.IsListening = false;
		}
	}
}
