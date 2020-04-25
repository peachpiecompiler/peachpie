<h1 align="center">
  <br>
  <img src="https://www.peachpie.io/wp-content/uploads/2017/10/full-orange-400x100.png" width="400" alt="PeachPie"/>
  <br>
  PeachPie Compiler
  <br>
</h1>

<h3 align="center">The open-source PHP compiler to .NET</h3>

> If you run into any inconsistencies, bugs or incompatibilities, kindly let us know and we'll do our best to address them. Take a look at our [Roadmap](https://docs.peachpie.io/roadmap/) to see which features and extensions we still have to implement.

<p align="center">
<a href="https://www.nuget.org/profiles/peachpie"><img src="https://img.shields.io/nuget/v/Peachpie.App.svg?style=flat"></a>
<a href="https://docs.peachpie.io"><img src="https://img.shields.io/badge/docs-peachpie.io-green.svg"></a>  
<a href="https://gitter.im/iolevel/peachpie"><img src="https://badges.gitter.im/iolevel/peachpie.svg"></a>
<a href="https://www.peachpie.io"><img src="https://img.shields.io/badge/Web-peachpie.io-orange.svg"></a>
<a href="https://twitter.com/pchpcompiler"><img src="https://img.shields.io/badge/Twitter-%40pchpcompiler-blue.svg"></a>
<a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=BY2V98VY57K2E" target="_blank"><img src="https://img.shields.io/badge/$-donate-ff69b4.svg?maxAge=2592000&amp;style=flat"></a>
</p>

[<img align="right" src="https://github.com/peachpiecompiler/peachpie/blob/master/docs/logos/dotnet-foundation-logo.png" width="100" />](https://www.dotnetfoundation.org/)
We are now a member of the [.NET Foundation](https://www.dotnetfoundation.org/about)!

## Continuous Integration

| Service  | Platform  | Build Status  |
|---|---|---|
| Travis CI | Ubuntu 18  | [![Travis Build status](https://api.travis-ci.org/peachpiecompiler/peachpie.svg?branch=master)](https://travis-ci.org/peachpiecompiler/peachpie)  |
| AppVeyor  | Visual Studio 2019  | [![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/1ym8cd838l0od9oa?svg=true)](https://ci.appveyor.com/project/dotnetfoundation/peachpie) |
| Azure DevOps | Windows | ![VSTS Build Status](https://iolevel.visualstudio.com/_apis/public/build/definitions/bd7dcca1-8515-44f8-81d0-bb2acc03d949/1/badge)|
| GitHub Actions | Ubuntu 18 | ![.NET Core](https://github.com/peachpiecompiler/peachpie/workflows/.NET%20Core/badge.svg) |

## What is PeachPie?
PeachPie is a modern PHP compiler based on the Microsoft Roslyn compiler platform and drawing from our popular Phalanger project. It allows PHP to be executed within the .NET framework, thereby opening the door for PHP developers into the world of .NET – and vice versa.

## Status and Compatibility
You can find an up-to-date status of the project in our [Roadmap](https://docs.peachpie.io/roadmap/) section. Please note that the status is dynamic; PeachPie is a work in progress, which means that the list of finished and planned features frequently changes and will be updated on a regular basis. To see the current status of compatibility with the PHP language, please refer to our [Compatibility overview](https://docs.peachpie.io/php/Compatibility/). 

## Project goals
- **Increased performance**: PeachPie's extensive type analysis and the influence of Microsoft Roslyn should provide an improved performance of PHP applications and components. 

- **Security**: since programs run within the standardized and manageable .NET or .NET Core environment, the code is fully verifiable without any unsafe constructs. In addition, PHP applications can be distributed source-lessly for added security benefits. 

- **Cross-platform development**: the project compiles legacy PHP code into portable class libraries, enabling developers to build cross-platform apps and libraries for Microsoft platforms.  

- **Full .NET compatibility**: compiled programs run on the reimplemented PeachPie runtime, fully compatibly with the PHP runtime.

- **Both-way interoperability**: the project allows for hybrid applications, where parts are written in C# and others in PHP. The parts will be entirely compatible and can communicate seamlessly, all within the .NET framework.  


## How to use PeachPie
There are currently two ways of using PeachPie via `dotnet`: in your favorite shell or comfortably in Visual Studio 2017/Visual Studio Code using our official extensions. 

### Visual Studio
Download our official [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vs), which makes working with PeachPie compiler as convenient as possible. The extension allows you to easily create a new project using our templates, build & debug, profile your PHP code using the VS diagnostic tools and deploy your project to Azure:

<p align="center">
<a href="https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vs" target="_blank"><img src="https://github.com/iolevel/peachpie-vs/blob/master/imgs/peachpie-new-project.gif?raw=true" 
alt="Peachpie Extension" border="10" /></a> 
</p>

### Visual Studio Code 
Grab our [VSCode extension](https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vscode) to quickstart your development with a more lightweight editor. The extension automatically installs all required dependencies, enables the `PeachPie: Create project` command, syntax error underlining and PeachPie analytics:

<p align="center">
<a href="https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vscode" target="_blank"><img src="https://raw.githubusercontent.com/iolevel/peachpie-vscode/master/src/Peachpie.VSCode/images/tEDLQt.gif" 
alt="Peachpie Extension" border="10" /></a> 
</p>

### Command line building
Alternatively, you can also work with PeachPie on the command line. Please refer to our [short introduction video](https://www.youtube.com/watch?v=GVWVInYiYLY) to see how to run the compiler on the command line and to the [Getting Started](https://docs.peachpie.io/get-started/) section in our documentation. 

## How to contribute?
We can use all the help we can get. You can contribute to our repository, spread the word about this project, or give us a small donation to help fund the development. If you believe you have valuable knowledge and experience to add to this project, please do not hesitate to contribute to our repo – your help is much appreciated. 

However, please read the [Contribution Guidelines](https://github.com/peachpiecompiler/peachpie/blob/master/CONTRIBUTING.md) first and ensure you are following them. Also, we kindly ask you to respect our [Code of Conduct](https://github.com/peachpiecompiler/peachpie/blob/master/CODE_OF_CONDUCT.md) when posting or interacting with other users. 

You can also contribute by donating a dollar or two to the development of PeachPie:
<p align="center"> <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=BY2V98VY57K2E" target="_blank"><img src="https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif"/></a> </p>  

## Providing feedback
If you found a bug, have a question or if you have an improvement suggestion, the easiest way of providing feedback is to post it on [Gitter](https://gitter.im/iolevel/peachpie) or submit an issue here on GitHub. We try to respond as quickly as possible.


## .NET Foundation
<a href="https://dotnetfoundation.org"><img src="https://github.com/peachpiecompiler/peachpie/blob/master/docs/logos/dotnet-foundation-logo.png" width="150" alt=".NET Foundation"></a>
  <br>
This project is supported by the [.NET Foundation](https://www.dotnetfoundation.org/).


## How to get in touch?
If you have a problem or question, the easiest way is to submit an issue here. You can also follow us on [Twitter](https://twitter.com/pchpcompiler) or [Facebook](https://www.facebook.com/pchpcompiler/) and contact us there regarding your questions or ask the community for support on [Gitter](https://gitter.im/iolevel/peachpie), but please understand that we do not provide email support.

For partnership inquiries, commercial support or other questions, please contact us via email at info@iolevel.com.
