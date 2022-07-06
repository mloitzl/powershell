using System;
using System.Linq;
using System.Management.Automation;
using PnP.PowerShell.Commands.Attributes;
using PnP.PowerShell.Commands.Base;
using PnP.PowerShell.Commands.Base.PipeBinds;
using PnP.PowerShell.Commands.Model.Planner;
using PnP.PowerShell.Commands.Utilities;
using PnP.PowerShell.Commands.Utilities.REST;

namespace PnP.PowerShell.Commands.Planner
{
    [Cmdlet(VerbsCommon.Set, "PnPPlannerTask")]
    [RequiredMinimalApiPermissions("Group.ReadWrite.All")]
    public class SetPlannerTask : PnPGraphCmdlet
    {
        [Parameter(Mandatory = true)]
        public string TaskId;

        [Parameter(Mandatory = false)]
        public string Title;

        [Parameter(Mandatory = false)]
        public PlannerBucketPipeBind Bucket;

        [Parameter(Mandatory = false)]
        public int PercentComplete;

        [Parameter(Mandatory = false)]
        public int Priority;

        [Parameter(Mandatory = false)]
        public DateTime DueDateTime;

        [Parameter(Mandatory = false)]
        public DateTime StartDateTime;

        [Parameter(Mandatory = false)]
        public string Description;

        [Parameter(Mandatory = false)]
        public string[] AssignedTo;

        protected override void ExecuteCmdlet()
        {
            var existingTask = PlannerUtility.GetTaskAsync(Connection, AccessToken, TaskId, false, false).GetAwaiter().GetResult();
            if (existingTask != null)
            {
                var plannerTask = new PlannerTask();
                if (ParameterSpecified(nameof(Title)))
                {
                    plannerTask.Title = Title;
                }
                if (ParameterSpecified(nameof(Bucket)))
                {
                    var bucket = Bucket.GetBucket(Connection, AccessToken, existingTask.PlanId);
                    if (bucket != null)
                    {
                        plannerTask.BucketId = bucket.Id;
                    }
                }
                if (ParameterSpecified(nameof(PercentComplete)))
                {
                    plannerTask.PercentComplete = PercentComplete;
                }

                if (ParameterSpecified(nameof(Priority)))
                {
                    if (Priority < 0 || Priority > 10)
                    {
                        throw new PSArgumentException($"Parameter '{nameof(Priority)}' must be a number between 0 and 10.");
                    }

                    plannerTask.Priority = Priority;
                }

                if (ParameterSpecified(nameof(DueDateTime)))
                {
                    plannerTask.DueDateTime = DueDateTime.ToUniversalTime();
                }

                if (ParameterSpecified(nameof(StartDateTime)))
                {
                    plannerTask.StartDateTime = StartDateTime.ToUniversalTime();
                }

                if (ParameterSpecified(nameof(AssignedTo)))
                {
                    plannerTask.Assignments = new System.Collections.Generic.Dictionary<string, TaskAssignment>();
                    var chunks = BatchUtility.Chunk(AssignedTo, 20);
                    foreach (var chunk in chunks)
                    {
                        var userIds = BatchUtility.GetPropertyBatchedAsync(Connection, AccessToken, chunk.ToArray(), "/users/{0}", "id").GetAwaiter().GetResult();
                        foreach (var userId in userIds)
                        {
                            plannerTask.Assignments.Add(userId.Value, new TaskAssignment());
                        }
                    }
                    foreach (var existingAssignment in existingTask.Assignments)
                    {
                        if (plannerTask.Assignments.FirstOrDefault(t => t.Key == existingAssignment.Key).Key == null)
                        {
                            plannerTask.Assignments.Add(existingAssignment.Key, null);
                        }
                    }
                }


                PlannerUtility.UpdateTaskAsync(Connection, AccessToken, existingTask, plannerTask).GetAwaiter().GetResult();

                if (ParameterSpecified(nameof(Description)))
                {
                    var existingTaskDetails = PlannerUtility.GetTaskDetailsAsync(Connection, AccessToken, TaskId, false).GetAwaiter().GetResult();
                    PlannerUtility.UpdateTaskDetailsAsync(Connection, AccessToken, existingTaskDetails, Description).GetAwaiter().GetResult();
                }
            }
            else
            {
                throw new PSArgumentException("Task not found", nameof(TaskId));
            }
        }
    }
}