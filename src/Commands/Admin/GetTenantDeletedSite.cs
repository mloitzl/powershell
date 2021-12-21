﻿using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using PnP.PowerShell.Commands.Base;
using PnP.PowerShell.Commands.Base.PipeBinds;
using PnP.PowerShell.Commands.Utilities;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PnP.PowerShell.Commands.Admin
{
    [Cmdlet(VerbsCommon.Get, "PnPTenantDeletedSite")]
    public class GetTenantDeletedSite : PnPAdminCmdlet
    {
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = false)]
        [Alias("Url")]
        public SPOSitePipeBind Identity { get; set; }

        [Parameter(Mandatory = false)]
        public uint Limit = 200;

        [Parameter(ParameterSetName = "ParameterSetAllSites")]
        public SwitchParameter IncludePersonalSite { get; set; }

        [Parameter(ParameterSetName = "ParameterSetPersonalSitesOnly", Mandatory = true)]
        public SwitchParameter IncludeOnlyPersonalSite { get; set; }

        protected override void ExecuteCmdlet()
        {
            bool flag = Identity != null && !string.IsNullOrEmpty(Identity.Url) && UrlUtilities.IsPersonalSiteUrl(Identity.Url);
            if (Identity == null || string.IsNullOrEmpty(Identity.Url) || flag)
            {
                List<DeletedSiteProperties> list = new List<DeletedSiteProperties>();
                uint siteRowLimit = Limit;
                bool flag2 = siteRowLimit == 0;
                bool flag3 = false;
                if (flag)
                {
                    flag3 = RequestDeletedSiteProperties((int start) => Tenant.GetDeletedPersonalSitePropertiesAllVersions(Identity.Url), list, ref siteRowLimit);
                }
                else
                {
                    if (!IncludeOnlyPersonalSite)
                    {
                        flag3 = RequestDeletedSitePropertiesFromSharePoint((string start) => Tenant.GetDeletedSitePropertiesFromSharePoint(start), list, ref siteRowLimit);
                    }
                    if (IncludeOnlyPersonalSite || IncludePersonalSite)
                    {
                        bool flag4 = RequestDeletedSiteProperties((int start) => Tenant.GetAllDeletedPersonalSitesPropertiesAllVersions(start), list, ref siteRowLimit);
                        flag3 = flag3 || flag4;
                    }
                }
                foreach (DeletedSiteProperties item in list)
                {
                    WriteObject(new Model.SPODeletedSite(item));
                }
                if (!flag2 && flag3)
                {
                    WriteWarning("More sites are available");
                }
            }
            else
            {
                DeletedSiteProperties deletedSitePropertiesByUrl = Tenant.GetDeletedSitePropertiesByUrl(Identity.Url);
                ClientContext.Load(deletedSitePropertiesByUrl);
                ClientContext.ExecuteQueryRetry();
                WriteObject(new Model.SPODeletedSite(deletedSitePropertiesByUrl));
            }
        }

        private bool RequestDeletedSitePropertiesFromSharePoint(Func<string, SPODeletedSitePropertiesEnumerable> getDeletedSitePropertiesFunc, List<DeletedSiteProperties> deletedSitePropertiesList, ref uint siteRowLimit)
        {
            bool flag = false;
            string text = null;
            bool flag2 = true;
            checked
            {
                while (flag2)
                {
                    SPODeletedSitePropertiesEnumerable spoDeletedSitePropertiesEnumerable = getDeletedSitePropertiesFunc(text);
                    if (spoDeletedSitePropertiesEnumerable == null)
                    {
                        throw new ArgumentNullException("Something went wrong fetching deleted sites");
                    }
                    ClientContext.Load(spoDeletedSitePropertiesEnumerable);
                    ClientContext.Load(spoDeletedSitePropertiesEnumerable, (SPODeletedSitePropertiesEnumerable sp) => sp.NextStartIndexFromSharePoint);
                    ClientContext.ExecuteQueryRetry();
                    if (siteRowLimit == 0 || spoDeletedSitePropertiesEnumerable.Count <= siteRowLimit)
                    {
                        deletedSitePropertiesList.AddRange(spoDeletedSitePropertiesEnumerable);
                        if (siteRowLimit != 0)
                        {
                            siteRowLimit -= (uint)spoDeletedSitePropertiesEnumerable.Count;
                        }
                        flag = false;
                    }
                    else
                    {
                        foreach (DeletedSiteProperties item in spoDeletedSitePropertiesEnumerable)
                        {
                            deletedSitePropertiesList.Add(item);
                            siteRowLimit--;
                            if (siteRowLimit == 0)
                            {
                                flag2 = false;
                                break;
                            }
                        }
                    }
                    text = spoDeletedSitePropertiesEnumerable.NextStartIndexFromSharePoint;
                    if (text == null)
                    {
                        break;
                    }
                    flag = flag || text != null;
                }
                return flag;
            }
        }

        private bool RequestDeletedSiteProperties(Func<int, SPODeletedSitePropertiesEnumerable> getDeletedSitePropertiesFunc, List<DeletedSiteProperties> deletedSitePropertiesList, ref uint siteRowLimit)
        {
            bool flag = false;
            int num = 0;
            bool flag2 = true;
            while (num >= 0 && flag2)
            {
                SPODeletedSitePropertiesEnumerable spoDeletedSitePropertiesEnumerable = getDeletedSitePropertiesFunc(num);
                ClientContext.Load(spoDeletedSitePropertiesEnumerable);
                ClientContext.Load(spoDeletedSitePropertiesEnumerable, (SPODeletedSitePropertiesEnumerable sp) => sp.NextStartIndex);
                ClientContext.ExecuteQueryRetry();
                if (spoDeletedSitePropertiesEnumerable == null)
                {
                    throw new ArgumentNullException("Something went wrong fetching deleted sites");
                }
                checked
                {
                    if (siteRowLimit == 0 || spoDeletedSitePropertiesEnumerable.Count <= siteRowLimit)
                    {
                        deletedSitePropertiesList.AddRange(spoDeletedSitePropertiesEnumerable);
                        if (siteRowLimit != 0)
                        {
                            siteRowLimit -= (uint)spoDeletedSitePropertiesEnumerable.Count;
                        }
                        flag = false;
                    }
                    else
                    {
                        foreach (DeletedSiteProperties item in spoDeletedSitePropertiesEnumerable)
                        {
                            deletedSitePropertiesList.Add(item);
                            siteRowLimit--;
                            if (siteRowLimit == 0)
                            {
                                flag2 = false;
                                break;
                            }
                        }
                    }
                    num = spoDeletedSitePropertiesEnumerable.NextStartIndex;
                    flag = flag || num > 0;
                }
            }
            return flag;
        }
    }
}
