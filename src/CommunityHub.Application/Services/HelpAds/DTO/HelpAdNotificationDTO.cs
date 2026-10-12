using System;

namespace CommunityHub.Application.Services.HelpAds.DTO
{
    public class HelpAdNotificationDTO
    {
        public long Id { get; set; }
        public long TenantId { get; set; }
        public long HelpAdId { get; set; }
        public long BookingId { get; set; }
        public long BookingTenantId { get; set; }
        public long RequestingHelpAdId { get; set; }
        public int SlotsCount { get; set; }
        public string Type { get; set; } = "SLOT_BOOKED";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
