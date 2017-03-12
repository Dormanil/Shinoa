using Discord.Commands;

namespace Shinoa.Services
{
    public interface IService
    {
        void Init(dynamic config, IDependencyMap map);
    }
}
