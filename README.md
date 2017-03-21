# Sense/Net SnAdmin

[![Join the chat at https://gitter.im/SenseNet/sn-admin](https://badges.gitter.im/SenseNet/sn-admin.svg)](https://gitter.im/SenseNet/sn-admin?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Upgrade and package executor tool for [Sense/Net ECM](https://github.com/SenseNet/sensenet). It is used to execute packages on the Content Repository (e.g. upgrade packages published by Sense/Net for *Enterprise* customers, or custom packages assembled by anyone). It also helps developers and operators automate their processes (e.g. a build or deployment workflow).

[SnAdmin](http://wiki.sensenet.com/SnAdmin) can be considered as a framework built on a script language that describes a set of steps to be executed one after the other. Sense/Net ECM offers many [built-in steps](http://wiki.sensenet.com/Built-in_steps) and it is also possible for third party developers to [create custom steps](http://wiki.sensenet.com/How_to_create_a_custom_step_for_SnAdmin) to include in custom packages.

The binaries of this tool are distributed via a _NuGet_ package, but you do not have to install it manually: it is installed as a dependency of the main Sense/Net ECM release.

## Usage
### Executing a package
`SnAdmin <packagename>`

### Help
Display usage and available package names and descriptions:

`SnAdmin -help`

Get package description and parameter list:

`SnAdmin <packagename> -help`

For details, examples and extensibility options please visit the following article:

- http://wiki.sensenet.com/SnAdmin
