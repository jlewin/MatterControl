using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatterHackers.MatterControl.Extensibility
{
    public class OsInformationPlugin
    {
        public virtual OSType GetOSType()
        {
            throw new Exception("You must implement this in an inherited class.");
        }
    }
}
