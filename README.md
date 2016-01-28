# Composable.Monolithic Documentation
<img align="right" style="height:90px;width:140px" src="images/monolith.jpg">

Composable.Monolithic is a framework for [Composability](https://en.wikipedia.org/wiki/Composability) in Software Development available for .NET.

Current version is 1.0, released in 2014.

[TOC]

## Installation

Git clone into your homedirectory via Powershell.

	cd ~
    cd ..
    git clone https://github.com/mlidbom/Composable.Monolithic

Once cloned to a directory, open the project and Build the solution (Ctrl + Shift + B).

> If this does not work you need to put the directory directly into your C: `cd C:\`, since windows does not allow strings longer than 260 Char (windows, please fix). See FAQ.

___
Thereafter you need to create a subdirectory to the Framework, called NuGetFeed. Here we will put the [pakets](https://fsprojects.github.io/Paket/) for NuGet.

	cd C:\
    mkdir NuGetFeed

Now we can run the package script `C:\Composable.Monolith\.buildpaket.ps1`

___
### Trying out the Framework

We have been so kind as to provide a Trial&Error solution within the Framework for you to try it out and see it as a whole. The solution is located in the folder:

	C:\Composable.Monolith\Samples\AccountManagement

This solution wants to have the Composable package but this is solved by just Building the solution (Ctrl + Shift + B).

Now it's ready for use! Enjoy!

## Show me the code already

```csharp
// how to use Composable..
```

So what about those [CQRS.ServiceBus](servicebus.md)

```csharp
using JetBrains.Annotations;
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary> Defines a message handler that should only listen for messages dispatched by <see cref="SynchronousBus"/>.</summary>
    public interface IHandleInProcessMessages<T> where T : IMessage
    {
        [UsedImplicitly]
        void Handle(T message);
    }
}
```

For more in-depth sample try the section below, or dive right into API documentation on the right.

## Samples and tutorials

Learn Windsor by example by completing step-by-step tutorials. See Windsor in action by exploring sample applications showcasing its capabilities:

* [Basic tutorial](basic-tutorial.md)

## FAQ

> Why am I getting the error of path too long?

Because Windows only allow filepaths to be 260 Characters long.

### Concepts

* [Composability](composability.md)
* [Services, Components and Dependencies](services-and-components.md)

### Frameworks used

* [Windsor](https://github.com/castleproject/Windsor) - Inversion of Control
* [Paket](https://fsprojects.github.io/Paket/) - NuGet dependency manager for .NET

## Resources

* [External Resources](external-resources.md) - screencasts, podcasts, etc
* [FAQ](faq.md)
* [Roadmap](roadmap.md)