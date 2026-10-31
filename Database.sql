-- SQLite
CREATE TABLE IF NOT EXISTS Account (
    Id         INTEGER  NOT NULL,
    Name       TEXT     NOT NULL,
    Password   BLOB     NOT NULL,
    Role       TEXT     NOT NULL,
    CreatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT),
    UNIQUE (Name)
);

CREATE TABLE IF NOT EXISTS OperationLog (
    Id          INTEGER  NOT NULL,
    ExecutedAt  TEXT     NOT NULL,
    UserId      TEXT     NOT NULL,
    HostName    TEXT     NOT NULL,
    VmId        TEXT     NOT NULL,
    VmName      TEXT     NOT NULL,
    Operation   TEXT     NOT NULL,
    Success     INTEGER  NOT NULL,
    Message     TEXT,
    PRIMARY KEY (Id AUTOINCREMENT)
);
