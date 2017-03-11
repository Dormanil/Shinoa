using System.Threading.Tasks;

namespace Shinoa.Services.TimedServices
{
    public interface ITimedService : IService
    {
        Task Callback();
    }
}
