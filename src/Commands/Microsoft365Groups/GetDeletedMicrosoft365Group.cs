﻿using System.Management.Automation;
using System.Linq;
using PnP.PowerShell.Commands.Attributes;
using PnP.PowerShell.Commands.Base;
using PnP.PowerShell.Commands.Base.PipeBinds;
using PnP.PowerShell.Commands.Utilities;

namespace PnP.PowerShell.Commands.Microsoft365Groups
{
    [Cmdlet(VerbsCommon.Get, "PnPDeletedMicrosoft365Group")]
    [RequiredMinimalApiPermissions("Group.Read.All")]
    public class GetDeletedMicrosoft365Group : PnPGraphCmdlet
    {
        [Parameter(Mandatory = false)]
        public Microsoft365GroupPipeBind Identity;

        protected override void ExecuteCmdlet()
        {
            if (Identity != null)
            {
                WriteObject(Identity.GetDeletedGroup(Connection, AccessToken));
            }
            else
            {
                var groups = Microsoft365GroupsUtility.GetDeletedGroupsAsync(Connection, AccessToken).GetAwaiter().GetResult();
                WriteObject(groups.OrderBy(g => g.DisplayName), true);
            }
        }
    }
}