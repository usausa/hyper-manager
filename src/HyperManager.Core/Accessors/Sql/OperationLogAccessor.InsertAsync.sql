INSERT INTO
    OperationLog (ExecutedAt, UserId, HostName, VmId, VmName, Operation, Success, Message)
VALUES
    (/*@ executedAt */'', /*@ userId */'', /*@ hostName */'', /*@ vmId */'', /*@ vmName */'', /*@ operation */'', /*@ success */0, /*@ message */NULL)
RETURNING
    Id
