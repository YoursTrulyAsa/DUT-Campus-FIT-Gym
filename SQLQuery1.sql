SELECT 
    wp.WorkoutPlanId,
    wp.WorkoutName,
    wp.ExerciseName,
    wp.ExerciseId,
    e.ExerciseName AS LinkedExercise
FROM dbo.WorkoutPlans wp
LEFT JOIN Exercises e
    ON wp.ExerciseId = e.ExerciseId
ORDER BY wp.WorkoutPlanId;

USE DUTCampusFITGymDB;
GO

SELECT DB_NAME() AS CurrentDatabase;

SELECT DB_NAME() AS CurrentDatabase;