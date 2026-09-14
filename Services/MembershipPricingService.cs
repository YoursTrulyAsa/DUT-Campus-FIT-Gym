namespace DUT_Campus_FIT_Gym.Services
{
    public class MembershipPricingService
    {
        public decimal GetBasePrice(string role, string membershipPeriod)
        {
            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                return membershipPeriod switch
                {
                    "Annual" => 300m,
                    "Semester1" => 150m,
                    "Semester2" => 150m,
                    _ => throw new ArgumentException("Invalid membership period.")
                };
            }

            if (role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                return membershipPeriod switch
                {
                    "Annual" => 340m,
                    "Semester1" => 170m,
                    "Semester2" => 170m,
                    _ => throw new ArgumentException("Invalid membership period.")
                };
            }

            throw new ArgumentException("Invalid member role.");
        }

        public decimal GetDiscountPercentage(string role, bool firstTimeMember)
        {
            if (!firstTimeMember)
            {
                return 0m;
            }

            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                return 10m;
            }

            if (role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                return 6m;
            }

            return 0m;
        }

        public decimal CalculatePrice(
            string role,
            string membershipPeriod,
            bool firstTimeMember)
        {
            var basePrice =
                GetBasePrice(
                    role,
                    membershipPeriod);

            var discountPercentage =
                GetDiscountPercentage(
                    role,
                    firstTimeMember);

            var discount =
                basePrice *
                (discountPercentage / 100m);

            return Math.Round(
                basePrice - discount,
                2,
                MidpointRounding.AwayFromZero);
        }

        public bool IsMembershipPeriodAvailable(
            string membershipPeriod,
            DateTime date)
        {
            var year = date.Year;

            var semester1Start =
                GetSecondWeekStart(year, 2);

            var semester1End =
                GetThirdWeekEnd(year, 6);

            var semester2Start =
                GetThirdWeekStart(year, 7);

            var semester2End =
                GetLastWeekEnd(year, 11);

            return membershipPeriod switch
            {
                "Annual" =>
                    date.Date >= semester1Start &&
                    date.Date <= semester2End,

                "Semester1" =>
                    date.Date >= semester1Start &&
                    date.Date <= semester1End,

                "Semester2" =>
                    date.Date >= semester2Start &&
                    date.Date <= semester2End,

                _ => false
            };
        }

        public (DateTime StartDate, DateTime EndDate)
            GetMembershipDates(
                string membershipPeriod,
                int year)
        {
            var februaryStart =
                GetSecondWeekStart(year, 2);

            var juneEnd =
                GetThirdWeekEnd(year, 6);

            var julyStart =
                GetThirdWeekStart(year, 7);

            var novemberEnd =
                GetLastWeekEnd(year, 11);

            return membershipPeriod switch
            {
                "Annual" =>
                    (februaryStart, novemberEnd),

                "Semester1" =>
                    (februaryStart, juneEnd),

                "Semester2" =>
                    (julyStart, novemberEnd),

                _ => throw new ArgumentException(
                    "Invalid membership period.")
            };
        }

        private DateTime GetSecondWeekStart(
            int year,
            int month)
        {
            var firstDay =
                new DateTime(
                    year,
                    month,
                    1);

            var firstMonday =
                GetMonday(firstDay);

            return firstMonday
                .AddDays(7)
                .Date;
        }

        private DateTime GetThirdWeekStart(
            int year,
            int month)
        {
            var firstDay =
                new DateTime(
                    year,
                    month,
                    1);

            var firstMonday =
                GetMonday(firstDay);

            return firstMonday
                .AddDays(14)
                .Date;
        }

        private DateTime GetThirdWeekEnd(
            int year,
            int month)
        {
            var start =
                GetThirdWeekStart(
                    year,
                    month);

            return start
                .AddDays(6)
                .Date;
        }

        private DateTime GetLastWeekEnd(
            int year,
            int month)
        {
            var lastDay =
                new DateTime(
                    year,
                    month,
                    DateTime.DaysInMonth(
                        year,
                        month));

            var daysSinceSunday =
                (int)lastDay.DayOfWeek;

            return lastDay
                .AddDays(-daysSinceSunday)
                .Date;
        }

        private DateTime GetMonday(
            DateTime date)
        {
            var difference =
                (7 +
                 (date.DayOfWeek -
                  DayOfWeek.Monday)) % 7;

            return date
                .AddDays(-difference)
                .Date;
        }
    }
}