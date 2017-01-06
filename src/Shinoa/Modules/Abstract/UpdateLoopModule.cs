using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa.Modules.Abstract
{
    public abstract class UpdateLoopModule : Module
    {
        Timer UpdateTimer;

        public void InitUpdateLoop()
        {
            UpdateTimer = new Timer(s =>
            {
                try
                {
                    UpdateLoop();
                }
                catch (Exception e)
                {
                    Logging.Log(e.ToString());
                }
            },
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
        }

        public abstract Task UpdateLoop();
    }
}
