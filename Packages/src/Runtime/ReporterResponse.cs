using System;

namespace oojjrs.ore
{
    public sealed class ReporterResponse
    {
        public Guid Id { get; }
        public DateTimeOffset ReceivedAtUtc { get; }

        public ReporterResponse(Guid id, DateTimeOffset receivedAtUtc)
        {
            Id = id;
            ReceivedAtUtc = receivedAtUtc;
        }
    }
}
