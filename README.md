<h1 align="center">
  <br>
  <a href="http://www.peachpie.io"><img src="http://www.peachpie.io/wp-content/uploads/2016/12/peachpie-round.png" width="400" alt="Peachpie" width="400"></a>
  <br>
  Peachpie Compiler
  <br>
</h1>

<h3 align="center">The open-source PHP compiler to .NET</h3>

> **Peachpie** is still in the alpha stages of development and not fully compatible with the PHP language yet. We recommend using the project with caution and testing it thoroughly before utilizing Peachpie in a production environment.

<p align="center">
<a href="http://www.nuget.org/profiles/peachpie"><img src="https://img.shields.io/nuget/v/Peachpie.App.svg?style=flat"></a>
<a href="https://gitter.im/iolevel/peachpie"><img src="https://badges.gitter.im/iolevel/peachpie.svg"></a>
<a href="https://github.com/peachpiecompiler/peachpie/blob/master/LICENSE.txt"><img src="https://img.shields.io/hexpm/l/plug.svg"></a>
<a href="http://www.peachpie.io"><img src="https://img.shields.io/badge/Web-peachpie.io-orange.svg"></a>
<a href="https://twitter.com/pchpcompiler"><img src="https://img.shields.io/badge/Twitter-%40pchpcompiler-blue.svg"></a>
<a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=BY2V98VY57K2E" target="_blank"><img src="https://img.shields.io/badge/$-donate-ff69b4.svg?maxAge=2592000&amp;style=flat"></a>
</p>

- [Getting Started](https://github.com/peachpiecompiler/peachpie/wiki/Getting-Started)  
- [Documentation](https://github.com/peachpiecompiler/peachpie/wiki)

[<img align="right" src="https://dotnetfoundation.org/Themes/DotNetFoundation.Theme/Images/logo-small.png" width="100" />](https://www.dotnetfoundation.org/)
We are now a member of the [.NET Foundation](https://www.dotnetfoundation.org/about)!

## Continuous Integration

| Service  | Platform  | Build Status  |
|---|---|---|
| Travis CI | Ubuntu  | [![Build status](https://api.travis-ci.org/peachpiecompiler/peachpie.svg?branch=master)](https://travis-ci.org/peachpiecompiler/peachpie)  |
| MyGet Build Services  | Windows  | [![peachpie MyGet Build Status](https://www.myget.org/BuildSource/Badge/peachpie?identifier=14586f8c-2600-412f-b9b0-39db8e930806)](https://www.myget.org/gallery/peachpie)    |
| Visual Studio Team Services | Windows | ![VSTS Build Status](https://iolevel.visualstudio.com/_apis/public/build/definitions/bd7dcca1-8515-44f8-81d0-bb2acc03d949/1/badge)|

## What is Peachpie?
Peachpie is a modern PHP compiler based on the Microsoft Roslyn compiler platform and drawing from our popular Phalanger project. It allows PHP to be executed within the .NET framework, thereby opening the door for PHP developers into the world of .NET – and vice versa.

## Status and Compatibility
You can find an up-to-date status of the project in our [Roadmap](https://github.com/peachpiecompiler/peachpie/wiki/Peachpie-Roadmap) section. Please note that the status is dynamic; Peachpie is a work in progress, which means that the list of finished and planned features frequently changes and will be updated on a regular basis.

The Wiki also includes a [Compatibility matrix](https://github.com/peachpiecompiler/peachpie/wiki/Compatibility) for a quick overview of which frameworks Peachpie is compatible with and which features are already supported. 

## Project goals
- **Increased performance**: Peachpie's extensive type analysis and the influence of Microsoft Roslyn should provide an improved performance of PHP applications and components. 

- **Security**: since programs run within the standardized and manageable .NET or .NET Core environment, the code is fully verifiable without any unsafe constructs. 

- **Cross-platform development**: the project compiles legacy PHP code into portable class libraries, enabling developers to build cross-platform apps and libraries for Microsoft platforms.  

- **Full .NET compatibility**: compiled programs run on the reimplemented Peachpie runtime, fully compatibly with the PHP runtime.

- **Both-way interoperability**: the project allows for hybrid applications, where parts are written in C# and others in PHP. The parts will be entirely compatible and can communicate seamlessly, all within the .NET framework.  


## How to use Peachpie
There are currently two ways of using Peachpie through `msbuild`: in your favorite shell or in Visual Studio Code/Visual Studio 2017. Keep in mind that Peachpie is still a work in progress and is therefore not intended to run full applications in a production setting.

### Visual Studio Code 
We have a custom [VSCode extension](https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vscode) to make working with Peachpie compiler as comfortable as possible. The extension automatically installs all required dependencies, enables the `Peachpie: Create project` command, syntax error underlining and Peachpie analytics:

<p align="center">
<a href="https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vscode" target="_blank"><img src="https://raw.githubusercontent.com/iolevel/peachpie-vscode/master/src/Peachpie.VSCode/images/tEDLQt.gif" 
alt="Peachpie Extension" border="10" /></a> 
</p>

To install the Peachpie extension, simply launch VS Code Quick Open (Ctrl+P), paste the following command, and press enter: `ext install peachpie-vscode`. Watch the intro video below to see how to work with the extension:

<p align="center">
<a href="https://youtu.be/hBiixbockK4" target="_blank"><img src="http://www.peachpie.io/wp-content/uploads/2017/04/yt-screen-1.png" 
alt="Peachpie Introduction" border="10" /></a>
</p>

### Command line building
Alternatively, you can also work with Peachpie on the command line. Please refer to our [short introduction video](https://www.youtube.com/watch?v=GVWVInYiYLY) to see how to run the compiler on the command line and to our [Getting Started](https://github.com/peachpiecompiler/peachpie/wiki/Getting-Started) section. 

## How to contribute?
We can use all the help we can get. You can contribute to our repository, spread the word about this project, or give us a small donation to help fund the development. If you believe you have valuable knowledge and experience to add to this project, please do not hesitate to contribute to our repo – your help is much appreciated. 

However, please read the [Contribution Guidelines](https://github.com/peachpiecompiler/peachpie/blob/master/CONTRIBUTING.md) first and ensure you are following them. Also, we kindly ask you to respect our [Code of Conduct](https://github.com/peachpiecompiler/peachpie/blob/master/CODE_OF_CONDUCT.md) when posting or interacting with other users. 

## Providing feedback
If you found a bug, the easiest way of providing feedback is to post it on [Gitter](https://gitter.im/iolevel/peachpie). We will enable the posting of issues on GitHub once the compiler will be in version 1.0.


## .NET Foundation
<a href="https://dotnetfoundation.org"><img src="https://dotnetfoundation.org/Themes/DotNetFoundation.Theme/Images/logo-small.png" width="150" alt=".NET Foundation"></a>
  <br>
This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).


## How to get in touch?
We kindly ask you to be patient with your queries; you can follow us on [Twitter](https://twitter.com/pchpcompiler) or on [Facebook](https://www.facebook.com/pchpcompiler/). You can contact us there regarding your questions or ask the community for support on [Gitter](https://gitter.im/iolevel/peachpie), but please understand that we do not provide support at this point.

For partnership inquiries or other questions, please contact us via email at info@iolevel.com.
