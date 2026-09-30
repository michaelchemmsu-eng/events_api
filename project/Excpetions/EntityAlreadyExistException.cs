namespace project.Excpetions
{
    public class EntityAlreadyExistsException:Exception
    {
        public EntityAlreadyExistsException(Guid id) : base($"Entity with Id {id} already exists.")
        {
        }
    }
}
