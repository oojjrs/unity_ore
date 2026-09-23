using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace oojjrs.ore
{
    public sealed class ReporterClient
    {
        private const string DefaultEventName = "ux";
        private const string EmptyJsonObject = "{}";
        private const int MaxPlayerLogBytes = 2 * 1024 * 1024;

        [Serializable]
        private sealed class ApplicationPayload
        {
            public string build;
            public string engine;
            public string platform;
            public string store;
            public string version;

            public ApplicationPayload(ReportApplication application)
            {
                build = ToPayloadString(application.Build);
                engine = ToPayloadString(application.Engine);
                platform = ToPayloadString(application.Platform);
                store = ToPayloadString(application.Store);
                version = ToPayloadString(application.Version);
            }
        }

        [Serializable]
        private sealed class EventPayload
        {
            public ApplicationPayload application;
            public string message;
            public string name;
            public string occurredAtUtc;
            public SubmitterPayload submitter;

            public EventPayload(EventRequest request)
            {
                application = (request.Application != null) ? new ApplicationPayload(request.Application) : null;
                message = ToPayloadString(request.Message);
                name = ToPayloadString(request.Name);
                occurredAtUtc = request.OccurredAtUtc.HasValue ? request.OccurredAtUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) : null;
                submitter = (request.Submitter != null) ? new SubmitterPayload(request.Submitter) : null;
            }
        }

        [Serializable]
        private sealed class ReportPayload
        {
            public ApplicationPayload application;
            public string clientReportId;
            public string description;
            public string occurredAtUtc;
            public SubmitterPayload submitter;
            public string summary;

            public ReportPayload(ReportRequest request)
            {
                application = (request.Application != null) ? new ApplicationPayload(request.Application) : null;
                clientReportId = ToPayloadString(request.ClientReportId);
                description = ToPayloadString(request.Description);
                occurredAtUtc = request.OccurredAtUtc.HasValue ? request.OccurredAtUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) : null;
                submitter = (request.Submitter != null) ? new SubmitterPayload(request.Submitter) : null;
                summary = ToPayloadString(request.Summary);
            }
        }

        [Serializable]
        private sealed class SubmitterPayload
        {
            public string displayName;
            public string id;

            public SubmitterPayload(ReportSubmitter submitter)
            {
                displayName = ToPayloadString(submitter.DisplayName);
                id = ToPayloadString(submitter.Id);
            }
        }

        private readonly ReporterClientOptions _options;

        public ReportApplication Application { get; set; }
        public ReporterClientOptions Options => _options;
        public ReportSubmitter Submitter { get; set; }

        public ReporterClient(ReporterClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            Application = ReportApplication.CreateUnity();
        }

        private static string AddRawProperty(string json, string propertyName, string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson) || string.Equals(rawJson.Trim(), "null", StringComparison.Ordinal))
                rawJson = EmptyJsonObject;

            return json.Insert(json.Length - 1, $",\"{propertyName}\":{rawJson}");
        }

        private static string CreateUri(ReporterClientOptions options, string collection)
        {
            return $"{options.BaseUrl}/api/v1/projects/{Uri.EscapeDataString(options.ProjectKey)}/{collection}";
        }

        private static IReadOnlyList<ReportAttachment> CreateDefaultAttachments(Texture2D screenshot)
        {
            var attachments = new List<ReportAttachment>();
            if (screenshot != null)
                attachments.Add(ReportAttachment.CreateJpeg("screenshot.jpg", screenshot));

            var playerLog = CreatePlayerLogAttachment();
            if (playerLog != null)
                attachments.Add(playerLog);

            return attachments;
        }

        private static ReportAttachment CreatePlayerLogAttachment()
        {
            var path = UnityEngine.Application.consoleLogPath;
            if (string.IsNullOrWhiteSpace(path) || (File.Exists(path) == false))
                return null;

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var length = (int)Math.Min(stream.Length, MaxPlayerLogBytes);
                    var data = new byte[length];
                    stream.Seek(-length, SeekOrigin.End);

                    var offset = 0;
                    while (offset < length)
                    {
                        var read = stream.Read(data, offset, length - offset);
                        if (read == 0)
                            break;

                        offset += read;
                    }

                    if (offset == length)
                        return new ReportAttachment("Player.log", data, "text/plain; charset=utf-8");

                    var partialData = new byte[offset];
                    Array.Copy(data, partialData, offset);
                    return new ReportAttachment("Player.log", partialData, "text/plain; charset=utf-8");
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        public static ReporterClient CreateUnity(string baseUrl, string projectKey, string ingestionToken, string store = null, string submitterId = null, string submitterDisplayName = null, int timeoutSeconds = 30)
        {
            return new ReporterClient(new ReporterClientOptions(baseUrl, projectKey, ingestionToken, timeoutSeconds))
            {
                Application = ReportApplication.CreateUnity(store),
                Submitter = new ReportSubmitter
                {
                    DisplayName = submitterDisplayName,
                    Id = submitterId,
                },
            };
        }

        private static async void SendAsync(UnityWebRequest request, CancellationToken cancellationToken)
        {
            using (request)
            {
                try
                {
                    await WaitForCompletionAsync(request, cancellationToken);

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        var responseBody = (request.downloadHandler != null) ? request.downloadHandler.text : string.Empty;
                        throw new ReporterException(request.error ?? "Reporter request failed.", request.responseCode, responseBody);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static string ToPayloadString(string value)
        {
            return value ?? string.Empty;
        }

        private static async Task WaitForCompletionAsync(UnityWebRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operation = request.SendWebRequest();
            operation.completed += _ => completionSource.TrySetResult(true);

            if (cancellationToken.CanBeCanceled == false)
            {
                await completionSource.Task;
                return;
            }

            var cancellationSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancellationSource.TrySetResult(true)))
            {
                var completedTask = await Task.WhenAny(completionSource.Task, cancellationSource.Task);
                if (completedTask == cancellationSource.Task)
                {
                    request.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await completionSource.Task;
            }
        }

        private void ConfigureRequest(UnityWebRequest request)
        {
            request.timeout = _options.TimeoutSeconds;
            request.SetRequestHeader("Authorization", $"Bearer {_options.IngestionToken}");
        }

        public void SendEvent(EventRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var json = AddRawProperty(JsonUtility.ToJson(new EventPayload(request)), "properties", request.PropertiesJson);
            var webRequest = new UnityWebRequest(CreateUri(_options, "events"), UnityWebRequest.kHttpVerbPOST);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            webRequest.uploadHandler.contentType = "application/json";
            ConfigureRequest(webRequest);
            SendAsync(webRequest, cancellationToken);
        }

        public void SendEvent(string name, string message = null, Func<string> getPropertiesJson = null, CancellationToken cancellationToken = default)
        {
            var request = new EventRequest(name)
            {
                Application = Application,
                Message = message,
                PropertiesJson = (getPropertiesJson != null) ? getPropertiesJson() : null,
                Submitter = Submitter,
            };
            SendEvent(request, cancellationToken);
        }

        public void SendReport(ReportRequest request, IReadOnlyList<ReportAttachment> attachments = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var reportJson = AddRawProperty(JsonUtility.ToJson(new ReportPayload(request)), "context", request.ContextJson);
            var sections = new List<IMultipartFormSection>((attachments != null) ? attachments.Count + 1 : 1)
            {
                new MultipartFormDataSection("report", reportJson, Encoding.UTF8, "application/json"),
            };

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment == null)
                        throw new ArgumentException("Attachments must not contain null entries.", nameof(attachments));

                    sections.Add(new MultipartFormFileSection("attachments", attachment.Data, attachment.FileName, attachment.ContentType));
                }
            }

            var webRequest = UnityWebRequest.Post(CreateUri(_options, "reports"), sections);
            ConfigureRequest(webRequest);
            SendAsync(webRequest, cancellationToken);
        }

        public void SendReport(Texture2D screenshot, string summary, Func<string> getContextJson = null, CancellationToken cancellationToken = default)
        {
            var request = new ReportRequest(summary)
            {
                Application = Application,
                ClientReportId = Guid.NewGuid().ToString("N"),
                ContextJson = (getContextJson != null) ? getContextJson() : null,
                Submitter = Submitter,
            };
            SendReport(request, CreateDefaultAttachments(screenshot), cancellationToken);
        }

        public void SendUx(string message, CancellationToken cancellationToken = default)
        {
            SendEvent(DefaultEventName, message, null, cancellationToken);
        }
    }
}
