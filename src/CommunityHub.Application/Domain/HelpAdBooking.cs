using System;
using System.Collections.Generic;

namespace CommunityHub.Application.Domain
{
    /// <summary>
    /// Represents a booking/reservation when a tenant selects a slot from another tenant's help ad.
    /// </summary>
    public class HelpAdBooking
    {
        public long Id { get; set; }

        /// <summary>
        /// The help ad that is being booked (offered by another tenant)
        /// </summary>
        public long OfferedHelpAdId { get; set; }

        /// <summary>
        /// The help ad that created this booking (seeking help or offering matching category)
        /// </summary>
        public long RequestingHelpAdId { get; set; }

        /// <summary>
        /// The tenant who is booking the slots
        /// </summary>
        public long BookingTenantId { get; set; }

        /// <summary>
        /// The tenant who offered the help ad
        /// </summary>
        public long OfferingTenantId { get; set; }

        /// <summary>
        /// Status: PENDING, CONFIRMED, CANCELLED
        /// </summary>
        public string Status { get; set; } = "PENDING";

        /// <summary>
        /// When the booking was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The slots that were reserved for this booking
        /// </summary>
        public List<long> ReservedSlotIds { get; set; } = new();
    }
}
