using System;
using System.Threading;
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

        public static Texture2D GetScreenshotAsJpeg()
        {
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if ((screenshot.width <= 1920) && (screenshot.height <= 1080))
                return screenshot;

            var activeRenderTexture = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D resizedTexture = null;
            try
            {
                var scale = Mathf.Min(1920f / screenshot.width, 1080f / screenshot.height);
                var width = Mathf.RoundToInt(screenshot.width * scale);
                var height = Mathf.RoundToInt(screenshot.height * scale);
                renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
                resizedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

                Graphics.Blit(screenshot, renderTexture);
                RenderTexture.active = renderTexture;
                resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                resizedTexture.Apply();
                return resizedTexture;
            }
            catch
            {
                if (resizedTexture != null)
                    UnityEngine.Object.Destroy(resizedTexture);

                throw;
            }
            finally
            {
                RenderTexture.active = activeRenderTexture;
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);

                UnityEngine.Object.Destroy(screenshot);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _client = null;
            Id = null;
            Nickname = null;
            StoreType = null;
        }

        public static void SendEvent(string name, string message = null, Func<string> getPropertiesJson = null, CancellationToken cancellationToken = default)
        {
            GetClient().SendEvent(name, message, getPropertiesJson, cancellationToken);
        }

        public static void SendMatchVerification(string matchId, byte[] data, Func<string> getContextJson = null, CancellationToken cancellationToken = default)
        {
            GetClient().SendMatchVerification(matchId, data, getContextJson, cancellationToken);
        }

        public static void SendReport(Texture2D screenshot, string summary, Func<string> getContextJson = null, CancellationToken cancellationToken = default)
        {
            GetClient().SendReport(screenshot, summary, getContextJson, cancellationToken);
        }

        public static void SendUx(string message, CancellationToken cancellationToken = default)
        {
            GetClient().SendUx(message, cancellationToken);
        }
    }
}
