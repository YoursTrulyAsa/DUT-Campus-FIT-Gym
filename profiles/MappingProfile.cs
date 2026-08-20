using AutoMapper;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;

namespace DUT_Campus_FIT_Gym.profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Define your mappings here
            CreateMap<Reservation, ReservationViewModel>();
        }
    }
}
