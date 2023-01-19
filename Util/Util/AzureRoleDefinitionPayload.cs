using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public class AzureRoleDefinitionPayload
    {
        public AzureRoleDefinitionPayloadProperties properties { get; set; }
    }

    public class AzureRoleDefinitionPayloadProperties
    {
        public string roleDefinitionId { get; set; }
        public string principalId { get; set; }
        public string principalType { get; set; }
    }
}
