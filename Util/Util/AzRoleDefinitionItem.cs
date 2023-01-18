using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    internal class RoleDefinitionItem
    {
        public RoleDefinitionItemProperties properties { get; set; }
    }

    internal  class RoleDefinitionItemProperties
    {
        public string roleDefinitionId { get; set; }
        public string principalId { get; set; }
        public string principalType { get; set; }
    }
}
