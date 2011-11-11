using System;

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
