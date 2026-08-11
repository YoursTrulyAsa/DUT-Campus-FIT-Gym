SELECT *
FROM dbo.Members;


INSERT INTO dbo.Memberships(
    MemberId,
    MembershipType,
    StartDate,
    EndDate,
    Status,
    Price
)
VALUES
(
    2,
    'Student Annual',
    '2026-08-01',
    '2027-07-31',
    'Active',
    500.00
);
SELECT * FROM dbo.Memberships;

SELECT MemberId, FirstName, LastName, Email
FROM dbo.Members;