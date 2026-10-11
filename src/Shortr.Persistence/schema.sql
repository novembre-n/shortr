CREATE TABLE IF NOT EXISTS Links (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Code          TEXT    NOT NULL UNIQUE,
    OriginalUrl   TEXT    NOT NULL,
    CreatedAtUtc TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_links_code ON Links (Code);