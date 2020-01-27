@{

# Script module or binary module file associated with this manifest.
RootModule = 'Piraeus.Module.Core.dll'

# Version number of this module.
ModuleVersion = '1.2.1'

# Supported PSEditions
# CompatiblePSEditions = @()

# ID used to uniquely identify this module
GUID = '10c6ace8-4a8c-4e59-8c04-b599523a4f54'

# Author of this module
Author = 'malong'

# Company or vendor of this module
CompanyName = 'SkunkLab'

# Copyright statement for this module
Copyright = '(c) SkunkLab. All rights reserved.'

# Description of the functionality provided by this module
Description = 'Piraeus Powershell Management Scripts'

# Minimum version of the PowerShell engine required by this module
PowerShellVersion = '6.2.3'

# Name of the PowerShell host required by this module
# PowerShellHostName = ''

# Minimum version of the PowerShell host required by this module
# PowerShellHostVersion = ''

# Minimum version of Microsoft .NET Framework required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
# DotNetFrameworkVersion = ''

# Minimum version of the common language runtime (CLR) required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
# CLRVersion = ''

# Processor architecture (None, X86, Amd64) required by this module
# ProcessorArchitecture = ''

# Modules that must be imported into the global environment prior to importing this module
#RequiredModules = @()

# Assemblies that must be loaded prior to importing this module
RequiredAssemblies = @('Capl.dll', 'Piraeus.Core.dll')

# Script files (.ps1) that are run in the caller's environment prior to importing this module.
# ScriptsToProcess = @()

# Type files (.ps1xml) to be loaded when importing this module
# TypesToProcess = @()

# Format files (.ps1xml) to be loaded when importing this module
# FormatsToProcess = @()

# Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
# NestedModules = @()

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = @()

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = @('Get-PiraeusManagementToken','Add-CaplPolicy','Get-CaplPolicy','Remove-CaplPolicy','New-CaplPolicy','New-CaplRule','New-CaplOperation','New-CaplMatch','New-CaplLogicalAnd','New-CaplLogicalOr','New-CaplTransform','New-CaplLiteralClaim','Add-PiraeusEventMetadata','Get-PiraeusEventMetadata','Get-PiraeusSigmaAlgebra','Remove-PiraeusEvent','Add-PiraeusSubscriptionMetadata','Get-PiraeusSubscriptionMetadata','Remove-PiraeusSubscription','Add-PiraeusBlobStorageSubscription','Add-PiraeusCosmosDbSubscription','Add-PiraeusEventGridSubscription','Add-PiraeusEventHubSubscription','Add-PiraeusIotHubCommandSubscription','Add-PiraeusIotHubDeviceSubscription','Add-PiraeusIotHubDirectMethodSubscription','Add-PiraeusQueueStorageSubscription','Add-PiraeusServiceBusSubscription','Add-PiraeusWebServiceSubscription','Add-PiraeusRedisCacheSubscription','Get-PiraeusSubscriptionList','Get-PiraeusSubscriberSubscriptions','Get-PiraeusEventMetrics','Get-PiraeusSubscriptionMetrics','Add-PiraeusServiceIdentityClaims','Add-PiraeusServiceIdentityCertificate','Add-PiraeusPskSecret','Get-PiraeusPskSecret','Get-PiraeusPskKeys','Remove-PiraeusPskSecret')

# Variables to export from this module
VariablesToExport = '*'

# Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
AliasesToExport = @()

# DSC resources to export from this module
# DscResourcesToExport = @()

# List of all modules packaged with this module
# ModuleList = @()

# List of all files packaged with this module
# FileList = @()

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        Tags = @('Piraeus')

        # A URL to the license for this module.
        LicenseUri = 'https://github.com/skunklab/piraeus/blob/master/LICENSE'

        # A URL to the main website for this project.
        ProjectUri = 'https://github.com/skunklab/piraeus'

        # A URL to an icon representing this module.
        IconUri = 'https://raw.githubusercontent.com/skunklab/piraeus/master/src/SkunkLab.Channels/skunklab.png'

        # ReleaseNotes of this module
        ReleaseNotes = 'Module for managing Piraeus.'

    } # End of PSData hashtable

} # End of PrivateData hashtable

# HelpInfo URI of this module
# HelpInfoURI = ''

# Default prefix for commands exported from this module. Override the default prefix using Import-Module -Prefix.
# DefaultCommandPrefix = ''

}