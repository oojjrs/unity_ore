using System;

namespace oojjrs.ore
{
    public sealed class EventRequest
    {
        public ReportApplication Application { get; set; }
        public string Message { get; set; }
        public string Name { get; }
        public DateTimeOffset? OccurredAtUtc { get; set; }
        public string PropertiesJson { get; set; }
        public ReportSubmitter Submitter { get; set; }

        public EventRequest(string name)
        {
            Name = name ?? string.Empty;
        }
    }
}
