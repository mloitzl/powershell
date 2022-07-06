using System.Management.Automation;
using PnP.PowerShell.Commands.Attributes;
using PnP.PowerShell.Commands.Base;
using PnP.PowerShell.Commands.Base.PipeBinds;
using PnP.PowerShell.Commands.Utilities;

namespace PnP.PowerShell.Commands.Planner
{
    [Cmdlet(VerbsCommon.New, "PnPPlannerPlan")]
    [RequiredMinimalApiPermissions("Group.ReadWrite.All")]
    public class NewPlannerPlan : PnPGraphCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Specify the group id of the plans to retrieve.")]
        public PlannerGroupPipeBind Group;

        [Parameter(Mandatory = true)]
        public string Title;
        protected override void ExecuteCmdlet()
        {
            var groupId = Group.GetGroupId(Connection, AccessToken);
            if (groupId != null)
            {
                WriteObject(PlannerUtility.CreatePlanAsync(Connection, AccessToken, groupId, Title).GetAwaiter().GetResult());
            }
            else
            {
                throw new PSArgumentException("Group not found");
            }
        }
    }
}