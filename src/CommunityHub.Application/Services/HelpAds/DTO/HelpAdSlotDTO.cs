using System;

namespace CommunityHub.Application.Services.HelpAds.DTO
{
    public class HelpAdSlotDTO
    {
        public long Id { get; set; }
        public long HelpAdId { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
