namespace DUT_Campus_FIT_Gym.Services
{
    public class RewardService
    {
        public int GetLevelReward(string level)
        {
            return level.Trim().ToLower() switch
            {
                "beginner" => 3,
                "intermediate" => 5,
                "pro" => 7,
                _ => 3
            };
        }

        public int GetChallengeReward(string level)
        {
            return level.Trim().ToLower() switch
            {
                "beginner" => 2,
                "intermediate" => 4,
                "pro" => 6,
                _ => 2
            };
        }

        public int GetCheckInReward()
        {
            return 1;
        }

        public int GetDailyChallengeReward()
        {
            return 2;
        }

        public int GetEquipmentReward(int equipmentCount)
        {
            return equipmentCount >= 4 ? 4 : 2;
        }
    }
}