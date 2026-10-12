-- Migration: Add homeowners association meetings and attendance tables
-- This migration creates tables for managing homeowners association meetings and attendance tracking

CREATE TABLE IF NOT EXISTS homeowners_meetings (
    id BIGSERIAL PRIMARY KEY,
    building_id BIGINT NOT NULL,
    scheduled_date TIMESTAMP NOT NULL,
    scheduled_time TIME NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'scheduled' CHECK (status IN ('scheduled', 'confirmed', 'cancelled')),
    created_date TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_date TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (building_id) REFERENCES buildings(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS meeting_attendance (
    id BIGSERIAL PRIMARY KEY,
    meeting_id BIGINT NOT NULL,
    tenant_id BIGINT NOT NULL,
    attended BOOLEAN NOT NULL DEFAULT false,
    reported_date TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (meeting_id) REFERENCES homeowners_meetings(id) ON DELETE CASCADE,
    FOREIGN KEY (tenant_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE(meeting_id, tenant_id)
);

CREATE TABLE IF NOT EXISTS meeting_topics (
    id BIGSERIAL PRIMARY KEY,
    meeting_id BIGINT NOT NULL,
    topic_text TEXT NOT NULL,
    proposed_by_tenant_id BIGINT NOT NULL,
    proposed_date TIMESTAMP NOT NULL DEFAULT NOW(),
    is_accepted BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (meeting_id) REFERENCES homeowners_meetings(id) ON DELETE CASCADE
);

-- Add is_accepted column if table already exists (safe migration)
ALTER TABLE meeting_topics ADD COLUMN IF NOT EXISTS is_accepted BOOLEAN NOT NULL DEFAULT FALSE;
-- Mark all existing topics as accepted
UPDATE meeting_topics SET is_accepted = TRUE WHERE is_accepted = FALSE;

ALTER TABLE meeting_topics DROP CONSTRAINT IF EXISTS meeting_topics_proposed_by_tenant_id_fkey;

-- Create indexes for efficient queries
CREATE INDEX idx_homeowners_meetings_building ON homeowners_meetings(building_id);
CREATE INDEX idx_homeowners_meetings_status ON homeowners_meetings(status);
CREATE INDEX idx_homeowners_meetings_scheduled_date ON homeowners_meetings(scheduled_date DESC);
CREATE INDEX idx_meeting_attendance_meeting ON meeting_attendance(meeting_id);
CREATE INDEX idx_meeting_attendance_tenant ON meeting_attendance(tenant_id);
CREATE INDEX idx_meeting_attendance_attended ON meeting_attendance(attended);
CREATE INDEX idx_meeting_topics_meeting ON meeting_topics(meeting_id);
CREATE INDEX idx_meeting_topics_tenant ON meeting_topics(proposed_by_tenant_id);
