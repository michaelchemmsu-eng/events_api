namespace project.Excpetions
{
    public class EventNotFoundExcpetion: Exception
    {
        public EventNotFoundExcpetion(Guid id) :base($"Событие c ключом {id} не найдено")
        {
        }
    }
}
