namespace Shinoa.Services
{
    using Discord;

    public interface IDatabaseService : IService
    {
        bool RemoveBinding<T>(T channel);
    }
}