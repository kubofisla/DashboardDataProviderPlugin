using GameReaderCommon;
using Newtonsoft.Json;
using SimHub.Plugins.DataPlugins.PersistantTracker;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

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
        private object _targetTimeLock = new object();
        private dynamic _latestData;
        private double _targetTime = 0.0;
        private string _settingsFilePath;

        /// <summary>
        /// Called once upon plugin loading. This is where you'll initialize
        /// your properties, settings, and any other one-time setup tasks.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager</param>
        public void Init(PluginManager pluginManager)
        {
            // Setup settings file path
            string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimHub", "DashboardDataProvider");
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }
            _settingsFilePath = Path.Combine(settingsDir, "settings.json");
            LoadSettings();

            // Start HTTP server
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:8080/dashboarddata/");
            _httpListener.Start();
            _httpThread = new Thread(HttpServerLoop) { IsBackground = true };
            _httpThread.Start();
        
            // Add properties
            pluginManager.AddProperty("DashboardData.PluginStatus", this.GetType(), "Initialized");
            pluginManager.AddProperty("DashboardData.TargetTime", this.GetType(), _targetTime);
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
                            TargetTime = GetTargetTime(),

                            // Add more fields as needed
                        };
                    }
                }
                // Update the status to show it's actively running.
                pluginManager.SetPropertyValue("DashboardData.PluginStatus", this.GetType(), "Running");
                pluginManager.SetPropertyValue("DashboardData.TargetTime", this.GetType(), GetTargetTime());
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
                        HandleGetRequest(context);
                    }
                    else if (context.Request.HttpMethod == "POST")
                    {
                        HandlePostRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 405;
                        context.Response.Close();
                    }
                }
                catch (Exception)
                {
                    // Handle exceptions/logging as needed
                }
            }
        }

        private void HandleGetRequest(HttpListenerContext context)
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
            context.Response.StatusCode = 200;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private void HandlePostRequest(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath.ToLower();
                
                if (path.EndsWith("/settarget"))
                {
                    string body;
                    using (var reader = new System.IO.StreamReader(context.Request.InputStream))
                    {
                        body = reader.ReadToEnd();
                    }

                    dynamic json = JsonConvert.DeserializeObject(body);
                    double newTargetTime = (double)json.targetTime;
                    SetTargetTime(newTargetTime);

                    var response = new { status = "success", targetTime = GetTargetTime() };
                    SendJsonResponse(context, response, 200);
                }
                else if (path.EndsWith("/adjust"))
                {
                    string body;
                    using (var reader = new System.IO.StreamReader(context.Request.InputStream))
                    {
                        body = reader.ReadToEnd();
                    }

                    dynamic json = JsonConvert.DeserializeObject(body);
                    double delta = (double)json.delta;
                    AdjustTargetTime(delta);

                    var response = new { status = "success", targetTime = GetTargetTime() };
                    SendJsonResponse(context, response, 200);
                }
                else if (path.EndsWith("/resettofast"))
                {
                    string body;
                    using (var reader = new System.IO.StreamReader(context.Request.InputStream))
                    {
                        body = reader.ReadToEnd();
                    }

                    dynamic json = JsonConvert.DeserializeObject(body);
                    double fastestLapTime = (double)json.fastestLapTime;
                    SetTargetTime(fastestLapTime);

                    var response = new { status = "success", targetTime = GetTargetTime(), resetTo = "fastest" };
                    SendJsonResponse(context, response, 200);
                }
                else if (path.EndsWith("/resettolast"))
                {
                    string body;
                    using (var reader = new System.IO.StreamReader(context.Request.InputStream))
                    {
                        body = reader.ReadToEnd();
                    }

                    dynamic json = JsonConvert.DeserializeObject(body);
                    double lastLapTime = (double)json.lastLapTime;
                    SetTargetTime(lastLapTime);

                    var response = new { status = "success", targetTime = GetTargetTime(), resetTo = "last" };
                    SendJsonResponse(context, response, 200);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                var response = new { status = "error", message = ex.Message };
                SendJsonResponse(context, response, 400);
            }
        }

        private void SendJsonResponse(HttpListenerContext context, object data, int statusCode)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = statusCode;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private double GetTargetTime()
        {
            lock (_targetTimeLock)
            {
                return _targetTime;
            }
        }

        private void SetTargetTime(double value)
        {
            lock (_targetTimeLock)
            {
                _targetTime = Math.Max(0, value);
                SaveSettings();
            }
        }

        private void AdjustTargetTime(double delta)
        {
            lock (_targetTimeLock)
            {
                _targetTime = Math.Max(0, _targetTime + delta);
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new { targetTime = _targetTime };
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail on settings save
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    dynamic settings = JsonConvert.DeserializeObject(json);
                    _targetTime = (double)settings.targetTime;
                }
            }
            catch (Exception)
            {
                _targetTime = 0.0;
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
