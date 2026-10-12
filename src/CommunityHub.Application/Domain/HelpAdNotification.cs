using System;

namespace CommunityHub.Application.Domain
{
    /// <summary>
    /// Notification sent to a tenant when someone books slots from their help ad.
    /// </summary>
    public class HelpAdNotification
    {
        public long Id { get; set; }

        /// <summary>
        /// The tenant who receives the notification (offered the help ad)
        /// </summary>
        public long TenantId { get; set; }

        /// <summary>
        /// The help ad that was booked
        /// </summary>
        public long HelpAdId { get; set; }

        /// <summary>
        /// The booking that triggered this notification
        /// </summary>
        public long BookingId { get; set; }

        /// <summary>
        /// The tenant who created the booking
        /// </summary>
        public long BookingTenantId { get; set; }

        /// <summary>
        /// The help ad that created the booking (seeking help)
        /// </summary>
        public long RequestingHelpAdId { get; set; }

        /// <summary>
        /// Number of slots reserved
        /// </summary>
        public int SlotsCount { get; set; }

        /// <summary>
        /// Notification type: SLOT_BOOKED
        /// </summary>
        public string Type { get; set; } = "SLOT_BOOKED";

        /// <summary>
        /// Whether the notification has been read
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// When the notification was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
