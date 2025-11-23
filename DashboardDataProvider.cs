using GameReaderCommon;
using Newtonsoft.Json;
using SimHub.Plugins.DataPlugins.PersistantTracker;
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace SimHub.Plugins.DashboardData
{
    /// <summary>
    /// This plugin collects common dashboard telemetry data points and exposes them
    /// as new properties within SimHub. This makes the data easily accessible
    /// for custom dashboards, overlays, or external applications.
    /// </summary>
    [PluginDescription("Forward dashboard information to devices like Loupedeck")]
    [PluginAuthor("Gemini")]
    [PluginName("Dashboard Data Provider")]
    public class DashboardDataProvider : IPlugin, IDataPlugin
    {
        /// <summary>
        /// Instance of the plugin manager.
        /// </summary>
        public PluginManager PluginManager { get; set; }

        private HttpListener _httpListener;
        private Thread _httpThread;
        private object _latestDataLock = new object();
        private object _listenerLock = new object();
        private dynamic _latestData;

        /// <summary>
        /// Called once upon plugin loading. This is where you'll initialize
        /// your properties, settings, and any other one-time setup tasks.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager</param>
        public void Init(PluginManager pluginManager)
        {
            // Start HTTP server
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:8080/dashboarddata/");
            _httpListener.Start();
            _httpThread = new Thread(HttpServerLoop) { IsBackground = true };
            _httpThread.Start();
        
            // Add a simple status property to confirm the plugin is running.
            pluginManager.AddProperty("DashboardData.PluginStatus", this.GetType(), "Initialized");
        }

        /// <summary>
        /// This method is called every time SimHub receives new data from the game (i.e., every frame).
        /// It's the core of the plugin, where you read game data and update your properties.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager</param>
        /// <param name="data">A snapshot of the current game data</param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // We only need to update the data if the game is running.
            if (data.GameRunning)
            {
                // The 'NewData' object contains the most recent telemetry update.
                if (data.NewData != null)
                {
                    lock (_latestDataLock)
                    {
                        // Store only the data you want to expose
                        _latestData = new
                        {
                            LastLapTime = data.NewData.LastLapTime,
                            CurrentLapTime = data.NewData.CurrentLapTime,
                            FastestLapTime = data.NewData.BestLapTime,
                            CompactDelta = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.CompactDelta"),
                            SessionBestLiveDeltaSeconds = pluginManager.GetPropertyValue("PersistantTrackerPlugin.SessionBestLiveDeltaSeconds").ToString(),
                            

                            // Add more fields as needed
                        };
                    }
                }
                // Update the status to show it's actively running.
                pluginManager.SetPropertyValue("DashboardData.PluginStatus", this.GetType(), "Running");
            }
            else
            {
                // If the game is not running, set status to indicate that.
                pluginManager.SetPropertyValue("DashboardData.PluginStatus", this.GetType(), "Game Not Running");
            }
        }

        private void HttpServerLoop()
        {
            while (_httpListener.IsListening)
            {
                try
                {
                    var context = _httpListener.GetContext();
                    if (context.Request.HttpMethod == "GET")
                    {
                        object dataToSend;
                        lock (_latestDataLock)
                        {
                            dataToSend = _latestData ?? new { };
                        }
                        string json = JsonConvert.SerializeObject(dataToSend);
                        byte[] buffer = Encoding.UTF8.GetBytes(json);
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                    }
                }
                catch (Exception)
                {
                    // Handle exceptions/logging as needed
                }
            }
        }

        /// <summary>
        /// Called when the plugin is being unloaded (e.g., when SimHub is closing).
        /// Use this to clean up any resources if needed.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager</param>
        public void End(PluginManager pluginManager)
        {
            // No cleanup necessary for this simple plugin.

            if (_httpListener != null)
            {
                _httpListener.Stop();
                _httpListener.Close();
            }
            if (_httpThread != null && _httpThread.IsAlive)
            {
                _httpThread.Join(1000);
            }

            // Update the status to show it's stopped.
            pluginManager.SetPropertyValue("DashboardData.PluginStatus", this.GetType(), "Stopped");
        }
    }
}
