-- Migration: Add problems table for problem reporting system (Specification 2.7)
-- This migration creates the problems table to allow tenants to report issues in their buildings

CREATE TABLE IF NOT EXISTS problems (
    id BIGSERIAL PRIMARY KEY,
    building_id BIGINT NOT NULL,
    tenant_id BIGINT NOT NULL,
    description TEXT NOT NULL,
    priority INT NOT NULL CHECK (priority IN (1, 2, 3)),
    status VARCHAR(50) NOT NULL DEFAULT 'Unresolved' CHECK (status IN ('Unresolved', 'Potentially_resolved', 'Resolved')),
    reported_date TIMESTAMP NOT NULL DEFAULT NOW(),
    resolved_date TIMESTAMP NULL,
    FOREIGN KEY (building_id) REFERENCES buildings(id) ON DELETE CASCADE,
    FOREIGN KEY (tenant_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Create indexes for efficient queries
CREATE INDEX idx_problems_building ON problems(building_id);
CREATE INDEX idx_problems_tenant ON problems(tenant_id);
CREATE INDEX idx_problems_status ON problems(status);
CREATE INDEX idx_problems_building_status ON problems(building_id, status);
CREATE INDEX idx_problems_reported_date ON problems(reported_date DESC);
