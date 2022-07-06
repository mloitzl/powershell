using System.Management.Automation;
using PnP.PowerShell.Commands.Attributes;
using PnP.PowerShell.Commands.Base;
using PnP.PowerShell.Commands.Base.PipeBinds;
using PnP.PowerShell.Commands.Utilities;

namespace PnP.PowerShell.Commands.Planner
{
    [Cmdlet(VerbsCommon.Get, "PnPPlannerBucket")]
    [RequiredMinimalApiPermissions("Group.Read.All")]
    public class GetPlannerBucket : PnPGraphCmdlet
    {
        private const string ParameterName_BYGROUP = "By Group";
        private const string ParameterName_BYPLANID = "By Plan Id";

        [Parameter(Mandatory = true, HelpMessage = "Specify the group id of group owning the plan.", ParameterSetName = ParameterName_BYGROUP)]
        public PlannerGroupPipeBind Group;

        [Parameter(Mandatory = true, HelpMessage = "Specify the name of the plan to retrieve the tasks for.", ParameterSetName = ParameterName_BYGROUP)]
        public PlannerPlanPipeBind Plan;

        [Parameter(Mandatory = true, ParameterSetName = ParameterName_BYPLANID)]
        public string PlanId;

        [Parameter(Mandatory = false, ParameterSetName = ParameterAttribute.AllParameterSets)]
        public PlannerBucketPipeBind Identity;

        protected override void ExecuteCmdlet()
        {
            if (ParameterSetName == ParameterName_BYGROUP)
            {
                var groupId = Group.GetGroupId(Connection, AccessToken);
                if (groupId != null)
                {
                    var planId = Plan.GetIdAsync(Connection, AccessToken, groupId).GetAwaiter().GetResult();
                    if (planId != null)
                    {
                        if (!ParameterSpecified(nameof(Identity)))
                        {
                            WriteObject(PlannerUtility.GetBucketsAsync(Connection, AccessToken, planId).GetAwaiter().GetResult(), true);
                        }
                        else
                        {
                            WriteObject(Identity.GetBucket(Connection, AccessToken, planId));
                        }
                    }
                    else
                    {
                        throw new PSArgumentException("Plan not found", nameof(Plan));
                    }
                }
                else
                {
                    throw new PSArgumentException("Group not found", nameof(Group));
                }
            }
            else
            {
                WriteObject(PlannerUtility.GetBucketsAsync(Connection, AccessToken, PlanId).GetAwaiter().GetResult(), true);
            }
        }
    }
}