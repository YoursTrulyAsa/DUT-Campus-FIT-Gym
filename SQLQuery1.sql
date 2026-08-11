SELECT MemberId, FirstName, LastName, Email, Role
FROM Members;

INSERT INTO Memberships
(
    MemberId,
    MembershipType,
    StartDate,
    EndDate,
    Status,
    Price
)
VALUES
(
    1,
    'Student Annual',
    '2026-08-01',
    '2027-07-31',
    'Active',
    500.00
);

SELECT *
FROM Memberships;