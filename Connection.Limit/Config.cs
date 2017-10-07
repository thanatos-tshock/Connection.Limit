using Newtonsoft.Json;
using System;

namespace Connection.Limit
{
	public class Config
	{
		[JsonProperty("enabled")]
		public bool Enabled { get; set; } = true;

		[JsonProperty("max-connections-per-ip")]
		public int MaxConnectionsPerIp { get; set; } = 1;

		[JsonProperty("recalibration-time-milliseconds")]
		public int RecalibrationTimeMilliseconds { get; set; } = 5000;

		public void Verify()
		{
			if (MaxConnectionsPerIp < 1)
			{
				Console.WriteLine($"{MaxConnectionsPerIp} is an invalid value for config property: max-connections-per-ip. Minimum value is 1.");
				MaxConnectionsPerIp = 1;
			}

			if (RecalibrationTimeMilliseconds < 1000)
			{
				Console.WriteLine($"{RecalibrationTimeMilliseconds} is an invalid value for config property: recalibration-time-milliseconds. Minimum value is 1000.");
				RecalibrationTimeMilliseconds = 1000;
			}
		}
	}
}
