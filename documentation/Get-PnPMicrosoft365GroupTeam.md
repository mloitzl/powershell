---
Module Name: PnP.PowerShell
schema: 2.0.0
applicable: SharePoint Online
online version: https://pnp.github.io/powershell/cmdlets/Get-PnPMicrosoft365GroupTeam.html
external help file: PnP.PowerShell.dll-Help.xml
title: Get-PnPMicrosoft365GroupTeam
---
  
# Get-PnPMicrosoft365GroupTeam

## SYNOPSIS

**Required Permissions**

  * Microsoft Graph API : Group.Read.All

Returns the Microsoft Teams team behind a particular Microsoft 365 Group

## SYNTAX

```powershell
Get-PnPMicrosoft365GroupTeam -Identity <Microsoft365GroupPipeBind> [-Connection] [-Verbose] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet allows retrieval of details on the Microsoft Teams team connected to a Microsoft 365 Group

## EXAMPLES

### EXAMPLE 2
```powershell
Get-PnPMicrosoft365GroupTeam
```

Retrieves the Microsoft Teams team details behind the Microsoft 365 Group of the currently connected to site

### EXAMPLE 2
```powershell
Get-PnPMicrosoft365GroupTeam -Identity "IT Team"
```

Retrieves the Microsoft Teams team details behind the Microsoft 365 Group named "IT Team"

### EXAMPLE 3
```powershell
Get-PnPMicrosoft365GroupTeam -Identity e6212531-7f09-4c3b-bc2e-12cae26fb409
```

Retrieves the Microsoft Teams team details behind the Microsoft 365 Group with the provided Id

## PARAMETERS

### -Identity
The Identity of the Microsoft 365 Group

```yaml
Type: Microsoft365GroupPipeBind
Parameter Sets: (All)

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Connection
Optional connection to be used by the cmdlet. Retrieve the value for this parameter by either specifying -ReturnConnection on Connect-PnPOnline or by executing Get-PnPConnection.

```yaml
Type: PnPConnection
Parameter Sets: (All)

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Verbose
When provided, additional debug statements will be shown while executing the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

## RELATED LINKS

[Microsoft 365 Patterns and Practices](https://aka.ms/m365pnp)