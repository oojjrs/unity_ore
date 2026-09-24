using System;

namespace oojjrs.ore
{
    public sealed class MatchVerificationRequest
    {
        public ReportApplication Application { get; set; }
        public string ClientMatchVerificationId { get; set; }
        public string ContextJson { get; set; }
        public string MatchId { get; }
        public DateTimeOffset? OccurredAtUtc { get; set; }
        public ReportSubmitter Submitter { get; set; }

        public MatchVerificationRequest(string matchId)
        {
            MatchId = matchId ?? string.Empty;
        }
    }
}
