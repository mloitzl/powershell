﻿using PnP.PowerShell.Commands.Attributes;
using PnP.PowerShell.Commands.Base;
using PnP.PowerShell.Commands.Base.PipeBinds;
using PnP.PowerShell.Commands.Model.Graph;
using PnP.PowerShell.Commands.Utilities;
using System.Management.Automation;

namespace PnP.PowerShell.Commands.Teams
{
    [Cmdlet(VerbsCommon.Add, "PnPTeamsChannelUser")]
    [RequiredMinimalApiPermissions("ChannelMember.ReadWrite.All")]
    public class AddTeamsChannelUser : PnPGraphCmdlet
    {
        [Parameter(Mandatory = true)]
        public TeamsTeamPipeBind Team;

        [Parameter(Mandatory = true)]
        public TeamsChannelPipeBind Channel;

        [Parameter(Mandatory = true)]
        public string User;

        [Parameter(Mandatory = true)]
        [ValidateSet("Owner", "Member")]
        public string Role;

        protected override void ExecuteCmdlet()
        {
            var groupId = Team.GetGroupId(Connection, AccessToken);
            if (groupId == null)
            {
                throw new PSArgumentException("Group not found");
            }

            var channelId = Channel.GetId(Connection, AccessToken, groupId);
            if (channelId == null)
            {
                throw new PSArgumentException("Channel not found");
            }

            try
            {
                TeamsUtility.AddChannelMemberAsync(Connection, AccessToken, groupId, channelId, User, Role).GetAwaiter().GetResult();
            }
            catch (GraphException ex)
            {
                if (ex.Error != null)
                {
                    throw new PSInvalidOperationException(ex.Error.Message);
                }

                throw;
            }
        }
    }
}
