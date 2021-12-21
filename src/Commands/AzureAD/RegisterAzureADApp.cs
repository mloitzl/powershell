﻿using PnP.Framework;
using PnP.Framework.Utilities;

using PnP.PowerShell.Commands.Model;
using PnP.PowerShell.Commands.Utilities;
using PnP.PowerShell.Commands.Utilities.REST;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OperatingSystem = PnP.PowerShell.Commands.Utilities.OperatingSystem;
using Resources = PnP.PowerShell.Commands.Properties.Resources;
using PnP.PowerShell.Commands.Attributes;
using PnP.PowerShell.Commands.Base;

namespace PnP.PowerShell.Commands.AzureAD
{
    [Cmdlet(VerbsLifecycle.Register, "PnPAzureADApp")]
    public class RegisterAzureADApp : BasePSCmdlet, IDynamicParameters
    {
        private const string ParameterSet_EXISTINGCERT = "Existing Certificate";
        private const string ParameterSet_NEWCERT = "Generate Certificate";

        private CancellationTokenSource cancellationTokenSource;

        [Parameter(Mandatory = true, ParameterSetName = ParameterAttribute.AllParameterSets)]
        public string ApplicationName;

        [Parameter(Mandatory = true, ParameterSetName = ParameterAttribute.AllParameterSets)]
        public string Tenant;

        [Parameter(Mandatory = true, ParameterSetName = ParameterSet_EXISTINGCERT)]
        public string CertificatePath;

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet_NEWCERT)]
        public string CommonName;

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = ParameterSet_NEWCERT)]
        public string Country = String.Empty;

        [Parameter(Mandatory = false, Position = 2, ParameterSetName = ParameterSet_NEWCERT)]
        public string State = string.Empty;

        [Parameter(Mandatory = false, Position = 3, ParameterSetName = ParameterSet_NEWCERT)]
        public string Locality = string.Empty;

        [Parameter(Mandatory = false, Position = 4, ParameterSetName = ParameterSet_NEWCERT)]
        public string Organization = string.Empty;

        [Parameter(Mandatory = false, Position = 5, ParameterSetName = ParameterSet_NEWCERT)]
        public string OrganizationUnit = string.Empty;

        [Parameter(Mandatory = false, Position = 7, ParameterSetName = ParameterSet_NEWCERT)]
        public int ValidYears = 10;

        [Parameter(Mandatory = false, Position = 8, ParameterSetName = ParameterSet_NEWCERT)]
        [Parameter(Mandatory = false, Position = 8, ParameterSetName = ParameterSet_EXISTINGCERT)]
        public SecureString CertificatePassword;

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet_NEWCERT)]
        public string OutPath;

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet_NEWCERT)]
        public StoreLocation Store;

        [Parameter(Mandatory = false)]
        public AzureEnvironment AzureEnvironment = AzureEnvironment.Production;

        [Parameter(Mandatory = false)]
        public string Username;

        [Parameter(Mandatory = false)]
        public SecureString Password;

        [Parameter(Mandatory = false)]
        public SwitchParameter DeviceLogin;

        [Parameter(Mandatory = false)]
        public SwitchParameter NoPopup;

        [Parameter(Mandatory = false)]
        public SwitchParameter Interactive;

        protected override void ProcessRecord()
        {
            if (ParameterSpecified(nameof(Store)) && !OperatingSystem.IsWindows())
            {
                throw new PSArgumentException("The Store parameter is only supported on Microsoft Windows");
            }

            if (!string.IsNullOrWhiteSpace(OutPath))
            {
                if (!Path.IsPathRooted(OutPath))
                {
                    OutPath = Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, OutPath);
                }
            }
            else
            {
                OutPath = SessionState.Path.CurrentFileSystemLocation.Path;
            }

            var redirectUri = "http://localhost";
            if (ParameterSpecified(nameof(DeviceLogin)))
            {
                redirectUri = "https://pnp.github.io/powershell/consent.html";
            }

            var messageWriter = new CmdletMessageWriter(this);
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var loginEndPoint = string.Empty;

            using (var authenticationManager = new AuthenticationManager())
            {
                loginEndPoint = authenticationManager.GetAzureADLoginEndPoint(AzureEnvironment);
            }

            var permissionScopes = new PermissionScopes();
            var scopes = new List<PermissionScope>();
            if (this.Scopes != null)
            {
                foreach (var scopeIdentifier in this.Scopes)
                {
                    PermissionScope scope = null;
                    scope = permissionScopes.GetScope(PermissionScopes.ResourceAppId_Graph, scopeIdentifier.Replace("MSGraph.", ""), "Role");
                    if (scope == null)
                    {
                        scope = permissionScopes.GetScope(PermissionScopes.ResourceAppId_SPO, scopeIdentifier.Replace("SPO.", ""), "Role");
                    }
                    if (scope == null)
                    {
                        scope = permissionScopes.GetScope(PermissionScopes.ResourceAppID_O365Management, scopeIdentifier.Replace("O365.", ""), "Role");
                    }
                    if (scope != null)
                    {
                        scopes.Add(scope);
                    }
                }
            }
            else
            {
                if (GraphApplicationPermissions != null)
                {
                    foreach (var scopeIdentifier in this.GraphApplicationPermissions)
                    {
                        scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_Graph, scopeIdentifier, "Role"));
                    }
                }
                if (GraphDelegatePermissions != null)
                {
                    foreach (var scopeIdentifier in this.GraphDelegatePermissions)
                    {
                        scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_Graph, scopeIdentifier, "Scope"));
                    }
                }
                if (SharePointApplicationPermissions != null)
                {
                    foreach (var scopeIdentifier in this.SharePointApplicationPermissions)
                    {
                        scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_SPO, scopeIdentifier, "Role"));
                    }
                }
                if (SharePointDelegatePermissions != null)
                {
                    foreach (var scopeIdentifier in this.SharePointDelegatePermissions)
                    {
                        scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_SPO, scopeIdentifier, "Scope"));
                    }
                }
            }
            if (!scopes.Any())
            {
                messageWriter.WriteWarning("No permissions specified, using default permissions");
                scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_SPO, "Sites.FullControl.All", "Role")); // AppOnly
                scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_SPO, "AllSites.FullControl", "Scope")); // AppOnly
                scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_Graph, "Group.ReadWrite.All", "Role")); // AppOnly
                scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_SPO, "User.ReadWrite.All", "Role")); // AppOnly
                scopes.Add(permissionScopes.GetScope(PermissionScopes.ResourceAppId_Graph, "User.ReadWrite.All", "Role")); // AppOnly
            }
            var record = new PSObject();

            string token = GetAuthToken(messageWriter);

            if (!string.IsNullOrEmpty(token))
            {
                var cert = GetCertificate(record);

                using (var httpClient = new HttpClient())
                {
                    if (!AppExists(ApplicationName, httpClient, token))
                    {
                        var azureApp = CreateApp(loginEndPoint, httpClient, token, cert, redirectUri, scopes);

                        record.Properties.Add(new PSVariableProperty(new PSVariable("AzureAppId/ClientId", azureApp.AppId)));
                        record.Properties.Add(new PSVariableProperty(new PSVariable("Certificate Thumbprint", cert.GetCertHashString())));
                        byte[] certPfxData = cert.Export(X509ContentType.Pfx, CertificatePassword);
                        var base64String = Convert.ToBase64String(certPfxData);
                        record.Properties.Add(new PSVariableProperty(new PSVariable("Base64Encoded", base64String)));
                        StartConsentFlow(loginEndPoint, azureApp, redirectUri, token, httpClient, record, messageWriter, scopes);
                    }
                    else
                    {
                        throw new PSInvalidOperationException($"The application with name {ApplicationName} already exists.");
                    }
                }
            }
        }

        protected override void StopProcessing()
        {
            cancellationTokenSource.Cancel();
        }

        private static object GetScopesPayload(List<PermissionScope> scopes)
        {
            var resourcePermissions = new List<AppResource>();
            var distinctResources = scopes.GroupBy(s => s.resourceAppId).Select(r => r.First()).ToList();
            foreach (var distinctResource in distinctResources)
            {
                var id = distinctResource.resourceAppId;
                var appResource = new AppResource() { Id = id };
                appResource.ResourceAccess.AddRange(scopes.Where(s => s.resourceAppId == id).ToList());
                resourcePermissions.Add(appResource);
            }
            return resourcePermissions;
        }

        protected IEnumerable<string> Scopes
        {
            get
            {
                if (ParameterSpecified(nameof(Scopes)) && MyInvocation.BoundParameters["Scopes"] != null)
                {
                    return MyInvocation.BoundParameters["Scopes"] as string[];
                }
                else
                {
                    return null;
                }
            }
        }

        protected IEnumerable<string> GraphApplicationPermissions
        {
            get
            {
                if (ParameterSpecified(nameof(GraphApplicationPermissions)) && MyInvocation.BoundParameters[nameof(GraphApplicationPermissions)] != null)
                {
                    return MyInvocation.BoundParameters[nameof(GraphApplicationPermissions)] as string[];
                }
                else
                {
                    return null;
                }
            }
        }

        protected IEnumerable<string> GraphDelegatePermissions
        {
            get
            {
                if (ParameterSpecified(nameof(GraphDelegatePermissions)) && MyInvocation.BoundParameters[nameof(GraphDelegatePermissions)] != null)
                {
                    return MyInvocation.BoundParameters[nameof(GraphDelegatePermissions)] as string[];
                }
                else
                {
                    return null;
                }
            }
        }

        protected IEnumerable<string> SharePointApplicationPermissions
        {
            get
            {
                if (ParameterSpecified(nameof(SharePointApplicationPermissions)) && MyInvocation.BoundParameters[nameof(SharePointApplicationPermissions)] != null)
                {
                    return MyInvocation.BoundParameters[nameof(SharePointApplicationPermissions)] as string[];
                }
                else
                {
                    return null;
                }
            }
        }

        protected IEnumerable<string> SharePointDelegatePermissions
        {
            get
            {
                if (ParameterSpecified(nameof(SharePointDelegatePermissions)) && MyInvocation.BoundParameters[nameof(SharePointDelegatePermissions)] != null)
                {
                    return MyInvocation.BoundParameters[nameof(SharePointDelegatePermissions)] as string[];
                }
                else
                {
                    return null;
                }
            }
        }

        protected IEnumerable<string> O365ManagementApplicationPermissions
        {
            get
            {
                if (ParameterSpecified(nameof(O365ManagementApplicationPermissions)) && MyInvocation.BoundParameters[nameof(O365ManagementApplicationPermissions)] != null)
                {
                    return MyInvocation.BoundParameters[nameof(O365ManagementApplicationPermissions)] as string[];
                }
                else
                {
                    return null;
                }
            }
        }

        protected IEnumerable<string> O365ManagementDelegatePermissions
        {
            get
            {
                if (ParameterSpecified(nameof(O365ManagementDelegatePermissions)) && MyInvocation.BoundParameters[nameof(O365ManagementDelegatePermissions)] != null)
                {
                    return MyInvocation.BoundParameters[nameof(O365ManagementDelegatePermissions)] as string[];
                }
                else
                {
                    return null;
                }
            }
        }

        public object GetDynamicParameters()
        {
            // var classAttribute = this.GetType().GetCustomAttributes(false).FirstOrDefault(a => a is PropertyLoadingAttribute);
            const string parameterName = "Scopes";

            var parameterDictionary = new RuntimeDefinedParameterDictionary();
            var attributeCollection = new System.Collections.ObjectModel.Collection<Attribute>();

            // Scopes
            var parameterAttribute = new ParameterAttribute
            {
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                Mandatory = false
            };

            attributeCollection.Add(parameterAttribute);
            attributeCollection.Add(new ObsoleteAttribute("Use either -GraphApplicationPermissions, -GraphDelegatePermissions, -SharePointApplicationPermissions or -SharePointDelegatePermissions"));

            var identifiers = new PermissionScopes().GetIdentifiers();

            var validateSetAttribute = new ValidateSetAttribute(identifiers);
            attributeCollection.Add(validateSetAttribute);

            var runtimeParameter = new RuntimeDefinedParameter(parameterName, typeof(string[]), attributeCollection);

            parameterDictionary.Add(parameterName, runtimeParameter);

            // Graph
            parameterDictionary.Add("GraphApplicationPermissions", GetParameter("GraphApplicationPermissions", PermissionScopes.ResourceAppId_Graph, "Role"));
            parameterDictionary.Add("GraphDelegatePermissions", GetParameter("GraphDelegatePermissions", PermissionScopes.ResourceAppId_Graph, "Scope"));

            // SharePoint
            parameterDictionary.Add("SharePointApplicationPermissions", GetParameter("SharePointApplicationPermissions", PermissionScopes.ResourceAppId_SPO, "Role"));
            parameterDictionary.Add("SharePointDelegatePermissions", GetParameter("SharePointDelegatePermissions", PermissionScopes.ResourceAppId_SPO, "Scope"));

            // O365 Management
            parameterDictionary.Add("O365ManagementApplicationPermissions", GetParameter("O365ManagementApplicationPermissions", PermissionScopes.ResourceAppID_O365Management, "Role"));
            parameterDictionary.Add("O365ManagementDelegatePermissions", GetParameter("O365ManagementDelegatePermissions", PermissionScopes.ResourceAppID_O365Management, "Scope"));

            return parameterDictionary;
        }

        private RuntimeDefinedParameter GetParameter(string parameterName, string resourceAppId, string type)
        {
            var attributeCollection = new System.Collections.ObjectModel.Collection<Attribute>();
            var parameterAttribute = new ParameterAttribute
            {
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                Mandatory = false
            };
            attributeCollection.Add(parameterAttribute);
            var validateSetAttribute = new ValidateSetAttribute(new PermissionScopes().GetIdentifiers(resourceAppId, type));
            attributeCollection.Add(validateSetAttribute);
            var parameter = new RuntimeDefinedParameter(parameterName, typeof(string[]), attributeCollection);
            return parameter;
        }

        private string GetAuthToken(CmdletMessageWriter messageWriter)
        {
            var token = string.Empty;
            if (DeviceLogin.IsPresent)
            {
                Task.Factory.StartNew(() =>
                {
                    token = AzureAuthHelper.AuthenticateDeviceLogin(cancellationTokenSource, messageWriter, NoPopup, AzureEnvironment);
                    if (token == null)
                    {
                        messageWriter.WriteWarning("Operation cancelled or no token retrieved.");
                    }
                    messageWriter.Stop();
                });
                messageWriter.Start();
            }
            else if (Interactive.IsPresent)
            {
                Task.Factory.StartNew(() =>
                {
                    token = AzureAuthHelper.AuthenticateInteractive(cancellationTokenSource, messageWriter, NoPopup, AzureEnvironment, Tenant);
                    if (token == null)
                    {
                        messageWriter.WriteWarning("Operation cancelled or no token retrieved.");
                    }
                    messageWriter.Stop();
                });
                messageWriter.Start();
            }
            else
            {
                if (PnPConnection.Current?.PSCredential != null)
                {
                    Username = PnPConnection.Current.PSCredential.UserName;
                    Password = PnPConnection.Current.PSCredential.Password;
                }
                if (string.IsNullOrEmpty(Username))
                {
                    throw new PSArgumentException("Username is required or use -DeviceLogin or -Interactive");
                }
                if (Password == null || Password.Length == 0)
                {
                    throw new PSArgumentException("Password is required or use -DeviceLogin or -Interactive");
                }
                token = AzureAuthHelper.AuthenticateAsync(Tenant, Username, Password, AzureEnvironment).GetAwaiter().GetResult();
            }

            return token;
        }

        private X509Certificate2 GetCertificate(PSObject record)
        {
            var cert = new X509Certificate2();
            if (ParameterSetName == ParameterSet_EXISTINGCERT)
            {
                if (!Path.IsPathRooted(CertificatePath))
                {
                    CertificatePath = Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, CertificatePath);
                }
                // Ensure a file exists at the provided CertificatePath
                if (!File.Exists(CertificatePath))
                {
                    throw new PSArgumentException(string.Format(Resources.CertificateNotFoundAtPath, CertificatePath), nameof(CertificatePath));
                }

                try
                {
                    cert = new X509Certificate2(CertificatePath, CertificatePassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                }
                catch (CryptographicException e) when (e.Message.Contains("The specified password is not correct"))
                {
                    throw new PSArgumentNullException(nameof(CertificatePassword), string.Format(Resources.PrivateKeyCertificateImportFailedPasswordIncorrect, nameof(CertificatePassword)));
                }

                // Ensure the certificate at the provided CertificatePath holds a private key
                if (!cert.HasPrivateKey)
                {
                    throw new PSArgumentException(string.Format(Resources.CertificateAtPathHasNoPrivateKey, CertificatePath), nameof(CertificatePath));
                }
            }
            else
            {
#if NETFRAMEWORK
                var x500Values = new List<string>();
                if (!MyInvocation.BoundParameters.ContainsKey("CommonName"))
                {
                    CommonName = ApplicationName;
                }
                if (!string.IsNullOrWhiteSpace(CommonName)) x500Values.Add($"CN={CommonName}");
                if (!string.IsNullOrWhiteSpace(Country)) x500Values.Add($"C={Country}");
                if (!string.IsNullOrWhiteSpace(State)) x500Values.Add($"S={State}");
                if (!string.IsNullOrWhiteSpace(Locality)) x500Values.Add($"L={Locality}");
                if (!string.IsNullOrWhiteSpace(Organization)) x500Values.Add($"O={Organization}");
                if (!string.IsNullOrWhiteSpace(OrganizationUnit)) x500Values.Add($"OU={OrganizationUnit}");

                string x500 = string.Join("; ", x500Values);

                if (ValidYears < 1 || ValidYears > 30)
                {
                    ValidYears = 10;
                }
                DateTime validFrom = DateTime.Today;
                DateTime validTo = validFrom.AddYears(ValidYears);

                byte[] certificateBytes = CertificateHelper.CreateSelfSignCertificatePfx(x500, validFrom, validTo, CertificatePassword);
                cert = new X509Certificate2(certificateBytes, CertificatePassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
#else
                if (!MyInvocation.BoundParameters.ContainsKey("CommonName"))
                {
                    CommonName = ApplicationName;
                }
                DateTime validFrom = DateTime.Today;
                DateTime validTo = validFrom.AddYears(ValidYears);
                cert = CertificateHelper.CreateSelfSignedCertificate(CommonName, Country, State, Locality, Organization, OrganizationUnit, CertificatePassword, CommonName, validFrom, validTo);
#endif
            }
            var pfxPath = string.Empty;
            var cerPath = string.Empty;


            if (Directory.Exists(OutPath))
            {
                pfxPath = Path.Combine(OutPath, $"{ApplicationName}.pfx");
                cerPath = Path.Combine(OutPath, $"{ApplicationName}.cer");
                byte[] certPfxData = cert.Export(X509ContentType.Pfx, CertificatePassword);
                File.WriteAllBytes(pfxPath, certPfxData);
                record.Properties.Add(new PSVariableProperty(new PSVariable("Pfx file", pfxPath)));

                byte[] certCerData = cert.Export(X509ContentType.Cert);
                File.WriteAllBytes(cerPath, certCerData);
                record.Properties.Add(new PSVariableProperty(new PSVariable("Cer file", cerPath)));
            }
            if (ParameterSpecified(nameof(Store)))
            {
                if (OperatingSystem.IsWindows())
                {
                    using (var store = new X509Store("My", Store))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        store.Add(cert);
                        store.Close();
                    }
                    Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, "Certificate added to store");
                }
            }
            return cert;
        }

        private bool AppExists(string appName, HttpClient httpClient, string token)
        {
            Host.UI.Write(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, $"Checking if application '{appName}' does not exist yet...");
            var azureApps = GraphHelper.GetAsync<RestResultCollection<AzureADApp>>(httpClient, $@"https://{PnP.Framework.AuthenticationManager.GetGraphEndPoint(AzureEnvironment)}/v1.0/applications?$filter=displayName eq '{appName}'&$select=Id", token).GetAwaiter().GetResult();
            if (azureApps != null && azureApps.Items.Any())
            {
                Host.UI.WriteLine();
                return true;
            }
            Host.UI.WriteLine(ConsoleColor.Green, Host.UI.RawUI.BackgroundColor, $"Success. Application '{appName}' can be registered.");
            return false;
        }

        private AzureADApp CreateApp(string loginEndPoint, HttpClient httpClient, string token, X509Certificate2 cert, string redirectUri, List<PermissionScope> scopes)
        {
            var expirationDate = cert.NotAfter.ToUniversalTime();
            var startDate = cert.NotBefore.ToUniversalTime();

            var scopesPayload = GetScopesPayload(scopes);
            var payload = new
            {
                isFallbackPublicClient = true,
                displayName = ApplicationName,
                signInAudience = "AzureADMyOrg",
                keyCredentials = new[] {
                    new {
                        customKeyIdentifier = cert.GetCertHashString(),
                        endDateTime = expirationDate,
                        keyId = Guid.NewGuid().ToString(),
                        startDateTime = startDate,
                        type= "AsymmetricX509Cert",
                        usage= "Verify",
                        key = Convert.ToBase64String(cert.GetRawCertData()),
                        displayName = cert.Subject,
                    }
                },
                publicClient = new
                {
                    redirectUris = new[] {
                        $"{loginEndPoint}/common/oauth2/nativeclient",
                        redirectUri
                    }
                },
                requiredResourceAccess = scopesPayload
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(payload));
            requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var azureApp = GraphHelper.PostAsync<AzureADApp>(httpClient, $"https://{AuthenticationManager.GetGraphEndPoint(AzureEnvironment)}/v1.0/applications", requestContent, token).GetAwaiter().GetResult();
            if (azureApp != null)
            {
                Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, $"App {azureApp.DisplayName} with id {azureApp.AppId} created.");
            }
            return azureApp;
        }

        private void StartConsentFlow(string loginEndPoint, AzureADApp azureApp, string redirectUri, string token, HttpClient httpClient, PSObject record, CmdletMessageWriter messageWriter, List<PermissionScope> scopes)
        {
            Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, $"Starting consent flow.");

            var resource = scopes.FirstOrDefault(s => s.resourceAppId == PermissionScopes.ResourceAppId_Graph) != null ? $"https://{AzureAuthHelper.GetGraphEndPoint(AzureEnvironment)}/.default" : "https://microsoft.sharepoint-df.com/.default";

            var consentUrl = $"{loginEndPoint}/{Tenant}/v2.0/adminconsent?client_id={azureApp.AppId}&scope={resource}&redirect_uri={redirectUri}";

            if (OperatingSystem.IsWindows() && !NoPopup)
            {
                var waitTime = 60;
                // CmdletMessageWriter.WriteFormattedWarning(this, $"Waiting {waitTime} seconds to launch the consent flow in a popup window.\n\nThis wait is required to make sure that Azure AD is able to initialize all required artifacts. You can always navigate to the consent page manually:\n\n{consentUrl}");

                var progressRecord = new ProgressRecord(1, "Please wait...", $"Waiting {waitTime} seconds to launch the consent flow in a popup window. This wait is required to make sure that Azure AD is able to initialize all required artifacts.");

                for (var i = 0; i < waitTime; i++)
                {
                    progressRecord.PercentComplete = Convert.ToInt32((Convert.ToDouble(i) / Convert.ToDouble(waitTime)) * 100);
                    WriteProgress(progressRecord);
                    // if (Convert.ToDouble(i) % Convert.ToDouble(10) > 0)
                    // {
                    //     Host.UI.Write(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, "-");
                    // }
                    // else
                    // {
                    //     Host.UI.Write(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, $"[{i}]");
                    // }
                    Thread.Sleep(1000);

                    // Check if CTRL+C has been pressed and if so, abort the wait
                    if (Stopping)
                    {
                        Host.UI.WriteLine("Wait cancelled. You can provide consent manually by navigating to");
                        Host.UI.WriteLine(consentUrl);
                        break;
                    }
                }
                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);

                if (!Stopping)
                {
                    // Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, $"[{waitTime}]");

                    // Host.UI.WriteLine();

                    if (ParameterSpecified(nameof(Interactive)))
                    {
                        using (var authManager = AuthenticationManager.CreateWithInteractiveLogin(azureApp.AppId, (url, port) =>
                         {
                             BrowserHelper.OpenBrowserForInteractiveLogin(url, port, true, cancellationTokenSource);
                         }, Tenant, "You successfully provided consent", "You failed to provide consent.", AzureEnvironment))
                        {
                            authManager.GetAccessToken(resource, Microsoft.Identity.Client.Prompt.Consent);
                        }
                    }
                    else
                    {
                        BrowserHelper.GetWebBrowserPopup(consentUrl, "Please provide consent", new[] { ("https://pnp.github.io/powershell/consent.html", BrowserHelper.UrlMatchType.StartsWith) }, cancellationTokenSource: cancellationTokenSource, cancelOnClose: false);
                    }
                    // Write results
                    WriteObject(record);
                }
            }
            else
            {
                Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, $"Open the following URL in a browser window to provide consent. This consent is required in order to use this application.\n\n{consentUrl}");
                WriteObject(record);
            }
        }
    }
}