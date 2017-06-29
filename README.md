# sensenet ECM SnAdmin

[![Join the chat at https://gitter.im/SenseNet/sn-admin](https://badges.gitter.im/SenseNet/sn-admin.svg)](https://gitter.im/SenseNet/sn-admin?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Upgrade and package executor tool for [sensenet ECM](https://github.com/SenseNet/sensenet). It can be used to execute packages (e.g. upgrade packages published for *Enterprise* customers, or custom packages assembled by anyone) that change the Content Repository or the web application folder. It also helps developers and operators automate their processes (e.g. a build or deployment workflow).

SnAdmin can be considered as a framework built on a script language that describes a set of steps to be executed one after the other. sensenet ECM offers many [built-in steps](https://github.com/SenseNet/sensenet/blob/master/docs/snadmin-builtin-steps.md) and it is also possible for third party developers to [create custom steps](https://github.com/SenseNet/sensenet/blob/master/docs/snadmin-create-custom-step.md) to include in custom packages.

The binaries of this tool are distributed via a _NuGet_ package, but you do not have to install it manually: it is installed as a dependency of the main sensenet ECM release.

## Usage
### Executing a package
`SnAdmin <packagename>`

### Help
Display usage and available package names and descriptions:

`SnAdmin -help`

Get package description and parameter list:

`SnAdmin <packagename> -help`

For details, examples and extensibility options please visit the following article:

- [SnAdmin details](/docs/SnAdmin.md)
