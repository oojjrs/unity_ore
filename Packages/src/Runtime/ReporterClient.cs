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
                build = application.Build;
                engine = application.Engine;
                platform = application.Platform;
                store = application.Store;
                version = application.Version;
            }
        }

        [Serializable]
        private sealed class DocumentPayload
        {
            public string id;
            public string receivedAtUtc;
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
                message = request.Message;
                name = request.Name;
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
                clientReportId = request.ClientReportId;
                description = request.Description;
                occurredAtUtc = request.OccurredAtUtc.HasValue ? request.OccurredAtUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) : null;
                submitter = (request.Submitter != null) ? new SubmitterPayload(request.Submitter) : null;
                summary = request.Summary;
            }
        }

        [Serializable]
        private sealed class SubmitterPayload
        {
            public string displayName;
            public string id;

            public SubmitterPayload(ReportSubmitter submitter)
            {
                displayName = submitter.DisplayName;
                id = submitter.Id;
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
            if (string.IsNullOrWhiteSpace(rawJson))
                return json;

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

        private static ReporterResponse ParseResponse(string responseBody, long statusCode)
        {
            DocumentPayload payload;

            try
            {
                payload = JsonUtility.FromJson<DocumentPayload>(responseBody);
            }
            catch (ArgumentException e)
            {
                throw new ReporterException("Reporter returned an invalid success response.", statusCode, responseBody, e);
            }

            if ((payload == null) || (Guid.TryParse(payload.id, out var id) == false) || (DateTimeOffset.TryParse(payload.receivedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var receivedAtUtc) == false))
                throw new ReporterException("Reporter returned an invalid success response.", statusCode, responseBody);

            return new ReporterResponse(id, receivedAtUtc);
        }

        private static async Task<ReporterResponse> SendAsync(UnityWebRequest request, CancellationToken cancellationToken)
        {
            await WaitForCompletionAsync(request, cancellationToken);

            var responseBody = (request.downloadHandler != null) ? request.downloadHandler.text : string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
                throw new ReporterException(request.error ?? "Reporter request failed.", request.responseCode, responseBody);

            return ParseResponse(responseBody, request.responseCode);
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

        public async Task<ReporterResponse> SendEventAsync(EventRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var json = AddRawProperty(JsonUtility.ToJson(new EventPayload(request)), "properties", request.PropertiesJson);
            using (var webRequest = new UnityWebRequest(CreateUri(_options, "events"), UnityWebRequest.kHttpVerbPOST))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                webRequest.uploadHandler.contentType = "application/json";
                ConfigureRequest(webRequest);
                return await SendAsync(webRequest, cancellationToken);
            }
        }

        public Task<ReporterResponse> SendEventAsync(string name, string message = null, Func<string> getPropertiesJson = null, CancellationToken cancellationToken = default)
        {
            var request = new EventRequest(name)
            {
                Application = Application,
                Message = message,
                PropertiesJson = (getPropertiesJson != null) ? getPropertiesJson() : null,
                Submitter = Submitter,
            };
            return SendEventAsync(request, cancellationToken);
        }

        public async Task<ReporterResponse> SendReportAsync(ReportRequest request, IReadOnlyList<ReportAttachment> attachments = null, CancellationToken cancellationToken = default)
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

            using (var webRequest = UnityWebRequest.Post(CreateUri(_options, "reports"), sections))
            {
                ConfigureRequest(webRequest);
                return await SendAsync(webRequest, cancellationToken);
            }
        }

        public Task<ReporterResponse> SendReportAsync(Texture2D screenshot, string summary, Func<string> getContextJson = null, CancellationToken cancellationToken = default)
        {
            var request = new ReportRequest(summary)
            {
                Application = Application,
                ClientReportId = Guid.NewGuid().ToString("N"),
                ContextJson = (getContextJson != null) ? getContextJson() : null,
                Submitter = Submitter,
            };
            return SendReportAsync(request, CreateDefaultAttachments(screenshot), cancellationToken);
        }

        public Task<ReporterResponse> SendUxAsync(string message, CancellationToken cancellationToken = default)
        {
            return SendEventAsync(DefaultEventName, message, null, cancellationToken);
        }
    }
}
