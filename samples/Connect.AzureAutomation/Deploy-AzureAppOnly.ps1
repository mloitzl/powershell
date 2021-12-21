<# 
----------------------------------------------------------------------------

Creates an Azure AD App for PnP.PowerShell

Created:      Paul Bullock
Date:         04/12/2020
Disclaimer:   

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

.Synopsis

.Example

.Notes

    Default App Scopes: Sites.FullControl.All, Group.ReadWrite.All, User.Read.All

    References: 
        https://pnp.github.io/powershell/cmdlets/nightly/Register-PnPAzureADApp.html
        https://docs.microsoft.com/powershell/module/az.automation/New-AzAutomationCredential?view=azps-4.4.0

 ----------------------------------------------------------------------------
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $Tenant, #yourtenant.onmicrosoft.com

    [Parameter(Mandatory = $true)]
    [string] $SPTenant, # https://[thispart].sharepoint.com

    [Parameter(Mandatory = $false)]
    [string] $AppName = "PnP.PowerShell Automation",

    [Parameter(Mandatory = $true)]
    [securestring] $CertificatePassword, # <-- Use a nice a super complex password

    [Parameter(Mandatory = $false)]
    [int] $ValidForYears = 2, 
    
    [Parameter(Mandatory = $false)]
    [string] $CertCommonName = "PnP.PowerShell Automation"
)
begin{


    Write-Host "Let's get started..."
  
    # Get the location of the script to copy the script locally
    $location = Get-Location 
    
    if(!$CertCommonName){
        $CertCommonName = "pnp.$($Tenant)"
    }
    
}
process {
    
    # ----------------------------------------------------------------------------------
    #   Azure - Create Azure App and Certificate
    # ----------------------------------------------------------------------------------
    Write-Host " - Registering AD app and creating certificate..." -ForegroundColor Cyan

    $result = Register-PnPAzureADApp -ApplicationName $AppName -Tenant $Tenant -OutPath $location `
        -CertificatePassword $CertificatePassword -ValidYears $ValidForYears `
        -CommonName $CertCommonName -DeviceLogin

    $result | Format-List

    # Example Output:
    # Pfx file               : C:\Git\tfs\Script-Library\Azure\Automation\Deploy\PnP-PowerShell Automation.pfx
    # Cer file               : C:\Git\tfs\Script-Library\Azure\Automation\Deploy\PnP-PowerShell Automation.cer
    # AzureAppId             : c5beca65-0000-1111-2222-8a02cbbf4c4d
    # Certificate Thumbprint : 78D0F76D900000C8B9F77E64903B6D7AEF55D233

}
end{

  Write-Host "Script all done, enjoy! :)" -ForegroundColor Green
}