<h1 align="center">
  <br>
  <img src="https://www.peachpie.io/wp-content/uploads/2017/10/full-orange-400x100.png" width="400" alt="PeachPie"/>
  <br>
  PeachPie Compiler
  <br>
</h1>

<h3 align="center">The open-source PHP compiler to .NET</h3>

<p align="center">
<a href="https://discord.com/invite/SAs8VP2XqP"><img src="https://img.shields.io/badge/chat-discord-purple.svg"></a>
<a href="https://docs.peachpie.io"><img src="https://img.shields.io/badge/docs-peachpie.io-green.svg"></a>  
<a href="https://www.peachpie.io"><img src="https://img.shields.io/badge/www-peachpie.io-orange.svg"></a>
<a href="https://twitter.com/pchpcompiler"><img src="https://img.shields.io/badge/x-%40pchpcompiler-blue.svg"></a>
<a href="https://www.patreon.com/pchpcompiler" target="_blank"><img src="https://img.shields.io/badge/sponsor-become_a_patron-ff69b4.svg?maxAge=2592000&amp;style=flat"></a>
</p>

[<img align="right" src="https://github.com/peachpiecompiler/peachpie/blob/master/docs/logos/dotnet-foundation-logo.png" width="100" />](https://www.dotnetfoundation.org/)
PeachPie is a member project of the [.NET Foundation](https://www.dotnetfoundation.org/about).

## Continuous Integration

| Service  | Platform  | Build Status  |
|---|---|---|
| AppVeyor  | Visual Studio 2019  | [![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/1ym8cd838l0od9oa?svg=true)](https://ci.appveyor.com/project/dotnetfoundation/peachpie) |
| Azure DevOps | Windows | ![VSTS Build Status](https://iolevel.visualstudio.com/_apis/public/build/definitions/bd7dcca1-8515-44f8-81d0-bb2acc03d949/1/badge)|
| GitHub Actions | Ubuntu 18 | ![.NET Core](https://github.com/peachpiecompiler/peachpie/workflows/.NET%20Core/badge.svg) |

## What is PeachPie?

PeachPie is a modern PHP compiler based on the Microsoft Roslyn compiler platform. It allows PHP to be compiled and executed under the .NET runtime, thereby opening the door for PHP developers into the world of .NET – and vice versa.

## Project goals

- **Both-way interoperability**: the project allows for hybrid applications, where parts are written in C# and others in PHP. The parts will be entirely compatible and can communicate seamlessly, all within the .NET framework.  

- **Full .NET compatibility**: compiled programs run on the reimplemented PeachPie runtime, fully compatibly with the PHP runtime.

- **Security**: since programs run within the standardized and manageable .NET or .NET Core environment, the code is fully verifiable without any unsafe constructs. In addition, PHP applications can be distributed source-lessly for added security benefits. 

- **Cross-platform development**: the project compiles legacy PHP code into portable class libraries, enabling developers to build cross-platform apps and libraries for Microsoft platforms.  

- **Increased performance**: PeachPie's extensive type analysis and the influence of Microsoft Roslyn should provide an improved performance of PHP applications and components. 

## How to use PeachPie

There are currently two ways of using PeachPie via `dotnet`: in your favorite shell or comfortably in Visual Studio using our official extension. 

### IDEs

You can comfortably work with PeachPie in your favorite IDEs. Download our official [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vs), which makes working with PeachPie compiler as convenient as possible. The extension allows you to easily create a new project using our templates, build & debug, profile your PHP code using the VS diagnostic tools and deploy your project to Azure:

<p align="center">
<a href="https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vs" target="_blank"><img src="https://github.com/iolevel/peachpie-vs/blob/master/imgs/peachpie-new-project.gif?raw=true" 
alt="Peachpie Extension" border="10" /></a> 
</p>

You can also use VS Code or Rider to build and debug PeachPie projects. 

### Command line building

Alternatively, you can also work with PeachPie on the command line. Please refer to our [short introduction video](https://www.youtube.com/watch?v=GVWVInYiYLY) to see how to run the compiler on the command line and to the [Getting Started](https://docs.peachpie.io/get-started/) section in our documentation.

### Getting Started with Command Line

For beginners, here is a simple step-by-step guide to run PeachPie projects using the command line:

1. **Install .NET SDK** (7.0 or higher):
  - [Download .NET SDK](https://dotnet.microsoft.com/en-us/download)

2. **Clone the PeachPie repository** (or your own project):
```bash
git clone https://github.com/your-user/peachpie.git
cd peachpie

3. **Run a PHP example script**:
Create a simple file called `HelloWord.php` in `examples/HelloWorld/`:
```php
<?php
echo "Hello, PeachPie!";

4. **Compile the Project**:
```bash
dotnet build
dotnet run --project examples/HelloWorld/HelloWord.csproj

### Nightly builds

The most convenient way of using PeachPie is to consume NuGet packages. We provide nightly builds and release builds to our subscribers on Patreon. [Become a Patron](https://www.patreon.com/pchpcompiler) to get access and get listed as our sponsor!

## Status and Compatibility

You can find an up-to-date status of the project in our [Roadmap](https://docs.peachpie.io/roadmap/) section. Please note that the status is dynamic; PeachPie is a work in progress, which means that the list of finished and planned features frequently changes and will be updated on a regular basis. To see the current status of compatibility with the PHP language, please refer to our [Compatibility overview](https://docs.peachpie.io/php/Compatibility/). 

## How to contribute?

PeachPie is an open source project we maintain in our spare time. We can use all the help we can get. If you believe you have valuable knowledge and expertise to add to this project, please do not hesitate to contribute to our repo via pull requests or issues – your help is much appreciated.

However, please read the [Contribution Guidelines](https://github.com/peachpiecompiler/peachpie/blob/master/CONTRIBUTING.md) first and ensure you are following them. Also, we kindly ask you to respect our [Code of Conduct](https://github.com/peachpiecompiler/peachpie/blob/master/CODE_OF_CONDUCT.md) when posting or interacting with other users. 

You can also support the project on [Patreon](https://www.patreon.com/pchpcompiler), which gives you access to all kinds of perks!

## Providing feedback

If you found a bug, have a question or if you have an improvement suggestion, the easiest way of providing feedback is to ask on [Discord](https://discord.gg/SAs8VP2XqP) or submit an issue here on GitHub. We try to respond as quickly as possible.


## .NET Foundation

<a href="https://dotnetfoundation.org"><img src="https://github.com/peachpiecompiler/peachpie/blob/master/docs/logos/dotnet-foundation-logo.png" width="150" alt=".NET Foundation"></a>
  <br>
This project is supported by the [.NET Foundation](https://www.dotnetfoundation.org/).
