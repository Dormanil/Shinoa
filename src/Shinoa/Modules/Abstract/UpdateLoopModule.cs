using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa.Modules.Abstract
{
    public abstract class UpdateLoopModule : HttpClientModule
    {
        Timer UpdateTimer;

        public void InitUpdateLoop()
        {
            UpdateTimer = new Timer(s =>
            {
                Logging.Log($"Running update loop for module: {GetType().Name}");
                UpdateLoop();
            },
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
        }

        public abstract Task UpdateLoop();
    }
}
