SELECT MemberId, FirstName, LastName, Email
FROM dbo.Members;

INSERT INTO dbo.Attendances
(
    MemberId,
    CheckInTime,
    CheckOutTime
)
VALUES
(
    2,
    '2026-08-08 08:15:00',
    '2026-08-08 10:02:00'
);

INSERT INTO dbo.Attendances
(
    MemberId,
    CheckInTime,
    CheckOutTime
)
VALUES
(
    2,
    '2026-08-06 17:20:00',
    '2026-08-06 18:45:00'
),
(
    2,
    '2026-08-03 09:05:00',
    '2026-08-03 10:30:00'
);

SELECT *
FROM dbo.Attendances;