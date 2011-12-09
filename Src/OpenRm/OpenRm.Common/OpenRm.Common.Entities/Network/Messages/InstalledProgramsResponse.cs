using System.Collections.Generic;
using System.Text;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class InstalledProgramsResponse : ResponseBase
    {
        public List<string> Progs;

        public override string ToString()
        {
            var builder = new StringBuilder();

            foreach (var prog in Progs)
            {
                builder.AppendLine(prog);
            }

            return builder.ToString();
        }
    }
}
