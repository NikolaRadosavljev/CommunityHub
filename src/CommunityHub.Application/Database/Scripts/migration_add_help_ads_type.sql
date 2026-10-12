-- Migration: Add type column to help_ads table
-- This migration adds the 'type' column to distinguish between 'offering' and 'seeking' help ads

ALTER TABLE help_ads ADD COLUMN type VARCHAR(20) NOT NULL DEFAULT 'offering' CHECK (type IN ('offering', 'seeking'));

-- Create index for faster filtering
CREATE INDEX idx_help_ads_type ON help_ads(type);
CREATE INDEX idx_help_ads_status ON help_ads(status);
CREATE INDEX idx_help_ads_building_status ON help_ads(building_id, status);
