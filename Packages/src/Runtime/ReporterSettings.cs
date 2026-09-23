using UnityEngine;

namespace oojjrs.ore
{
    [CreateAssetMenu(fileName = "ReporterSettings", menuName = "Ore/Reporter Settings")]
    public sealed class ReporterSettings : ScriptableObject
    {
        [SerializeField]
        private string _baseUrl = "https://oojjrs-reporter.azurewebsites.net";
        [SerializeField]
        private string _ingestionToken;
        [SerializeField]
        private string _projectKey;
        [SerializeField]
        private int _timeoutSeconds = 30;

        public string BaseUrl => _baseUrl ?? string.Empty;
        public string IngestionToken => _ingestionToken ?? string.Empty;
        public string ProjectKey => _projectKey ?? string.Empty;
        public int TimeoutSeconds => _timeoutSeconds;
    }
}
