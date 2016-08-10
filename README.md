# Sitecore.Support.74811.60652.457194.841
It has fixes for AD module bugs #74811, #60652 and #841 and an issue mentioned in ticket #457194 (ECM dispatch-related).


## Main

The following issues are fixed:

- 74811 - bug, disabling notification provider for profile provider disables it for the role provider
- 60652 - bug, unexpected roles number if a group is created on the AD side when the role cache is empty
- 457194 - customer ticket with an issue while sending ECM messages to AD users with a load balanced AD domain controller
- 841 - bug, cyclic relationship of security groups leads to StackOverflowException

## Deployment

To apply the patch perform the following steps:

1. Place the `Sitecore.Support.74811.60652.457194.841` assembly into the `\bin` directory.
2. Set `LightLDAP.Support.SitecoreADRoleProvider` instead of the default AD role provider `LightLDAP.SitecoreADRoleProvider`.
3. Set `LightLDAP.Support.SitecoreADProfileProvider` instead of the default AD profile provider `LightLDAP.SitecoreADProfileProvider`.

## Content 

Sitecore Patch includes the following files:

1. `\bin\Sitecore.Support.74811.60652.457194.841.dll`

## License

This patch is licensed under the [Sitecore Corporation A/S License](LICENSE).

## Download

Downloads are available via [GitHub Releases](https://github.com/SitecoreSupport/Sitecore.Support.74811.60652.457194.841/releases).
