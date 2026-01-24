<#
.SYNOPSIS
  Intune Policy Setting Search (WPF) via Microsoft Graph


.DESCRIPTION
  Searches selected Intune policy types (ADMX, Settings Catalog, Legacy Configuration Policies and Compliance Policies)
  for a user-provided string, and displays matches with Policy Name, Policy GUID, and Setting Found.


.NOTES
  Uses Microsoft Graph PowerShell SDK (Connect-MgGraph + Invoke-MgGraphRequest).
  Uses /beta for Intune policy settings endpoints
#>


[CmdletBinding()]
param (
    [string]$ClientId = "",
    [string]$TenantId = "",
    [string]$ClientSecret = ""
    # You can also set up an environment variable with your secret key in it and call it using $env:GraphKey
    # Replace "GraphKey" with the name of your environment variable. This can be a more secure way of pulling the key instead of hard coding it
)


Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
Add-Type -AssemblyName System.Windows.Forms


# -----------------------------
# Globals / Model
# -----------------------------
$script:Results = New-Object System.Collections.ObjectModel.ObservableCollection[psobject]
$script:IsConnected = $false
$script:GraphSession = $null
$script:ConnectionType = ""
$script:GCCHConnected = $false


# Store Policy Information to avoid needing to query every search, use radio to force refresh policies


# --------------------
# Policy Globals
# These are used to store policies returned from the graph calls to avoid unnecessary calls when searching for multiple terms
# --------------------
$script:ConfigurationPolicies = @()
$script:ConfigurationPolicies_Settings = @{}
$script:LegacyPolicies = @()


$script:AdmxPolicies = @()
$script:AdmxPolicies_Settings = @{}


$script:EndpointProtection = @()
$script:EndpointProtection_settings = @{}


$script:CompliancePolicies = @()




function Show-UiMessage {
    param([string]$Message, [string]$Title = "Intune Policy Search")
    [System.Windows.MessageBox]::Show($Message, $Title, 'OK', 'Information') | Out-Null
}


function Import-Graph {
    param([bool]$UseGCCH)


    # Delegated scope for Intune policy reads (used by the Graph Intune endpoints we query)
    $scopes = @("DeviceManagementConfiguration.Read.All")
    $AuthType = "Interactive"


    if (-not [string]::IsNullOrWhiteSpace($ClientId) -and
        -not [string]::IsNullOrWhiteSpace($TenantId) -and
        -not [string]::IsNullOrWhiteSpace($ClientSecret) 
    ) { $AuthType = "App" }


    try {
        # Ensure Graph is Imported prior to connecting
        if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Authentication)) {
            throw "Microsoft.Graph PowerShell SDK not found. Install with: Install-Module Microsoft.Graph -Scope CurrentUser"
        }


        Import-Module Microsoft.Graph.Authentication -ErrorAction Stop


        $script:GraphEnvironmentUsed = "Global"


        if ($UseGCCH) {
            $envs = Get-MgEnvironment
            $gcchName = "USGov"


            if ($envs.Name -contains $gcchName) {
                if ( $AuthType -eq "App") {
                
                    $SecureSecret = ConvertTo-SecureString "$ClientSecret" -AsPlainText -Force
                    $Cred = New-Object System.Management.Automation.PSCredential ($ClientId, $SecureSecret)


                    Connect-MgGraph -TenantId $TenantId -ClientSecretCredential $Cred -Environment $gcchName -NoWelcome -ErrorAction Stop | Out-Null
                    $script:GCCHConnected = $true
                }
                else {
                    if ([string]::IsNullOrEmpty($TenantId)) {
                        throw "Please update the TenantID variable in the script with your GCC-H Tenant. This ensures that WAM doesn't prevent successful sign-ins from a commercial device"
                    }


                    Connect-MgGraph -Scopes $scopes -Environment $gcchName -TenantId $TenantId -NoWelcome -ErrorAction Stop | Out-Null
                    $script:GCCHConnected = $true
                }
            }
            else {
                throw "$gcchName does not appear to be available in this Microsoft.Graph SDK install. Run Get-MgEnvironment to see valid values."
            }
        }
        else {
            if ( $AuthType -eq "App") {
                
                $SecureSecret = ConvertTo-SecureString "$ClientSecret" -AsPlainText -Force
                $Cred = New-Object System.Management.Automation.PSCredential ($ClientId, $SecureSecret)


                Connect-MgGraph -TenantId $TenantId -ClientSecretCredential $Cred -NoWelcome -ErrorAction Stop | Out-Null
                $script:GCCHConnected = $false
            }
            else {
                Connect-MgGraph -Scopes $scopes -NoWelcome -ErrorAction Stop | Out-Null
                $script:GCCHConnected = $false
            }


        }


        $ctx = Get-MgContext
        if (-not $ctx -or (-not $ctx.Account -and $AuthType -ne "App")) { throw "Graph connection established but no context/account returned." }


        if ($AuthType -eq "App") {
            if ($script:GCCHConnected) {
                $null = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.us/beta/deviceManagement/deviceConfigurations" -SessionVariable script:GraphSession
            }
            else {
                $null = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.com/beta/deviceManagement/deviceConfigurations" -SessionVariable script:GraphSession
            }
            $script:ConnectionType = "App"
        }
        else {
            if ($script:GCCHConnected) {
                $null = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.us/beta/deviceManagement/deviceConfigurations" -SessionVariable script:GraphSession
            }
            else {
                $null = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.com/beta/deviceManagement/deviceConfigurations" -SessionVariable script:GraphSession
            }
            $script:ConnectionType = "User"
        }
       
        $script:IsConnected = $true
        return $true
    }
    catch {
        $script:IsConnected = $false
        throw
    }
}




function Invoke-GraphGetAll {
    param(
        [Parameter(Mandatory)] [string] $Uri,
        [Parameter(Mandatory)] $GraphSession
    )


    $items = @()
    $next = $Uri


    $GovUrls = ("https://graph.microsoft.us/beta/`$metadata#microsoft.graph.groupPolicyDefinition", "https://graph.microsoft.us/beta/`$metadata#microsoft.graph.groupPolicyPresentation")
    $CommercialUrls = ("https://graph.microsoft.com/beta/`$metadata#microsoft.graph.groupPolicyDefinition", "https://graph.microsoft.com/beta/`$metadata#microsoft.graph.groupPolicyPresentation")


    while ($next) {
        $resp = Invoke-MgGraphRequest -Method GET -Uri $next -GraphRequestSession $GraphSession -ErrorAction Stop
        if ($resp.value) { 
            $items += $resp.value 
        }
        elseif ($resp."@odata.context" -eq "https://graph.microsoft.com/beta/`$metadata#deviceManagement/reusablePolicySettings/`$entity") {
            $items += $resp
        }
        elseif ($resp."@odata.context" -in $CommercialUrls -or $resp."@odata.context" -in $GovUrls) {
            # GPDefs and GPPresentation do not have a value so we do need to return any object that is present
            $items += $resp
        }
        $next = $resp.'@odata.nextLink'
    }


    return $items
}


# -----------------------------
# Graph Query Functions
# -----------------------------


function Get-ConfigPolicies {
    param([Parameter(Mandatory)] $GraphSession)


    $uri = "/beta/deviceManagement/configurationPolicies"
    Invoke-GraphGetAll -Uri $uri -GraphSession $GraphSession
}


function Get-ConfigPolicySettings {
    param([Parameter(Mandatory)][string]$PolicyId,
        [Parameter(Mandatory)] $GraphSession)


    $uri = "/beta/deviceManagement/configurationPolicies/$PolicyId/settings"
    Invoke-GraphGetAll -Uri $uri -GraphSession $GraphSession
}


function Get-ReusableSettings {
    param([Parameter(Mandatory)][string]$PolicyId,
        [Parameter(Mandatory)] $GraphSession)


    $uri = "beta/deviceManagement/reusablePolicySettings/$PolicyId"
    Invoke-GraphGetAll -uri $uri -GraphSession $GraphSession
}


function Get-LegacyConfigPolicies {
    param([Parameter(Mandatory)] $GraphSession)


    $uri = "/beta/deviceManagement/deviceConfigurations"
    Invoke-GraphGetAll -Uri $uri -GraphSession $GraphSession
}


function Get-GroupPolicyConfigurations {
    param([Parameter(Mandatory)] $GraphSession)


    $uri = "/beta/deviceManagement/groupPolicyConfigurations?`$select=id,displayName"
    Invoke-GraphGetAll -Uri $uri -GraphSession $GraphSession
}


function Get-GroupPolicyDefinitionValues {
    param([Parameter(Mandatory)][string]$PolicyId,
        [Parameter(Mandatory)] $GraphSession)


    $SettingValues = [System.Collections.Generic.List[object]]::new()


    $uri = "/beta/deviceManagement/groupPolicyConfigurations/$PolicyId/definitionValues/"
    $Definitions = Invoke-GraphGetAll -Uri $uri -GraphSession $GraphSession


    foreach ($Definition in $Definitions) {
        if (-not [string]::IsNullOrEmpty($definition.Id)) {
            $id = $definition.id
            $defuri = "/beta/deviceManagement/groupPolicyConfigurations/$PolicyId/definitionValues/$id/definition"
            $presUri = "/beta/deviceManagement/groupPolicyConfigurations/$PolicyID/definitionValues/$id/presentationValues"
            $defVal = Invoke-GraphGetAll -Uri $defUri -GraphSession $GraphSession
            $value = if ($Definition.Enabled) { "Enabled" } else { "Disabled" }


            if ($defVal) {
                if ($value -eq "Enabled") {
                    # Check for presentation values as well
                    $PresValues = Invoke-GraphGetAll -Uri $presUri -GraphSession $GraphSession
                    if ($presValues.Id) {
                        foreach ($presentation in $PresValues) {
                            $PresValue = $presentation.Value
                            $presId = $Presentation.id
                            $itemVal = ""
                            if ($presentation."@odata.type" -eq "#microsoft.graph.groupPolicyPresentationValueList") {
                                foreach ($presVal in $PresValues.values) {
                                    $ItemVal += "{Name: $($presVal.name), Value: $($presVal.Value)} "
                                }
                            }
                            else {
                                $PresDefinitionUri = "/beta/deviceManagement/groupPolicyConfigurations/$PolicyID/definitionValues/$id/presentationValues/$presId/presentation"
                                $PresDefinition = Invoke-GraphGetAll -Uri $presDefinitionUri -GraphSession $GraphSession
                                foreach ($presDef in $PresDefinition.Items) {
                                    if ($presDef.Value -eq $PresValue) {
                                        $ItemVal = $PresDef.DisplayName
                                        break;
                                    }
                                }
                            }


                            $reportingValue = ""
                            if ([string]::IsNullOrEmpty($presValue)) {
                                $reportingValue = "$value - $itemVal"
                            }
                            else {
                                $reportingValue = "$value - [$presValue] $itemVal"
                            }
                            $SettingValues.Add([pscustomobject]@{
                                    Name     = $defVal.displayName
                                    Platform = $defVal.supportedOn
                                    Value    = $ReportingValue
                                })
                        }
                    }
                    else {
                        # No Presentation Values found
                        $SettingValues.Add([pscustomobject]@{
                                Name     = $defVal.displayName
                                Platform = $defVal.supportedOn
                                Value    = $Value
                            })
                    }
                }
                elseif ($Value -eq "Disabled") {
                    $SettingValues.Add([pscustomobject]@{
                            Name     = $defVal.displayName
                            Platform = $defVal.supportedOn
                            Value    = $Value
                        })
                }
            }
        }
    } # End of ForEach


    return $SettingValues
}


function Get-CompliancePolicies {
    param([Parameter(Mandatory)] $GraphSession)
    $uri = "/beta/deviceManagement/deviceCompliancePolicies"
    Invoke-GraphGetAll -Uri $uri -GraphSession $GraphSession
}


function Get-PolicyPlatformType {
    param(
        $Policy
    )


    # ADMX
    if ($Policy -is [pscustomobject]) {
        if ($Policy."Platform" -like "*android*") {
            return "Android"
        }
        elseif ($Policy."Platform" -match "ios") {
            return "iOS"
        }
        elseif ($Policy."Platform" -match "Windows") {
            return "Windows"
        }
        elseif ($Policy."Platform" -match "Linux") {
            return "Linux"
        }
        elseif ($Policy."Platform" -match "Mac") {
            return "MacOS"
        }
    }


    # Configuration Templates and Compliance
    if ($Policy.ContainsKey("@odata.type")) {


        if ($Policy."@odata.type" -like "*android*") {
            return "Android"
        }
        elseif ($Policy."@odata.type" -match "ios") {
            return "iOS"
        }
        elseif ($Policy."@odata.type" -match "Windows") {
            return "Windows"
        }
        elseif ($Policy."@odata.type" -match "Linux") {
            return "Linux"
        }
        elseif ($Policy."@odata.type" -match "Mac") {
            return "MacOS"
        }
    }
    # Settings Catalog
    elseif ($Policy.ContainsKey("Platforms")) {


        if ($Policy."Platforms" -like "*android*") {
            return "Android"
        }
        elseif ($Policy."Platforms" -match "ios") {
            return "iOS"
        }
        elseif ($Policy."Platforms" -match "Windows") {
            return "Windows"
        }
        elseif ($Policy."Platforms" -match "Linux") {
            return "Linux"
        }
        elseif ($Policy.'Platforms' -match "Mac") {
            return "MacOS"
        }
    }


}




# -----------------------------
# Helpers
# -----------------------------
function Get-ResultObject {
    param (
        [string]$Type,
        $PolicyObject,
        [string]$Key,
        [string]$Needle
    )

    $Ignores = @("createdDateTime",
        "description",
        "displayName",
        "version",
        "supportsScopeTags",
        "@odata.type",
        "roleScopeTagIds",
        "lastModifiedDateTime",
        "id"
    )


    $Platform = Get-PolicyPlatformType -Policy $PolicyObject


    $val = $PolicyObject.$Key
    if ($val -is [hashtable]) {
        if ($val.ContainsKey("State") -and $val.ContainsKey("LocalUsersOrGroups")) {
            $state = $val["State"]
            $UserGroupString = "$($state): "
            foreach ($userOrGroup in $val["localUsersOrGroups"]) {
                $name = $userOrGroup.name
                $securityIdentifier = $userOrGroup.securityIdentifier


                $UserGroupString += "$name ($securityIdentifier) | "
            }


            return [pscustomobject]@{
                Type         = $Type
                Platform     = $Platform
                PolicyName   = $PolicyObject.displayName
                PolicyId     = $PolicyObject.id
                SettingFound = $key
                SettingValue = $UserGroupString
            }
        }
        elseif ($val.ContainsKey("configurations")) {
            $Objs = [System.Collections.Generic.List[object]]::new()
            foreach ($config in $val.Configurations) {
                $Objs.Add([pscustomobject]@{
                        Type         = $Type
                        Platform     = $Platform
                        PolicyName   = $PolicyObject.displayName
                        PolicyId     = $PolicyObject.id
                        SettingFound = $key
                        SettingValue = "$($config.key) = $($config.Value)"
                    })
            }


            return $Objs
        }
        else {
            foreach ($valKey in $val.Keys) {
                if ( $valkey -notin $Ignores) {
                    $arrayVal = ""
                    if ($val.$valkey -is [array]) {
                        foreach ($obj in $val.$valkey ) {
                            $arrayVal += "$($val.$valkey),"
                        }
                    }
                    elseif ($val.$valkey -is [hashtable]) {
                        $hash = $val.$valkey
                        foreach ($hashkey in $hash.Keys ) {
                            $arrayVal += "$($hashkey): $($hash.$hashkey),"
                        }
                    }
                    else {
                        $arrayVal = $val.$valkey
                    }


                    return [pscustomobject]@{
                        Type         = $Type
                        Platform     = $Platform
                        PolicyName   = $PolicyObject.displayName
                        PolicyId     = $PolicyObject.id
                        SettingFound = $valkey
                        SettingValue = $arrayVal
                    }
                }
            }
        }


    }
    else {
        if ($key -eq "extendedKeyUsages") {
            $Value = $PolicyObject.$key
            return [pscustomobject]@{
                Type         = $Type
                Platform     = $Platform
                PolicyName   = $PolicyObject.displayName
                PolicyId     = $PolicyObject.id
                SettingFound = $key
                SettingValue = "$($Value.Name)($($value.objectIdentifier))"
            }
        }
        elseif ($key -eq "scepServerUrls") {
            $UrlList = ""
            $UrlCount = $PolicyObject.$Key.count
            $index = 1 
            foreach ($url in $PolicyObject.$key) {
                if ($index -eq $UrlCount) {
                    $UrlList += $url
                }
                else {
                    $UrlList += "$url, "
                }
            }


            return [pscustomobject]@{
                Type         = $Type
                Platform     = $Platform
                PolicyName   = $PolicyObject.displayName
                PolicyId     = $PolicyObject.id
                SettingFound = $key
                SettingValue = $UrlList
            }


        }
        elseif ($key -eq "omaSettings") {
            $Objs = [System.Collections.Generic.List[object]]::new()
            foreach ($set in $PolicyObject.$key) {
                $setValue = "$($set.displayName) -  $($set.OmaUri)"


                $Objs.Add([pscustomobject]@{
                        Type         = $Type
                        Platform     = $Platform
                        PolicyName   = $PolicyObject.displayName
                        PolicyId     = $PolicyObject.id
                        SettingFound = $key
                        SettingValue = $setValue
                    })
            }


            return $Objs
        }
        elseif ($key -eq "customSubjectAlternativeNames") {
            $Objs = [System.Collections.Generic.List[object]]::new()
            foreach ($customSAN in $PolicyObject.$key) {
                $Objs.Add([pscustomobject]@{
                        Type         = $Type
                        Platform     = $Platform
                        PolicyName   = $PolicyObject.displayName
                        PolicyId     = $PolicyObject.id
                        SettingFound = $key
                        SettingValue = "$($customSAN.sanType) - $($customSAN.name)"
                    })
            }


            return $Objs
        }
        elseif ($Key -eq "kioskModeApps") {
            $AppValue = ""


            foreach ($app in $PolicyObject.$Key) {
                $AppValue += "{$($app.publisher), $($app.appId), $($app.appStoreUrl), $($app.name)},"
            }


            return [pscustomobject]@{
                Type         = $Type
                Platform     = $Platform
                PolicyName   = $PolicyObject.displayName
                PolicyId     = $PolicyObject.id
                SettingFound = $key
                SettingValue = $AppValue
            }
        }
        elseif ($Key -eq "silentCertificateAccessDetails") {
            $AppValue = ""


            foreach ($app in $PolicyObject.$Key) {
                $AppValue += "{$($app.packageId),"
            }


            return [pscustomobject]@{
                Type         = $Type
                Platform     = $Platform
                PolicyName   = $PolicyObject.displayName
                PolicyId     = $PolicyObject.id
                SettingFound = $key
                SettingValue = $AppValue
            }
        }
        elseif ($key -eq "trustedServerCertificateNames") {
            $certValue = ""
            foreach ($cert in $PolicyObject.$key) {
                $certValue += "$($cert) "
            }


            return [pscustomobject]@{
                Type         = $Type
                Platform     = $Platform
                PolicyName   = $PolicyObject.displayName
                PolicyId     = $PolicyObject.id
                SettingFound = $key
                SettingValue = $certValue
            }
        }
        elseif ($key -eq "blockedUrls") {
            $urls = ""


            foreach ($url in $PolicyObject.$key) {
                $urls += "$url "
            }


            return [pscustomobject]@{
                Type         = $Type
                Platform     = $Platform
                PolicyName   = $PolicyObject.displayName
                PolicyId     = $PolicyObject.id
                SettingFound = $key
                SettingValue = $urls
            }
        }
        elseif ($val -is [array]) {


            $Objs = [System.Collections.Generic.List[object]]::new()
            # Catch all for any unknown or new settings that may come through as arrays/hashtables within arrays


            foreach ($entry in $val) {
                if ($entry -is [hashtable]) {
                    foreach ($hashKey in $entry) {


                        $Objs.Add([pscustomobject]@{
                                Type         = $Type
                                Platform     = $Platform
                                PolicyName   = $PolicyObject.displayName
                                PolicyId     = $PolicyObject.id
                                SettingFound = $key
                                SettingValue = "$($hashKey): $($entry.$hashKey)"
                            })
                    }
                }
            }


            return $Objs
        }
        else {


            return [pscustomobject]@{
                Type         = $Type
                Platform     = $Platform
                PolicyName   = $PolicyObject.displayName
                PolicyId     = $PolicyObject.id
                SettingFound = $key
                SettingValue = $PolicyObject.$key
            }
        }
    }
}


function Add-ResultRow {
    param(
        [Parameter(Mandatory)][string]$Type,
        [string] $Platform,
        [Parameter(Mandatory)][string]$PolicyName,
        [Parameter(Mandatory)][string]$PolicyId,
        [Parameter(Mandatory)][string]$SettingFound,
        [string]$SettingValue
    )


    if ($SettingValue -isnot [string]) {
        write-host $SettingValue
        return
    }


    $script:Results.Add([pscustomobject]@{
            Type         = $Type
            Platform     = $Platform
            PolicyName   = $PolicyName
            PolicyGuid   = $PolicyId
            SettingFound = $SettingFound
            SettingValue = $SettingValue
        }) | Out-Null
}


$width = [System.Windows.SystemParameters]::PrimaryScreenWidth
$height = [System.Windows.SystemParameters]::PrimaryScreenHeight


if ($width -ne $null -and $width -gt 0) {
    $width = $width * 0.8
}


if ($height -ne $null -and $height -gt 0) {
    $height = $height * 0.8
}


# -----------------------------
# WPF UI (XAML)
# -----------------------------
[xml]$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Intune Policy Search" Height="$height" Width="$width"
        WindowStartupLocation="CenterScreen">
  <Grid Margin="12">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>


    <Border Grid.Row="0" Margin="0,10,0,10" BorderBrush="#DDD" BorderThickness="1" Padding="10" CornerRadius="6">
    <Grid>
        <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>      <!-- Main content (search + checkboxes) -->
        <ColumnDefinition Width="240"/>    <!-- Status area -->
        </Grid.ColumnDefinitions>


        <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>     <!-- Search row -->
        <RowDefinition Height="Auto"/>     <!-- Checkbox row -->
        </Grid.RowDefinitions>


        <Grid Grid.Row="0" Grid.Column="0">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>  <!-- label -->
            <ColumnDefinition Width="*"/>     <!-- textbox -->
            <ColumnDefinition Width="Auto"/>  <!-- buttons -->
        </Grid.ColumnDefinitions>


        <TextBlock Grid.Column="0"
                    VerticalAlignment="Center"
                    Margin="0,0,10,0"
                    Text="Search string:"/>


        <TextBox x:Name="TxtSearch"
                Grid.Column="1"
                Height="28"
                MinWidth="200"
                Margin="0,0,10,0"/>


        <StackPanel Grid.Column="2"
                    Orientation="Horizontal"
                    HorizontalAlignment="Right">
            <Button x:Name="BtnSearch"
                    Width="120"
                    Height="28"
                    Padding="12,0"
                    Content="Search"
                    Margin="0,0,8,0"/>
            <Button x:Name="BtnSave"
                    Width="120"
                    Height="28"
                    Padding="12,0"
                    Content="Save results"/>
        </StackPanel>
        </Grid>


    <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal" Margin="0,10,0,0">
        <TextBlock VerticalAlignment="Center" Margin="0,0,10,0" Text="Policy types:"/>
        <CheckBox x:Name="ChkConfigPolicies"
                Content="Device Configurations"
                IsChecked="True"
                Margin="0,0,16,0"/>
        <CheckBox x:Name="ChkCompliance"
                Content="Device Compliance"
                IsChecked="True"
                Margin="0,0,16,0"/>
        <CheckBox x:Name="ChkADMX"
                Content="Administrative Templates"
                IsChecked="True"
                Margin="0,0,16,0"/>


    </StackPanel>


    <StackPanel Grid.Row="0" Grid.RowSpan="2" Grid.Column="1" HorizontalAlignment="Right">
        <!-- Align status text to top -->
        <TextBlock x:Name="TxtStatus"
                Text="Ready"
                Foreground="#555"
                TextWrapping="Wrap"
                Width="220"
                VerticalAlignment="Top"/>
        
        <!-- Add Refresh Policies radio slider -->
        <StackPanel Orientation="Horizontal" Margin="0,10,0,0">
            <TextBlock Text="Refresh Policies:" VerticalAlignment="Center" Margin="0,0,10,0"/>
            <RadioButton x:Name="RbRefreshOn" Content="On" GroupName="RefreshPolicies" Margin="0,0,10,0"/>
            <RadioButton x:Name="RbRefreshOff" Content="Off" GroupName="RefreshPolicies" IsChecked="True"/>
        </StackPanel>
    </StackPanel>
    </Grid>
    </Border>


    <DataGrid Grid.Row="1" x:Name="GridResults" AutoGenerateColumns="False" IsReadOnly="True"
              CanUserAddRows="False" ItemsSource="{Binding}" HeadersVisibility="Column"
              BorderBrush="#DDD" BorderThickness="1"
              HorizontalScrollBarVisibility="Auto">
      <DataGrid.Columns>
        <DataGridTextColumn Header="Type" Binding="{Binding Type}" Width ="*" />
        <DataGridTextColumn Header="Platform" Binding="{Binding Platform}" Width="*" />
        <DataGridTextColumn Header="Policy Name" Binding="{Binding PolicyName}" Width="*"/>
        <DataGridTextColumn Header="Policy GUID" Binding="{Binding PolicyGuid}" Width="260"/>
        <DataGridTextColumn Header="Setting" Binding="{Binding SettingFound}" Width="*"/>
        <DataGridTextColumn Header="Value" Binding="{Binding SettingValue}" Width="*" />
      </DataGrid.Columns>
    </DataGrid>


    <!-- Row 3: Bottom Connect + Clear -->
    <Border Grid.Row="2"
            BorderBrush="#DDD"
            BorderThickness="1"
            Padding="10"
            Margin="0,10,0,10"
            CornerRadius="6">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>


            <Button x:Name="BtnConnect"
                    Grid.Column="0"
                    Width="160"
                    Height="30"
                    Margin="0,0,10,0"
                    Content="Connect to Graph"/>


            <CheckBox x:Name="ChkGcch"
                    Grid.Column="1"
                    VerticalAlignment="Center"
                    Margin="0,0,10,0"
                    Content="GCC High"/>


            <TextBlock x:Name="TxtConnStatus"
                    Grid.Column="2"
                    VerticalAlignment="Center"
                    Margin="0,0,10,0"/>


            <Button x:Name="BtnClear"
                    Grid.Column="2"
                    Width="120"
                    Height="30"
                    HorizontalAlignment="Right"
                    Content="Clear"/>
        </Grid>
    </Border>




    <!-- Row 4: Footer -->
    <TextBlock Grid.Row="3" Margin="0,10,0,0" Foreground="#777"
               Text="Tip: Searches use Graph /beta for detailed settings. For large tenants this can take time; narrow your search string when possible."/>
  </Grid>
</Window>
"@


$reader = New-Object System.Xml.XmlNodeReader $xaml
$window = [Windows.Markup.XamlReader]::Load($reader)


# Find controls
$BtnConnect = $window.FindName("BtnConnect")
$ChkGcch = $window.FindName("ChkGcch")
$TxtConnStatus = $window.FindName("TxtConnStatus")
$BtnClear = $window.FindName("BtnClear")


$TxtSearch = $window.FindName("TxtSearch")
$BtnSearch = $window.FindName("BtnSearch")


$ChkConfigPolicies = $window.FindName("ChkConfigPolicies")
$ChkADMX = $window.FindName("ChkADMX")


$ChkCompliance = $window.FindName("ChkCompliance")


$BtnSave = $window.FindName("BtnSave")
$TxtStatus = $window.FindName("TxtStatus")
$GridResults = $window.FindName("GridResults")


$script:RefreshPolicies = $false
$RbRefreshOn = $Window.FindName("RbRefreshOn")
$RbRefreshOff = $Window.FindName("RbRefreshOff")


# Bind results
$GridResults.ItemsSource = $script:Results


# -----------------------------------
# Runspace setup for faster searches
# -----------------------------------




# ----- RunspacePool setup (call once after you load functions/modules and build UI) -----


# Defaults
$script:SearchAsync = $null
$script:SearchTimer = $null
$script:SearchPS = $null


# Create once
$script:StatusUpdateQueue = [System.Collections.Concurrent.ConcurrentQueue[string]]::new()


$script:StatusTimer = New-Object System.Windows.Threading.DispatcherTimer
$script:StatusTimer.Interval = [TimeSpan]::FromMilliseconds(100)


$script:StatusTimer.Add_Tick({
        $msg = $null
        $last = $null


        while ($script:StatusUpdateQueue.TryDequeue([ref]$msg)) {
            $last = $msg
            $msg = $null
        }


        if ($last) { $TxtStatus.Text = $last }
    })


$script:StatusTimer.Start()


# Create an InitialSessionState and inject your functions into it


$iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault()


# Import specific functions
foreach ($fn in @(
        'Get-ConfigPolicies',
        'Get-ConfigPolicySettings',
        'Get-GroupPolicyConfigurations',
        'Get-GroupPolicyDefinitionValues',
        'Get-LegacyConfigPolicies',  
        'Get-CompliancePolicies', 
        'Invoke-GraphGetAll',
        'Get-ResultObject',
        'Get-PolicyPlatformType',
        'Get-ReusableSettings'
    )) {
    $def = (Get-Command $fn).Definition
    $entry = New-Object System.Management.Automation.Runspaces.SessionStateFunctionEntry $fn, $def
    $iss.Commands.Add($entry)
}




# If your Get-* functions rely on modules, import them here as well:
# Example:
# $iss.ImportPSModule(@('Microsoft.Graph.Authentication','Microsoft.Graph.DeviceManagement'))


# Create a pool (tune maxRunspaces if you plan parallel searches)
$script:SearchPool = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspacePool($iss)
$script:SearchPool.SetMinRunspaces(1) | Out-Null
$script:SearchPool.SetMaxRunspaces(2) | Out-Null
$script:SearchPool.ApartmentState = 'STA'
$script:SearchPool.ThreadOptions = 'ReuseThread'
$script:SearchPool.Open()




# -----------------------------
# UI Events
# -----------------------------


$BtnConnect.Add_Click({
        try {
            $TxtConnStatus.Text = "Connecting..."
            $ok = Import-Graph -UseGCCH ([bool]$ChkGcch.IsChecked)
            if ($ok) {
                if ($script:ConnectionType -eq "User") {
                    $Account = (Get-MgContext).Account
                    $TxtConnStatus.Text = "(Connected: $Account)"
                }
                elseif ($script:ConnectionType -eq "App") {
                    $TxtConnStatus.Text = "(Connected: App Authentication)"
                }
            }
        }
        catch {
            $TxtConnStatus.Text = "(Connection Failed)"
            Show-UiMessage -Message $_.Exception.Message -Title "Graph Connection Error"
        }
    })


$BtnClear.Add_Click({
        $TxtSearch.Text = ""
        $script:Results.Clear()
        $TxtStatus.Text = "Cleared"
    })


$BtnSave.Add_Click({
        if ($script:Results.Count -eq 0) {
            Show-UiMessage "No results to save."
            return
        }


        $dlg = New-Object Microsoft.Win32.SaveFileDialog
        $dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        $dlg.FileName = "IntunePolicySearchResults.csv"


        if ($dlg.ShowDialog() -eq $true) {
            try {
                $script:Results | Export-Csv -Path $dlg.FileName -NoTypeInformation -Encoding UTF8
                $TxtStatus.Text = "Saved: $($dlg.FileName)"
            }
            catch {
                Show-UiMessage -Message $_.Exception.Message -Title "Save Error"
            }
        }
    })


    
$RbRefreshOn.Add_Checked({
        $script:RefreshPolicies = $true
    })


$RbRefreshOff.Add_Checked({
        $script:RefreshPolicies = $false
    })


$BtnSearch.Add_Click({


        $needle = $TxtSearch.Text
        if ([string]::IsNullOrWhiteSpace($needle)) { Show-UiMessage "Enter a search string."; return }
        if (-not $script:IsConnected) { Show-UiMessage "Connect to Microsoft Graph first."; return }


        $doConfig = [bool]$ChkConfigPolicies.IsChecked
        $doCompliance = [bool]$ChkCompliance.IsChecked  
        $doADMX = [bool]$ChkADMX.IsChecked
   
        if (-not ($doConfig -or $doCompliance -or $doADMX -or $doIntent)) {
            Show-UiMessage "Select at least one policy type to search."
            return
        }


        # UI prep
        $BtnSearch.IsEnabled = $false
        $TxtStatus.Text = "Searching..."
        $script:Results.Clear()


        if ($RefreshPolicies) {
            $TxtStatus.Text = "Querying Graph for Policies..."
        }
        elseif ($script:ConfigurationPolicies.Count -eq 0) {
            $TxtStatus.Text = "Querying Graph for Policies..."
        }
        elseif (!$RefreshPolicies) {
            $TxtStatus.Text = "Using Cached Policies..."
        }


        # Status reporter delegate - SAFE to pass into runspace
        $statusReporter = [Action[string]] {
            param($msg)
            $script:StatusUpdateQueue.Enqueue($msg)
        }




        # Build the runspace PowerShell instance
        $ps = [System.Management.Automation.PowerShell]::Create()
        $ps.RunspacePool = $script:SearchPool


        # The actual work (runs inside the runspace)
        $sb = {
            param(
                [string]$Needle,
                [bool]$DoConfig,
                [bool]$doCompliance, 
                [bool]$DoADMX,
                [Action[string]]$Status,
                $GraphSession,
                $ConfigPolicies,
                $LegacyPolicies,
                $AdmxPolicies,
                $ConfigSettings,
                $AdmxSettings,
                $RefreshPolicies,
                $CompliancePolicies
            )


            # Use a List for fast adds
            $results = [System.Collections.Generic.List[object]]::new()


            $Ignores = @("createdDateTime",
                "description",
                "displayName",
                "version",
                "supportsScopeTags",
                "@odata.type",
                "roleScopeTagIds",
                "lastModifiedDateTime",
                "id"
            )


            try {
                if ($DoConfig) {


                    $Status.Invoke("Searching Device Configurations...")


                    $pols = $null
                    if ($ConfigPolicies.Count -gt 0 -and !$RefreshPolicies) {
                        $pols = $ConfigPolicies
                    }
                    elseif ($ConfigPolicies.Count -eq 0 -or $RefreshPolicies) {
                        $pols = Get-ConfigPolicies -GraphSession $GraphSession
                        $ConfigPolicies = $pols;
                    }


                    $idx = 0
                    foreach ($p in $pols) {
                    
                        $idx++


                        if (($idx % 10) -eq 0) { $Status.Invoke("Searching... $idx/$($pols.Count)") }


                        $Platform = Get-PolicyPlatformType -Policy $p


                        $settings = $null


                        foreach ($Key in $ConfigSettings.Keys) {
                            if ($key -eq $p.id) {
                                $settings = $ConfigSettings."$($p.id)"
                                break
                            }
                        }
                    
                        if ($settings -eq $null -or $RefreshPolicies) {
                            $settings = Get-ConfigPolicySettings -PolicyId $p.id -GraphSession $GraphSession
                            $ConfigSettings["$($p.id)"] = $settings
                        }


                        $fireWallRuleName = ""
                        foreach ($s in $settings) {


                            if ($s.settingInstance.groupSettingCollectionValue) {
                                # Collection of settings found, parse them all to ensure we get accurate results
                                foreach ($groupSetting in $s.settingInstance.groupSettingCollectionValue.Children) {


                                    if ($groupSetting.settingDefinitionId -eq "vendor_msft_firewall_mdmstore_firewallrules_{firewallrulename}_name") {
                                        $fireWallRuleName = $groupSetting.simpleSettingValue.Value
                                    }


                                    if ($groupSetting.settingDefinitionId -like "*$needle*") {
                                        if ($groupSetting.choiceSettingValue) {
                                            
                                            $choiceVal = $groupSetting.choiceSettingValue.value.Replace("$($groupSetting.settingDefinitionId)_", "")


                                            $results.Add([pscustomobject]@{
                                                    Type         = "Settings Catalog"
                                                    Platform     = $Platform
                                                    PolicyName   = $p.name
                                                    PolicyId     = $p.id
                                                    SettingFound = "$($groupSetting.settingDefinitionId)".Replace("{firewallrulename}", $fireWallRuleName)
                                                    SettingValue = $choiceVal
                                                })
                                        }
                                        elseif ($groupSetting.choiceSettingCollectionValue) {
                                            
                                            # Parse out Collection Values
                                            $collectionValues = ""
                                            foreach ($choiceVal in $groupSetting.choiceSettingCollectionValue) {
                                                $parsedVal = "$($choiceVal.value)".Replace("$($groupSetting.settingDefinitionId)_", "")
                                                $collectionValues += "$($parsedVal) "    
                                            }


                                            $results.Add([pscustomobject]@{
                                                    Type         = "Settings Catalog"
                                                    Platform     = $Platform
                                                    PolicyName   = $p.name
                                                    PolicyId     = $p.id
                                                    SettingFound = "$($groupSetting.settingDefinitionId)".Replace("{firewallrulename}", $fireWallRuleName)
                                                    SettingValue = $collectionValues
                                                })                                          
                                        }
                                        elseif ($groupSetting.simpleSettingValue) {
                                            if (-not $groupSetting.settingDefinitionId -eq "vendor_msft_firewall_mdmstore_firewallrules_{firewallrulename}_name") {
                                                
                                                $results.Add([pscustomobject]@{
                                                        Type         = "Settings Catalog"
                                                        Platform     = $Platform
                                                        PolicyName   = $p.name
                                                        PolicyId     = $p.id
                                                        SettingFound = "$($groupSetting.settingDefinitionId)".Replace("{firewallrulename}", $fireWallRuleName)
                                                        SettingValue = $groupSetting.simpleSettingValue.value
                                                    })
                                            }


                                        }
                                        elseif ($groupSetting.simpleSettingCollectionValue) {
                                            # Find CollectionValue GUIDs and parse out selections
                                            if ($groupSetting.settingDefinitionId -eq "vendor_msft_firewall_mdmstore_firewallrules_{firewallrulename}_remoteaddressdynamickeywords") {
                                                # Reusable settings for firewall - parse differently
                                                # value is the GUID of the reusable setting
                                                $ReusableSettingNames = ""






                                                foreach ($reusable in $groupSetting.simpleSettingCollectionValue) {


                                                    $reusableSetting = Get-ReusableSettings -PolicyId "$($reusable.Value)" -GraphSession $GraphSession
                                                    if ($reusableSetting) {
                                                        $ReusableSettingNames += "$($reusableSetting.displayName) | "
                                                    }
                                                    
                                                }


                                                $results.Add([pscustomobject]@{
                                                        Type         = "Settings Catalog"
                                                        Platform     = $Platform
                                                        PolicyName   = $p.name
                                                        PolicyId     = $p.id
                                                        SettingFound = "$($groupSetting.settingDefinitionId)".Replace("{firewallrulename}", $fireWallRuleName)
                                                        SettingValue = $ReusableSettingNames
                                                    })


                                            }
                                            else {
                                                $simpleSettingVal = ""
                                                foreach ($ssV in $groupSetting.simpleSettingCollectionValue) {
                                                    $simpleSettingVal += "$($ssV.value) | "
                                                }


                                                $results.Add([pscustomobject]@{
                                                        Type         = "Settings Catalog"
                                                        Platform     = $Platform
                                                        PolicyName   = $p.name
                                                        PolicyId     = $p.id
                                                        SettingFound = "$($groupSetting.settingDefinitionId)".Replace("{firewallrulename}", $fireWallRuleName)
                                                        SettingValue = $simpleSettingVal
                                                    })
                                            }
                                        }
                                    }
                                }
                            }
                            else {
                                if ($s.settingInstance.choiceSettingValue.Children) {
                                    foreach ($child in $s.settingInstance.choiceSettingValue.Children) {
                                        if ($child.settingDefinitionId -like "*$needle*") {
                                            if ($child.choiceSettingValue) {
                                                $Value = $child.choiceSettingValue.Value
                                                $value = $value.replace("$($child.settingDefinitionId)_", "")


                                                $results.Add([pscustomobject]@{
                                                        Type         = "Settings Catalog"
                                                        Platform     = $Platform
                                                        PolicyName   = $p.name
                                                        PolicyId     = $p.id
                                                        SettingFound = $child.settingDefinitionId
                                                        SettingValue = $value
                                                    })
                                            }
                                            else {
                                                $results.Add([pscustomobject]@{
                                                        Type         = "Settings Catalog"
                                                        Platform     = $Platform
                                                        PolicyName   = $p.name
                                                        PolicyId     = $p.id
                                                        SettingFound = $child.settingDefinitionId
                                                        SettingValue = $child.simpleSettingValue.value
                                                    })
                                            }


                                        }
                                    }
                                }
                                else {


                                    if ($s.settingInstance.settingDefinitionId -like "*$needle*") {
                                        $value = ""


                                        if (![string]::IsNullOrEmpty($s.settingInstance.choiceSettingValue.value)) {
                                            $value = "$($s.settingInstance.choiceSettingValue.value)"
                                            $value = $value.Replace("$($s.settingInstance.settingDefinitionId)_", "")
                                            
                                        }
                                        elseif ($s.settingInstance.simpleSettingValue) {
                                            $value = $s.settingInstance.simpleSettingValue.Value
                                        }
                                        elseif ($s.settingInstance.simpleSettingCollectionValue) {
                                            foreach ($setVal in $s.settingInstance.simpleSettingCollectionValue) {
                                                $value += "$($setVal.value) | "
                                            }
                                        }
                                        elseif ($s.settingInstance.choiceSettingCollectionValue) {
                                            # Parse out Collection Values
                                            $collectionValues = ""
                                            foreach ($choiceVal in $s.settingInstance.choiceSettingCollectionValue) {
                                                $parsedVal = "$($choiceVal.value)".Replace("$($s.settingInstance.settingDefinitionId)_", "")
                                                $collectionValues += "$($parsedVal) "    
                                            }


                                            $value = $collectionValues
                                        }




                                        if ($value -ne $null) {
                                            $results.Add([pscustomobject]@{
                                                    Type         = "Settings Catalog"
                                                    Platform     = $Platform
                                                    PolicyName   = $p.name
                                                    PolicyId     = $p.id
                                                    SettingFound = $s.settingInstance.settingDefinitionId
                                                    SettingValue = $value
                                                })
                                        }
                                    }
                                }
                            }
                        }
                    }


                    # Search legacy as well
                    $Status.Invoke("Searching Device Configurations Templates...")


                    $legacyPols = $null
                    if ($LegacyPolicies.Count -gt 0 -and -not $RefreshPolicies) {
                        $legacyPols = $LegacyPolicies
                    }
                    else {
                        $legacyPols = Get-LegacyConfigPolicies -GraphSession $GraphSession
                        $LegacyPolicies = $legacyPols;
                    }


                    $idx = 0
                    foreach ($lp in $legacyPols) {
                        $idx++
                        if (($idx % 10) -eq 0) { $Status.Invoke("Device Config Templates... $idx/$($legacyPols.Count)") }


                        # For Legacy Policies it returns the policies and all settings using the main query, we don't need to query for settings
                        foreach ($key in $lp.Keys) {
                            if ($lp.$key -notin ($null, "notConfigured", "none") -and $key -notin $Ignores -and $key -like "*$needle*") {


                                $PsObj = Get-ResultObject -Type "Configuration Template" -PolicyObject $lp -Key $key -Needle $needle


                                foreach ($rslt in $PsObj) {
                                    $results.Add($rslt)
                                }
                            }
                        }
                    } # End of searching legacy policies


                } # End of Device Config


            
                if ($DoCompliance) {
                    $Status.Invoke("Searching Compliance Policies...")
                    $compPols = $null
                
                    if ($CompliancePolicies.Count -gt 0 -and -not $RefreshPolicies) {
                        $compPols = $CompliancePolicies
                    }
                    else {
                        $compPols = Get-CompliancePolicies -GraphSession $GraphSession
                        $CompliancePolicies = $compPols
                    }


                    $idx = 0
                    foreach ($cp in $compPols) {
                        $idx++
                        if (($idx % 10) -eq 0) { $Status.Invoke("Compliance Policies... $idx/$($compPols.Count)") }


                        $Platform = Get-PolicyPlatformType -Policy $cp


                        # For Compliance Policies it returns the policies and all settings using the main query, we don't need to query for settings
                        foreach ($key in $cp.Keys) {


                            if ($cp.$key -notin ($null, "notConfigured", "none") -and $key -notin $Ignores -and $Key -like "*$needle*") {
                                $val = $cp.$Key
                                if ($val -is [hashtable]) {
                                    foreach ($valKey in $val.Keys) {
                                        $results.Add([pscustomobject]@{
                                                Type         = "Compliance"
                                                Platform     = $Platform
                                                PolicyName   = $cp.displayName
                                                PolicyId     = $cp.id
                                                SettingFound = $valkey
                                                SettingValue = $val.$Valkey
                                            })
                                    }
                                }
                                elseif ($val -is [array]) {


                                    foreach ($object in $val) {
                                        if ($object -is [hashtable]) {
                                            $hashValue = ""
                                            foreach ($objKey in $object.keys) {
                                                if (-not [string]::IsNullOrEmpty($object.$objkey)) {
                                                    $hashValue += "$($objKey): $($object.$objkey) | "
                                                }
                                            }


                                            $results.Add([pscustomobject]@{
                                                    Type         = "Compliance"
                                                    Platform     = $Platform
                                                    PolicyName   = $cp.displayName
                                                    PolicyId     = $cp.id
                                                    SettingFound = $key
                                                    SettingValue = $hashValue
                                                })
                                                    
                                        }
                                        else {
                                            $results.Add([pscustomobject]@{
                                                    Type         = "Compliance"
                                                    Platform     = $Platform
                                                    PolicyName   = $cp.displayName
                                                    PolicyId     = $cp.id
                                                    SettingFound = $key
                                                    SettingValue = "$($object)"
                                                })
                                        }


                                    }
                                    
                                }
                                else {
                                    $results.Add([pscustomobject]@{
                                            Type         = "Compliance"
                                            Platform     = $Platform
                                            PolicyName   = $cp.displayName
                                            PolicyId     = $cp.id
                                            SettingFound = $key
                                            SettingValue = $cp.$key
                                        })
                                }
                            }
                        }
                    } # End of searching Compliance policies
                } # End of Compliance


                if ($DoADMX) {
                    $Status.Invoke("Searching ADMX...")


                    $gpPols = $null
                    if ($AdmxPolicies.Count -gt 0 -and !$RefreshPolicies) {
                        $gpPols = $AdmxPolicies
                    }
                    elseif ($AdmxPolicies.Count -eq 0 -or $RefreshPolicies) {
                        $gpPols = Get-GroupPolicyConfigurations -GraphSession $GraphSession
                        $AdmxPolicies = $gpPols;
                    }


                    $idx = 0
                    foreach ($gp in $gpPols) {
                        $idx++
                        $gpID = $gp.id


                        if (($idx % 10) -eq 0) { $Status.Invoke("Searching... $idx/$($gpPols.Count)") }


                        $defVals = $null
                        if ($AdmxSettings.Contains("$gpid") -and !$RefreshPolicies) {
                            $defVals = $AdmxSettings["$gpID"]
                        }
                        else {
                            $defVals = Get-GroupPolicyDefinitionValues -PolicyId $gpId -GraphSession $GraphSession
                            $AdmxSettings["$gpID"] = $defVals
                        }


                        foreach ($dv in $defVals) {
                            if ($dv.Name -like "*$needle*") {


                                $Platform = Get-PolicyPlatformType -Policy $dv


                                $results.Add([pscustomobject]@{
                                        Type         = "ADMX Template"
                                        Platform     = $Platform
                                        PolicyName   = $gp.displayName
                                        PolicyId     = $gpId
                                        SettingFound = $dv.Name
                                        SettingValue = $dv.Value
                                    })
                            }
                        }
                    }
                } # End of ADMX


                # Return results to caller
                if (-not $RefreshPolicies) {
                    $Status.Invoke("Search Complete")
                }
                else {
                    $Status.Invoke("Parsing Matches...")
                }


                $globalPolicies = @{
                    Config     = $ConfigPolicies
                    Legacy     = $LegacyPolicies
                    Admx       = $AdmxPolicies
                    ConfigSet  = $ConfigSettings
                    AdmxSet    = $AdmxSettings
                    Compliance = $CompliancePolicies
                }


                $resultArray = @($globalPolicies, $results)


                return $resultArray
            }
            catch {
                # Throw a terminating error so the caller sees it in EndInvoke()
                throw
            }
        }


    
        $script:SearchPS = $ps.AddScript($sb).
        AddArgument($needle).
        AddArgument($doConfig).
        AddArgument($doCompliance). 
        AddArgument($doADMX).
        AddArgument($statusReporter).
        AddArgument($script:GraphSession).
        AddArgument($script:ConfigurationPolicies).
        AddArgument($script:LegacyPolicies).     
        AddArgument($script:AdmxPolicies).
        AddArgument($script:ConfigurationPolicies_Settings).
        AddArgument($script:AdmxPolicies_Settings).
        AddArgument($script:RefreshPolicies).
        AddArgument($script:CompliancePolicies)


        $script:SearchAsync = $ps.BeginInvoke()


        # Poll completion on UI thread
        $script:SearchTimer = New-Object System.Windows.Threading.DispatcherTimer
        $script:SearchTimer.Interval = [TimeSpan]::FromMilliseconds(250)




        $script:SearchTimer.Add_Tick({
               
                if ($script:SearchAsync.IsCompleted -or
                    $script:SearchPS.InvocationStateInfo.State -in @('Completed', 'Failed', 'Stopped')) {


                    $script:SearchTimer.Stop()


                    try {
                        $data = $script:SearchPS.EndInvoke($script:SearchAsync)


                        $PolicyInfo = $data[0]
                        $results = $data[1]


                        if ($script:SearchPS.Streams.Error.Count -gt 0) {
                            $errs = ($script:SearchPS.Streams.Error | ForEach-Object { $_.ToString() }) -join "`r`n"
                            Show-UiMessage -Message $errs -Title "Runspace Errors"
                        }


                        $script:ConfigurationPolicies = $PolicyInfo.Config
                        $script:ConfigurationPolicies_Settings = $PolicyInfo.ConfigSet
                        $script:LegacyPolicies = $PolicyInfo.Legacy
                        $script:AdmxPolicies = $PolicyInfo.Admx
                        $script:AdmxPolicies_Settings = $PolicyInfo.AdmxSet
                        $script:EndpointProtection = $PolicyInfo.Endpoint
                        $script:EndpointProtection_Settings = $PolicyInfo.EndpointSet
                        $script:CompliancePolicies = $PolicyInfo.Compliance


                        foreach ($r in $results) {


                            if ([string]::IsNullOrEmpty($r.SettingValue)) {
                                $r.SettingValue = " "
                            }


                            Add-ResultRow -Type $r.Type -Platform $r.Platform -PolicyName $r.PolicyName -PolicyId $r.PolicyId -SettingFound $r.SettingFound -SettingValue $r.SettingValue
                        }


                        $TxtStatus.Text = "Done. Matches: $($script:Results.Count)"
                    }
                    catch {
                        $TxtStatus.Text = "Background Search Failed"
                        Show-UiMessage -Message $_.Exception.ToString() -Title "Search Error"
                        $script:SearchTimer.Stop()
                        $BtnSearch.IsEnabled = $true
                        $script:SearchPS.Dispose()
                    }
                    finally {
                        $BtnSearch.IsEnabled = $true
                        $script:SearchPS.Dispose()
                    }
                }
            })




        $script:SearchTimer.Start()




    })




# Show window
$null = $window.ShowDialog()