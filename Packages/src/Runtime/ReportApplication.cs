namespace oojjrs.ore
{
    public sealed class ReportApplication
    {
        public string Build { get; set; }
        public string Engine { get; set; }
        public string Platform { get; set; }
        public string Store { get; set; }
        public string Version { get; set; }

        public static ReportApplication CreateUnity(string store = null)
        {
            return new ReportApplication
            {
                Build = UnityEngine.Application.buildGUID,
                Engine = $"Unity {UnityEngine.Application.unityVersion}",
                Platform = UnityEngine.Application.platform.ToString(),
                Store = store ?? string.Empty,
                Version = UnityEngine.Application.version,
            };
        }
    }
}
