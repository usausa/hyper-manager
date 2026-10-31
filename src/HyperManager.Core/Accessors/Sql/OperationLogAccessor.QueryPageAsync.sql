SELECT
    *
FROM
    OperationLog
WHERE
    1 = 1
/*% if (keyword != null) { */
    AND (HostName LIKE /*@ keyword */'' ESCAPE '\' OR VmName LIKE /*@ keyword */'' ESCAPE '\')
/*% } */
ORDER BY
    Id DESC
LIMIT /*@ limit */20 OFFSET /*@ offset */0
