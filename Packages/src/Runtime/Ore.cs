using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace oojjrs.ore
{
    public static class Ore
    {
        private const string SettingsResourcePath = "ReporterSettings";

        private static ReporterClient _client;

        public static string Id { get; set; }
        public static string Nickname { get; set; }
        public static string StoreType { get; set; }

        private static ReporterClient GetClient()
        {
            if (_client == null)
            {
                var settings = Resources.Load<ReporterSettings>(SettingsResourcePath);
                if (settings == null)
                    throw new InvalidOperationException("ReporterSettings was not found. Create it at Assets/Resources/ReporterSettings.asset.");

                _client = ReporterClient.CreateUnity(settings.BaseUrl, settings.ProjectKey, settings.IngestionToken, StoreType, Id, Nickname, settings.TimeoutSeconds);
            }

            _client.Application.Store = StoreType;
            _client.Submitter.DisplayName = Nickname;
            _client.Submitter.Id = Id;
            return _client;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _client = null;
            Id = null;
            Nickname = null;
            StoreType = null;
        }

        public static Task<ReporterResponse> SendEventAsync(string name, string message = null, Func<string> getPropertiesJson = null, CancellationToken cancellationToken = default)
        {
            return GetClient().SendEventAsync(name, message, getPropertiesJson, cancellationToken);
        }

        public static Task<ReporterResponse> SendReportAsync(Texture2D screenshot, string summary, Func<string> getContextJson = null, CancellationToken cancellationToken = default)
        {
            return GetClient().SendReportAsync(screenshot, summary, getContextJson, cancellationToken);
        }

        public static Task<ReporterResponse> SendUxAsync(string message, CancellationToken cancellationToken = default)
        {
            return GetClient().SendUxAsync(message, cancellationToken);
        }
    }
}
