using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shinoa.Net.Module
{
    interface IModule
    {
        void Init();
        void MessageReceived(object sender, MessageEventArgs e);        
    }
}
