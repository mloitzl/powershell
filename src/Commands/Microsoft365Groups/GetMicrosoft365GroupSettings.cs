﻿using PnP.PowerShell.Commands.Attributes;
using PnP.PowerShell.Commands.Base;
using PnP.PowerShell.Commands.Base.PipeBinds;
using PnP.PowerShell.Commands.Utilities;
using System;
using System.Management.Automation;

namespace PnP.PowerShell.Commands.Microsoft365Groups
{
    [Cmdlet(VerbsCommon.Get, "PnPMicrosoft365GroupSettings")]
    [RequiredMinimalApiPermissions("Directory.Read.All")]
    public class GetMicrosoft365GroupSettings : PnPGraphCmdlet
    {
        [Parameter(Mandatory = false)]
        public Microsoft365GroupPipeBind Identity;
        
        protected override void ExecuteCmdlet()
        {
            if (Identity != null)
            {
                var groupId = Identity.GetGroupId(HttpClient, AccessToken);
                var groupSettings = Microsoft365GroupsUtility.GetGroupSettingsAsync(HttpClient, AccessToken, groupId.ToString()).GetAwaiter().GetResult();
                WriteObject(groupSettings?.Value, true);
            }
            else
            {
                var groupSettings = Microsoft365GroupsUtility.GetGroupSettingsAsync(HttpClient, AccessToken).GetAwaiter().GetResult();
                WriteObject(groupSettings?.Value, true);
            }
        }
    }
}