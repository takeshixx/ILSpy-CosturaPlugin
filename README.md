# Costura Plugin for ILSpy

[Costura](https://github.com/Fody/Costura) is an add-in for [Fody](https://github.com/Fody/Fody/) which allows to embedd references in binaries as resources. This means that e.g. all the DLL files that are required by a binary are added as compressed resources in the new binary. They are loaded with some compiled-in trampolin code by Costura. This plugin adds decompression and loading support for such embedded references to ILSpy to make decompilation of binaries compiled with Costura easier.

## Installation

A pre-built DLL is available in the [release section](https://github.com/takeshixx/ILSpy-CosturaPlugin/releases). Just copy it to the same directory where the `ILSpy.exe` resides and run `ILSpy.exe`.

## Building

Clone the ILSpy repository:

```
git clone https://github.com/icsharpcode/ILSpy.git
```

Copy the `CosturaPlugin` folder to the ILSpy directory. Then open `ILSpy.sln` in Visual Studio and add `CosturaPlugin/CosturaPlugin.csproj` as existing project. Then just build the `CosturaPlugin` project.
