using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    [Table("Announcements")]
    public class Announcement
    {
        [Key]
        public int AnnouncementID { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public DateTime DatePosted { get; set; }

        public string Category { get; set; }
    }
}
