namespace Shinoa.Services
{
    using Discord;

    public interface IDatabaseService : IService
    {
        bool RemoveBinding(IEntity<ulong> binding);
    }
}