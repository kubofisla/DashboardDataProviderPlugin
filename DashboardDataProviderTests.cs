using System;
using System.IO;
using System.Reflection;
using SimHub.Plugins.DashboardData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DashboardDataProviderPlugin.Tests
{
    [TestClass]
    public class DashboardDataProviderTests
    {
        private DashboardDataProvider CreateProvider()
        {
            var provider = new DashboardDataProvider();

            // Point settings to a temp file so SaveSettings() doesn't throw.
            var settingsField = typeof(DashboardDataProvider)
                .GetField("_settingsFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
            settingsField.SetValue(provider, Path.GetTempFileName());

            return provider;
        }

        private void SetLatestData(DashboardDataProvider provider, object latestData)
        {
            var latestDataField = typeof(DashboardDataProvider)
                .GetField("_latestData", BindingFlags.NonPublic | BindingFlags.Instance);
            latestDataField.SetValue(provider, latestData);
        }

        private double GetTargetTime(DashboardDataProvider provider)
        {
            var targetTimeField = typeof(DashboardDataProvider)
                .GetField("_targetTime", BindingFlags.NonPublic | BindingFlags.Instance);
            return (double)targetTimeField.GetValue(provider);
        }

        private (object response, int statusCode) InvokeResetCore(DashboardDataProvider provider, string methodName)
        {
            var method = typeof(DashboardDataProvider)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            object[] args = new object[] { null, 0 };
            method.Invoke(provider, args);

            return (args[0], (int)args[1]);
        }

        private double InvokeConvertToSeconds(DashboardDataProvider provider, object input)
        {
            var method = typeof(DashboardDataProvider)
                .GetMethod("ConvertToSeconds", BindingFlags.NonPublic | BindingFlags.Instance);

            return (double)method.Invoke(provider, new object[] { input });
        }

        [TestMethod]
        public void ResetToFast_SetsTargetTime_WhenFastestLapAvailable()
        {
            // Arrange
            var provider = CreateProvider();
            var fastestLap = TimeSpan.FromSeconds(90); // 1:30.000
            SetLatestData(provider, new { FastestLapTime = fastestLap });

            // Act
            var (response, statusCode) = InvokeResetCore(provider, "ResetToFastCore");

            // Assert
            Assert.AreEqual(200, statusCode);

            var statusProp = response.GetType().GetProperty("status");
            var targetTimeProp = response.GetType().GetProperty("targetTime");

            Assert.AreEqual("success", statusProp.GetValue(response));
            Assert.AreEqual(90.0, (double)targetTimeProp.GetValue(response), 0.0001);
            Assert.AreEqual(90.0, GetTargetTime(provider), 0.0001);
        }

        [TestMethod]
        public void ResetToFast_ReturnsError_WhenNoFastestLapAvailable()
        {
            // Arrange
            var provider = CreateProvider();
            // No FastestLapTime in latest data => should trigger error path.
            SetLatestData(provider, new { FastestLapTime = (TimeSpan?)null });

            // Act
            var (response, statusCode) = InvokeResetCore(provider, "ResetToFastCore");

            // Assert
            Assert.AreEqual(400, statusCode);

            var statusProp = response.GetType().GetProperty("status");
            var messageProp = response.GetType().GetProperty("message");

            Assert.AreEqual("error", statusProp.GetValue(response));
            Assert.AreEqual("No fastest lap time available", messageProp.GetValue(response));
            Assert.AreEqual(0.0, GetTargetTime(provider), 0.0001);
        }

        [TestMethod]
        public void ResetToLast_SetsTargetTime_WhenLastLapAvailable()
        {
            // Arrange
            var provider = CreateProvider();
            var lastLap = 75.5; // seconds
            SetLatestData(provider, new { LastLapTime = lastLap });

            // Act
            var (response, statusCode) = InvokeResetCore(provider, "ResetToLastCore");

            // Assert
            Assert.AreEqual(200, statusCode);

            var statusProp = response.GetType().GetProperty("status");
            var targetTimeProp = response.GetType().GetProperty("targetTime");

            Assert.AreEqual("success", statusProp.GetValue(response));
            Assert.AreEqual(75.5, (double)targetTimeProp.GetValue(response), 0.0001);
            Assert.AreEqual(75.5, GetTargetTime(provider), 0.0001);
        }

        [TestMethod]
        public void ResetToLast_ReturnsError_WhenNoLastLapAvailable()
        {
            // Arrange
            var provider = CreateProvider();
            SetLatestData(provider, new { LastLapTime = (double?)null });

            // Act
            var (response, statusCode) = InvokeResetCore(provider, "ResetToLastCore");

            // Assert
            Assert.AreEqual(400, statusCode);

            var statusProp = response.GetType().GetProperty("status");
            var messageProp = response.GetType().GetProperty("message");

            Assert.AreEqual("error", statusProp.GetValue(response));
            Assert.AreEqual("No last lap time available", messageProp.GetValue(response));
            Assert.AreEqual(0.0, GetTargetTime(provider), 0.0001);
        }

        [TestMethod]
        public void ConvertToSeconds_HandlesTimeSpanDoubleAndString()
        {
            // Arrange
            var provider = CreateProvider();

            var ts = TimeSpan.FromSeconds(42);
            double d = 123.45;
            string s = "120"; // integer string avoids culture issues

            // Act
            double tsSeconds = InvokeConvertToSeconds(provider, ts);
            double dSeconds = InvokeConvertToSeconds(provider, d);
            double sSeconds = InvokeConvertToSeconds(provider, s);

            // Assert
            Assert.AreEqual(42.0, tsSeconds, 0.0001, "TimeSpan should convert to TotalSeconds");
            Assert.AreEqual(123.45, dSeconds, 0.0001, "double should pass through unchanged");
            Assert.AreEqual(120.0, sSeconds, 0.0001, "parsable string should convert to double");
        }
    }
}