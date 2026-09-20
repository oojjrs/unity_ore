using System;

namespace oojjrs.ore
{
    public sealed class ReportRequest
    {
        public ReportApplication Application { get; set; }
        public string ClientReportId { get; set; }
        public string ContextJson { get; set; }
        public string Description { get; set; }
        public DateTimeOffset? OccurredAtUtc { get; set; }
        public ReportSubmitter Submitter { get; set; }
        public string Summary { get; }

        public ReportRequest(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                throw new ArgumentException("The report summary is required.", nameof(summary));

            Summary = summary;
        }
    }
}
