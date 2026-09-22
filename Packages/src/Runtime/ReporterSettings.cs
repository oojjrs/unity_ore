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

        public string BaseUrl => _baseUrl;
        public string IngestionToken => _ingestionToken;
        public string ProjectKey => _projectKey;
        public int TimeoutSeconds => _timeoutSeconds;
    }
}
