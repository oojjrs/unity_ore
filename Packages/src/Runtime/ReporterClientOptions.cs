using System;

namespace oojjrs.ore
{
    public sealed class ReporterClientOptions
    {
        public string BaseUrl { get; }
        public string IngestionToken { get; }
        public string ProjectKey { get; }
        public int TimeoutSeconds { get; }

        public ReporterClientOptions(string baseUrl, string projectKey, string ingestionToken, int timeoutSeconds = 30)
        {
            if ((Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) == false) || ((baseUri.Scheme != Uri.UriSchemeHttp) && (baseUri.Scheme != Uri.UriSchemeHttps)))
                throw new ArgumentException("The Reporter base URL must be an absolute HTTP or HTTPS URL.", nameof(baseUrl));
            if (IsProjectKey(projectKey) == false)
                throw new ArgumentException("The project key must contain only lowercase letters, digits, or hyphens and must not exceed 63 characters.", nameof(projectKey));
            if (string.IsNullOrWhiteSpace(ingestionToken))
                throw new ArgumentException("The ingestion token is required.", nameof(ingestionToken));
            if (timeoutSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            BaseUrl = baseUrl.TrimEnd('/');
            IngestionToken = ingestionToken;
            ProjectKey = projectKey;
            TimeoutSeconds = timeoutSeconds;
        }

        private static bool IsProjectKey(string projectKey)
        {
            if (string.IsNullOrEmpty(projectKey) || (projectKey.Length > 63))
                return false;

            for (var index = 0; index < projectKey.Length; ++index)
            {
                var character = projectKey[index];
                if (((character >= 'a') && (character <= 'z')) || ((character >= '0') && (character <= '9')) || (character == '-'))
                    continue;

                return false;
            }

            return true;
        }
    }
}
