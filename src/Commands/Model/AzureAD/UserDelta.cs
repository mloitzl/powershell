using System.Collections.Generic;

namespace PnP.PowerShell.Commands.Model.AzureAD
{
    /// <summary>
    /// User delta in Azure Active Directory
    /// </summary>
    public class UserDelta
    {
        /// <summary>
        /// User objects with changes or all users if no SkipToken has been provided
        /// </summary>
        public IList<User> Users { get; set; }

        /// <summary>
        /// The DeltaToken which can be used when querying for changes to request changes made to User objects since this DeltaToken has been given out
        /// </summary>
        public string DeltaToken { get; set; }

        /// <summary>
        /// The NextLink which indicates there are more results
        /// </summary>
        public string NextLink { get; set; }
    }
}
