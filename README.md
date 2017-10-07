# Connection.Limit

# Connection.Limit plugin for TShock
A basic connection limiter for denying multiple connections from the same ip

# Install
Copy the xxConnection.Limit.dll to your ServerPlugins folder. 

**Note:** This plugin will not work for you if the server is behind a proxy

# Configuration
Upon the first run the plugin will generate it's config file at /tshock/connection_limit/config.json. Inside you can:
* alter how many connections are allowed per ip (defaults to 1)
* disable/enable the plugin (defaults to true)
* recalibration timeout - how often the plugin will sync connected clients and check for connections exceeding the limits (default is 5000)

# Commands
cl-stats - Displays how many connections have been handled
cl-toggle - Toggles the plugin on and off
