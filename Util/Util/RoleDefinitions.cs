using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public class AzureRoleDefinitions
    {
        public AzureRoleDefinition[] value { get; set; }
    }

    public class AzureRoleDefinition
    {
        public AzureRoleDefinitionProperties properties { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
    }

    public class AzureRoleDefinitionProperties
    {
        public string roleName { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string[] assignableScopes { get; set; }
        public AzureRoleDefinitionPermission[] permissions { get; set; }
        public DateTime createdOn { get; set; }
        public DateTime updatedOn { get; set; }
        public object createdBy { get; set; }
        public string updatedBy { get; set; }
    }

    public class AzureRoleDefinitionPermission
    {
        public string[] actions { get; set; }
        public string[] notActions { get; set; }
        public string[] dataActions { get; set; }
        public string[] notDataActions { get; set; }
    }

}
