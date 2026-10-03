SELECT
    COUNT(*) AS TotalCompletions
FROM WorkoutCompletions;

SELECT
    COUNT(*) AS TotalProgrammes
FROM WorkoutProgrammes;

SELECT
    MemberId,
    COUNT(*) AS CompletionCount
FROM WorkoutCompletions
GROUP BY MemberId
ORDER BY MemberId;

SELECT
    COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WorkoutCompletions'
ORDER BY ORDINAL_POSITION;