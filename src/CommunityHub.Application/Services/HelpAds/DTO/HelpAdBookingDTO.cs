using System;
using System.Collections.Generic;

namespace CommunityHub.Application.Services.HelpAds.DTO
{
    public class HelpAdBookingDTO
    {
        public long Id { get; set; }
        public long OfferedHelpAdId { get; set; }
        public long RequestingHelpAdId { get; set; }
        public long BookingTenantId { get; set; }
        public long OfferingTenantId { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime CreatedAt { get; set; }
        public List<long> ReservedSlotIds { get; set; } = new();
    }

    public class CreateHelpAdBookingDTO
    {
        public long OfferedHelpAdId { get; set; }
        public long RequestingHelpAdId { get; set; }
        public long BookingTenantId { get; set; }
        public List<long> SlotIds { get; set; } = new();
    }

    public class HelpAdBookingDetailDTO
    {
        public long BookingId { get; set; }
        public long OfferedHelpAdId { get; set; }
        public long RequestingHelpAdId { get; set; }
        public string RequestingAdDescription { get; set; } = string.Empty;
        public long BookingTenantId { get; set; }
        public string BookingTenantName { get; set; } = string.Empty;
        public int SlotsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime SlotDateTime { get; set; }
    }
}
