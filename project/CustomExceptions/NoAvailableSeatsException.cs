namespace project.CustomExceptions
{
    /// <summary>
    /// тип ошибки "нет доступных мест для бронирования"
    /// </summary>
    public class NoAvailableSeatsException: Exception
    {
        public NoAvailableSeatsException(string? message):base(message) 
        {
           
        }
    }
}
