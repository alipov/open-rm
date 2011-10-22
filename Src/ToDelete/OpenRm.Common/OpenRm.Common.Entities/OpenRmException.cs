using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRm.Common.Entities
{
    public class OpenRmException : Exception
    {
        public OpenRmException(string message, Exception inner, bool toLog)
        {
            //this.InnerException = inner;

            if (toLog)
            {
                
            }
        }

    }

    
}
