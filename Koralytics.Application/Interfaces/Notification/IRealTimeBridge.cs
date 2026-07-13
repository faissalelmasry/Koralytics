using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Notification
{
    
        public interface IRealTimeBridge
        {
            Task SendToAllAsync(string method, object payload);
            Task SendToGroupAsync(string groupName, string method, object payload);
        }
    }

