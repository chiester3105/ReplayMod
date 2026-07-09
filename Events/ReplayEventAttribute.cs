using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayMod.Events
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ReplayEventAttribute : Attribute
    {
        public EventType Type { get; }
        public ReplayEventAttribute(EventType type) => Type = type;
    }
}
