using Microsoft.AspNetCore.Mvc.Diagnostics;
using project.Models;
using System.Collections.Concurrent;

namespace project.Services
{
    //хранилище для бронирований
    public class InMemoryBookingDictionary: IBookingRepository
    {
        private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

        public BookingResponse[] GetAllBookings() 
        {
            if (_bookings.IsEmpty)
            {
                return Array.Empty<BookingResponse>();
            }

            return _bookings.Values.Select(booking => booking.ToBookingResponse()).ToArray();
        } 


        public PaginatedResultBookings GetAllBookingsPaginated(
            Guid? EventId,
            DateTime? from, 
            DateTime? to,
            int page = 1, 
            int pageSize = 10) 
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Номер страницы должен быть не меньше 1.");
            }
            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Размер страницы должен быть от 1 до 100.");
            }
            var query = _bookings.Values.AsEnumerable();
            //фильтруем по EventId
            if (EventId.HasValue)
            {
                query = query.Where(b=>b.EventId == EventId);
            }
            if (from.HasValue)
            {
                query = query.Where(b => b.CreatedAt>= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(b => b.CreatedAt <= to.Value);
            }
            var filteredBookings = query.ToList();
            int totalCount = filteredBookings.Count;
            var pagedBookings = filteredBookings
                        .OrderBy(e => e.CreatedAt)//для тестирования сортируем по StartAt, так как ConcurrentDictionary не гарантирует порядок элементов. В тесте ожидаемые элементы хранятся в List.
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(b => b.ToBookingResponse())
                        .ToArray();
            return new PaginatedResultBookings
            {
                totalBookings = totalCount,
                BookingsArray = pagedBookings,
                currentPage = page,
                pageSizeOfCurrentPage = pagedBookings.Length
            };


        }

        public BookingResponse GetBookingById(Guid bookingId) 
        {
            //return _events.TryGetValue(id, out Event? retVal) ? retVal: null;
            if (!_bookings.TryGetValue(bookingId, out var retVal))
            {
                throw new KeyNotFoundException($"Бронирование с идентификатором '{bookingId}' не найдено.");
            }
            return retVal.ToBookingResponse();
        }

        public void UpdateBooking(Guid bookingId, BookingDto bookingDto) 
        {
            
            while (true)
            {
             
                if (!_bookings.TryGetValue(bookingId, out var existingBooking))
                {
                    throw new KeyNotFoundException($"Бронирование с идентификатором {bookingId} не найдено.");
                }


                var updatedBooking = new Booking
                {
                    Id = existingBooking.Id,
                    EventId = existingBooking.EventId,
                    Status = bookingDto.Status,
                    CreatedAt = existingBooking.CreatedAt,
                    ProcessedAt = bookingDto.ProcessedAt
                };

             
                if (_bookings.TryUpdate(bookingId, updatedBooking, existingBooking))
                {
                    return; 
                }

   
            }
        }

        public void DeleteBooking(Guid bookingId) 
        {
            if (!_bookings.TryRemove(bookingId, out Booking? obj))
            {
                throw new KeyNotFoundException($"Бронирование с идентификатором {bookingId} не найдено.");
            }
        }

        public BookingResponse CreateBooking(BookingRequest bookingRequest)
        {
            var newBooking = new Booking
            {
                Id = Guid.NewGuid(),//уникальный идентификатор бронирования генерируется системой
                EventId = bookingRequest.EventId,
                //Status = BookingStatus.Pending,//по умолчанию статус бронирования Pending
                //CreatedAt = DateTime.UtcNow//текущая дата 
            };
            _bookings[newBooking.Id] = newBooking;
            return newBooking.ToBookingResponse();
        }



    }
}
