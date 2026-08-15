INSERT INTO dbo.Announcements
    (Title, Message, DatePosted, Category)
VALUES
    (
        'Gym Maintenance',
        'The gym will be closed on Friday from 08:00 to 12:00 for scheduled maintenance.',
        GETDATE(),
        'Maintenance'
    ),
    (
        'New Equipment Available',
        'New cardio equipment has been added to the gym. Members can now reserve available equipment online.',
        DATEADD(DAY, -1, GETDATE()),
        'General'
    ),
    (
        'Fitness Challenge',
        'A new fitness challenge is starting next week. Visit the gym reception for more information.',
        DATEADD(DAY, -3, GETDATE()),
        'Event'
    );

SELECT DB_NAME() AS CurrentDatabase;

SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'Announcements';

SELECT name
FROM sys.databases
ORDER BY name;

SELECT *
FROM dbo.Announcements
ORDER BY DatePosted DESC;

USE DUTCampusFITGymDB;

USE DUTCampusFITGymDB;

SELECT *
FROM dbo.Announcements
ORDER BY DatePosted DESC;

USE DUTCampusFITGymDB;

INSERT INTO dbo.Announcements
    (Title, Message, DatePosted, Category)
VALUES
(
    'Gym Maintenance',
    'The gym will be closed on Friday from 08:00 to 12:00 for scheduled maintenance.',
    GETDATE(),
    'Maintenance'
),
(
    'New Equipment Available',
    'New cardio equipment has been added to the gym. Members can now reserve available equipment online.',
    DATEADD(DAY, -1, GETDATE()),
    'General'
),
(
    'Fitness Challenge',
    'A new fitness challenge is starting next week. Visit the gym reception for more information.',
    DATEADD(DAY, -3, GETDATE()),
    'Event'
),
(
    'Gym Hours Updated',
    'Please note that the gym will now open at 06:00 and close at 20:00 on weekdays.',
    DATEADD(DAY, -4, GETDATE()),
    'General'
),
(
    'Personal Trainer Sessions',
    'Personal trainer sessions are now available for members who would like additional guidance with their workouts.',
    DATEADD(DAY, -5, GETDATE()),
    'Training'
),
(
    'Equipment Reservation Reminder',
    'Please remember to cancel your equipment reservation when you are finished using the equipment so that other members can access it.',
    DATEADD(DAY, -6, GETDATE()),
    'Reminder'
),
(
    'Gym Safety Reminder',
    'Members are reminded to return equipment to its designated location after use and keep workout areas clear.',
    DATEADD(DAY, -7, GETDATE()),
    'Safety'
),
(
    'Student Fitness Event',
    'Join us for our upcoming student fitness event. More details about the venue and activities will be announced soon.',
    DATEADD(DAY, -9, GETDATE()),
    'Event'
);