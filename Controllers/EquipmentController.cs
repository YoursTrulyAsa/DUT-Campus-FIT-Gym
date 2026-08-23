using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Mvc;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class EquipmentController : Controller
    {
        public IActionResult Index()
        {
            var equipment = new List<Equipment>
            {
                new Equipment
                {
                    EquipmentID= 1,
                    EquipmentName= "Treadmill",
                    Category = "Cardio",
                    Location = "Cardio Area",
                    IsAvailable = "Available"
                },
                new Equipment
                {
                    EquipmentID = 2,
                    EquipmentName = "Bench Press",
                    Category = "Strength",
                    Location = "Strength Area",
                    IsAvailable = "Available"
                   
                },
                new Equipment
                {
                    EquipmentID= 3,
                    EquipmentName = "Dumbbells",
                    Category = "Free Weights",
                    Location = "Free Weight Area",
                    IsAvailable = "In Use"
                },
                new Equipment
                {
                    EquipmentID = 4,
                    EquipmentName = "Exercise Bike",
                    Category = "Cardio",
                    Location = "Cardio Area",
                   IsAvailable = "Available"
                },
                new Equipment
                {
                    EquipmentID = 5,
                    EquipmentName = "Cable Machine",
                    Category = "Strength",
                    Location = "Strength Area",
                    IsAvailable = "Available"
                },
                new Equipment
                {
                    EquipmentID = 6,
                    EquipmentName = "Squat Rack",
                    Category = "Strength",
                    Location = "Strength Area",
                    IsAvailable = "Maintenance"
                }
            };

            return View(equipment);
        }
    }
 }



