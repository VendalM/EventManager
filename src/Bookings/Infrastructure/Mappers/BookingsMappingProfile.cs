using AutoMapper;
using Bookings.Domain.Models;
using Contracts.Bookings;

namespace Bookings.Infrastructure.Mappers;

/// <summary>
/// Профиль AutoMapper для маппинга бронирования
/// </summary>
public class BookingsMappingProfile : Profile
{
    /// <summary>
    /// Конструктор профиля, в котором настраиваются правила маппинга
    /// </summary>
    public BookingsMappingProfile()
    {
        CreateMap<BookingDto, BookingEntity>()
            .ReverseMap();
    }
}
