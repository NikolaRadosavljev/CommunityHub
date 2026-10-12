-- Migration: Add booking and notification system for help ads
-- This migration creates tables for booking slots and tracking notifications

-- Table for help ad bookings
CREATE TABLE help_ad_bookings (
    id SERIAL PRIMARY KEY,
    offered_help_ad_id BIGINT NOT NULL REFERENCES help_ads(id),
    requesting_help_ad_id BIGINT NOT NULL REFERENCES help_ads(id),
    booking_tenant_id BIGINT NOT NULL REFERENCES users(id),
    offering_tenant_id BIGINT NOT NULL REFERENCES users(id),
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'CONFIRMED', 'CANCELLED')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Junction table for booking slots
CREATE TABLE help_ad_slot_bookings (
    id SERIAL PRIMARY KEY,
    booking_id BIGINT NOT NULL REFERENCES help_ad_bookings(id) ON DELETE CASCADE,
    slot_id BIGINT NOT NULL REFERENCES help_ad_slots(id),
    reserved_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(booking_id, slot_id)
);

-- Table for notifications
CREATE TABLE help_ad_notifications (
    id SERIAL PRIMARY KEY,
    tenant_id BIGINT NOT NULL REFERENCES users(id),
    help_ad_id BIGINT NOT NULL REFERENCES help_ads(id),
    booking_id BIGINT NOT NULL REFERENCES help_ad_bookings(id),
    booking_tenant_id BIGINT NOT NULL REFERENCES users(id),
    requesting_help_ad_id BIGINT NOT NULL REFERENCES help_ads(id),
    slots_count INT NOT NULL,
    type VARCHAR(50) NOT NULL DEFAULT 'SLOT_BOOKED',
    is_read BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better query performance
CREATE INDEX idx_help_ad_bookings_offered_ad ON help_ad_bookings(offered_help_ad_id);
CREATE INDEX idx_help_ad_bookings_requesting_ad ON help_ad_bookings(requesting_help_ad_id);
CREATE INDEX idx_help_ad_bookings_booking_tenant ON help_ad_bookings(booking_tenant_id);
CREATE INDEX idx_help_ad_bookings_offering_tenant ON help_ad_bookings(offering_tenant_id);
CREATE INDEX idx_help_ad_bookings_status ON help_ad_bookings(status);

CREATE INDEX idx_help_ad_slot_bookings_booking ON help_ad_slot_bookings(booking_id);
CREATE INDEX idx_help_ad_slot_bookings_slot ON help_ad_slot_bookings(slot_id);

CREATE INDEX idx_help_ad_notifications_tenant ON help_ad_notifications(tenant_id);
CREATE INDEX idx_help_ad_notifications_help_ad ON help_ad_notifications(help_ad_id);
CREATE INDEX idx_help_ad_notifications_is_read ON help_ad_notifications(is_read);
CREATE INDEX idx_help_ad_notifications_created ON help_ad_notifications(created_at DESC);
