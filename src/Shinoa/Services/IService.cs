using System.Collections.Generic;
using Discord.Commands;

namespace Shinoa.Services
{
    public interface IService
    {
        void Init(IDictionary<string, dynamic> config, IDependencyMap map);
    }
}
