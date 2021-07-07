# Code Analyzers and Source Generation

---

## Introduction

In this lecture I want to talk about two of the things we can do with Roslyn compiler api - "Code Analysis" and "Source Generation". All of the code shown here is published on my github (all links are at the end).

Some parts of the things I will show here can be done automatically with recent IDE's (there are predefined templates for analysis projects for example), but I wanted to dive a bit deeper to provide a better understanding of how the things work under the hood.

There are also some things I will skip (for example I will not elaborate on "Analyzer Release Tracking"), because those things are a bit out of the scope of what I want to explain.

Finally, in some examples here, I will omit proper error handling and/or complete logical solutions, edge case handling, etc. for the sake of simplicity and readability.

## What is Roslyn?

Roslyn is an open source compiler platform for .NET languages, that was released as a stable release somewhere around 2015. It's supporting all the .NET languages, but all new features come to C# first.

The idea behind Roslyn project was to provide programmers a way to interface with the compiler via an open API. 

Previously, the compiler was acting as a black box, that doing it's magic in the background, and when it's finished, you would get a compiled assembly or an executable. But with Roslyn we can interact with the compilation process.

## What is "Code Analyzer"?

When we code, the IDE is constantly interacts with the compiler. It has multiple "events" that trigger the compilation process. For example, when we are opening a file, the compiler is triggered for this class only. We need this to be able to see Intellisense tips, errors, warnings and code completion suggestions.

With Roslyn "Code Analysis" we can hook into this process and provide our own tips, errors, warnings, etc. by adding an assembly as a "Code Analyzer Reference" to our project via direct referencing or via nuget package.

So, how does it works? As I said, the IDE has many events that trigger the compiler and we can hook into each one of them.

When the event is triggered, the compiler doing it's job and returns some complex object to the IDE, which contains all the information IDE needs to display to the programmer. When we hook into an event like this, we can modify or add any information to this object, by reading and analyzing the code that is being compiled.

In other words, we can display errors or warnings to the IDE user based on the code he's written.

Great thing about this - we don't need some complex RegEx patterns or "IndexOf"s - all the code in the compiled assembly is parsed and provided to us as a C# API (or [VB.NET](http://vb.NET) if you into this kind of fetishes).

## "Code Analyzer" use cases

Most common use case for "Code Analyzer" is probably coding standards enforcement.

It can be something small, like: "Every method should be written in PascalCase". Or something bigger like: "We're not using linq in our project".

In both cases, we can write an analyzer that will search for the defined occurrences and will add a compilation error (or warning) with syntax highlighting (the red/orange underline) when we find it.

"Code Analyzer" can also compliment our "Source Generator". For example, if our generated code require some conditions to be met, we can notify the programmer about it.

This is only a small amount of use cases for code analysis, and I bet you will find many more, if you'll start using it.

## What is a "Source Generator"?

In .NET we had source generation for a long time now. Almost from the start. We had an ability to hook into the compilation process even before the Roslyn compiler came around, but comparing to the current implementation all of this was half baked. 

When Roslyn came around it provided us to write source generation outside of the IDE with pretty complex API, but as time passed, now it is integrated directly into our IDE and have pretty nice API to go with it.

"Source Generators" are built on top of the "Code Analyzers" and use the same API for code parsing, but while the "Code Analyzers" allow us to analyze code and suggest (or force) a different approach, "Source Generator" can generate custom code in the middle of the compilation.

The code we generate can be based on something the programmer already wrote, in this case we analyze the code and act accordingly, or we can just add some code regardless of what is already exists in the project (which will be pretty dumb, because we can use assemblies for this).

It works in the following way:

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled.png)

Source: [https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/)

After the compilation is finished, the "Source Generation" assembly is triggered. This assembly is analyzing the code, adding some code to the assembly and returning to the compiler, which is resuming the compilation.

## Source Generation in Unity

At the time of writing Unity doesn't support Source Generation and it is declared as a stretch goal for Unity 2021.3.

## "Source Generation" use cases

As a game developer, for me personally, the most common use case for "Source Generation" is a replacement to "Reflection". Sometimes in our day to day tasks we can't avoid Reflection, but using it is slow and generated a lot of garbage. Wouldn't it be great if we can achieve the same results at compile time, without runtime garbage generation?

I'm not saying that "Source Generators" can completely remove the need to use "Reflection", but in most cases it can. I will elaborate on this a bit later.

But we are not limited with this use case only. We can generate extension classes, we can create custom overloads for methods to avoid boxing/unboxing and we can add "code aware" classes that would be added to an assembly only if some criteria is met.

## Getting started

The "Source Generator" itself has to be written as a `netstarndard2.0` assembly, but in order to use it, our project have to use C# 9 along with Roslyn compiler 3.8.0 (or later), latest build of Visual Studio 2019 or latest stable release of Rider.

In the following sections I will explain how to create a source generator in Rider, but it can be done with Visual Studio in similar fashion.

## Creating empty Source generation project

Let's create an empty solution and add a class library project to it with target framework `netstandard2.0`. This will be our source generation and code analysis project.

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%201.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%201.png)

1

Now we need to add couple of nuget packages to the project. Open your nuget explorer, search and install the following packages:

1. Microsoft.CodeAnalysis.CSharp
2. Microsoft.CodeAnalysis.Analyzers
3. Microsoft.Net.Compilers

Alternatively, you can edit your ".csproj" file by adding to it the following:

```xml
<ItemGroup>                                                                                            
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="3.8.0" PrivateAssets="all" />   
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.2" PrivateAssets="all" />
    <PackageReference Include="Microsoft.Net.Compilers.Toolset" Version="3.11.0-1.final">                      
        <PrivateAssets>all</PrivateAssets>                                                             
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>                                                                                
</ItemGroup>
```

> At the time of writing, the latest versions of those libraries not working properly, so in case of compilation issues, use the versions from the snippet above.

## Attaching the generator to a project

Now let's create a new project. This time we will create a Console application which will target the "net5.0" framework.

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%202.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%202.png)

2

We have to make sure, that our project is using the C# 9. This can be found in project properties window. 

In Rider, right-click on the project and select "Properties" and then make sure that the "Language Version" is set to "C# 9" and click "OK".

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%203.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%203.png)

3

The next step is to add our "Source Generator" as an "Analyzer Reference" to our project (remember that "Source Generator" is built on top of the "Code Analyzers"?).

The easiest way I found to do so is to add the "Source Generator" assembly as a regular reference and then to edit the ".csproj" file.

Under your project, right-click on the "Dependencies" and select "Add Reference". In the opened window, check the "Source Generator" project and click "Add".

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%204.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%204.png)

4.1

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%205.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%205.png)

4.2

Now, in the solution window, select the project and press F4 on your keyboard (or right-click the project → Edit → Edit '...csproj' ).

The content of the "csproj" file will look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net5.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
      <ProjectReference Include="..\SourceGeneration.SourceGenerator\SourceGeneration.SourceGenerator.csproj" />
    </ItemGroup>

</Project>
```

We need to modify the `ProjectReference` node by adding two attributes to it:

1. OutputItemType="Analyzer"
2. ReferenceOutputAssembly="false"

After the change, my "csproj" file is looking like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net5.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
      <ProjectReference Include="..\SourceGeneration.SourceGenerator\SourceGeneration.SourceGenerator.csproj" **OutputItemType="Analyzer" ReferenceOutputAssembly="false"** />
    </ItemGroup>

</Project>
```

At this point, we've finished with the preparations, so let's dive into source generation.

## Source generator planning

The first thing we want to do, before writing any code is to plan what "problem" we're gonna solve and how we're going to do it.

### The problem

In this example, I want to go with something simple (not necessarily best, performance-wise) and how can we replace reflection in our code with compile-time code.

At some point in your career you've probably wrote something like this:

```csharp
var values = Enum.GetValues<SomeEnum>();
var values = Enum.GetValues(typeof(SomeEnum)).Cast<SomeEnum>().ToArray();
```

So basically we have an enum, and we want to get all the values in this enum and the code above is pretty simple, but under the hood we're using reflection, so the execution is slow and we're generating garbage.

To demonstrate it better, I've created an enum with three values and run some benchmarks with "BenchmarkDotNet" package. Here's the results:

```csharp
|           Method |     Mean |   Error |  StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|--------:|--------:|-------:|------:|------:|----------:|
| GetValuesGeneric | 314.2 ns | 1.21 ns | 0.95 ns | 0.0134 |     - |     - |     112 B |
|        GetValues | 370.9 ns | 4.19 ns | 3.92 ns | 0.0181 |     - |     - |     152 B |
```

The "Mean" header defines the time in nanoseconds per 1000 executions and the "Allocated" header - how many Bytes were allocated per execution.

When I say we can do better, I mean we can achieve the following results:

```csharp
|             Method |      Mean |     Error |    StdDev |    Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------- |----------:|----------:|----------:|----------:|-------:|------:|------:|----------:|
| GetNonCachedValues | 4.5978 ns | 0.0574 ns | 0.0537 ns | 4.5835 ns | 0.0048 |     - |     - |      40 B |
|    GetCachedValues | 0.0007 ns | 0.0020 ns | 0.0017 ns | 0.0000 ns |      - |     - |     - |         - |
```

We can actually reduce the generated garbage to 40 Bytes (in this particular case) per enum, per application and reduce the execution time to 0.0007 nanoseconds per 1000 executions. Pretty nice, huh?

### The solution

Before writing the first line of code for our "Source Generator", we need to understand what we want to achieve.

Because "Source Generator" will add a normal ".cs" file to our project, it's always better to implement this class manually. This way we'll be able to generate this file easily.

In our console application, let's create an enum:

```csharp
namespace SourceGeneration.ConsoleApp
{
    public enum Animal
    {
        Cow,
        Pig,
        Chicken
    }
}
```

And now the solution:

```csharp
namespace SourceGeneration.ConsoleApp
{
    public static class AnimalExtension
    {
        public static readonly Animal[] Values = new[]
        {
            Animal.Cow,
            Animal.Pig,
            Animal.Chicken
        };
    }
}
```

Yep - it's that easy. We just create a static class with static array of the animals. But this is only easy, if we have a limited amount of enums that will never change - which is impossible, and if someone will try to maintain those "Extension" classes manually will find himself in maintenance hell.

At this point Roslyn "Source Generators" comes to the rescue. We can automate the generation of those classes.

> Note that this solution is not perfectly safe and was intentionally simplified for easier explanation of the topic. 
The values in this example can be changed in runtime and with this approach we don't have the control over when the array is created.

## Implementing first source generator

Now, when we know exactly what we want to do, it's time to write some code.

### The templates

When writing a generator, I love to use embedded resources as a template. Let's create one. Right-click on the "Source Generator" project → "Add" → "File" and call it something like `EnumExtensionTemplate.txt`.

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%206.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%206.png)

5.1

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%207.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%207.png)

5.2

Now right-click on the newly created file and then press on the "Properties" field. In the opened window change the "Build action:" to `EmbededResource`.

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%208.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%208.png)

6.1

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%209.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%209.png)

6.2

Let's look at the code we wrote earlier. If we want to convert this solution into a template, we need to identify the reusable parts. Usually I copy the whole solution and then start to replace some parts with a placeholders wrapped in double curly brackets.

This is what I managed so far:

```csharp
namespace {{NAMESPACE}}
{
    public static class {{ENUM_NAME}}Extension
    {
        public static readonly {{ENUM_NAME}}[] Values = new[]
        {
            {{INITIALIZATION}}
        };
    }
}
```

As you can see, in order to create a class we'll have to provide a namespace, an enum name and some initialization to this template. But wait a second... What's an "Initialization"?

Every enum has its own amount of values. We're going to provide them here by using a loop, so the best course of action is to create a template for the initialization row.

Let's create another template the same way we've created the previous one and call it `EnumValueInit.txt`. Here's what I've got:

```csharp
{{ENUM_NAME}}.{{VALUE}},
```

### The code

At this point all the preparations are done and it's time to write some code. We're starting with a generator class. Let's create one and call it `EnumExtensionGenerator`.

To tell the compiler that this class is acting as a "Source Generator", this class have to implement `ISourceGenerator` interface and we need to add `Generator` attribute to it (both of them located under "Microsoft.CodeAnalysis" namespace).

Here's how it should look like:

```csharp
using Microsoft.CodeAnalysis;

namespace SourceGeneration.SourceGenerator
{
    [Generator]
    public class EnumExtensionGenerator : ISourceGenerator
    {
    }
}
```

First thing I want to do is to write some code to load the templates. For this I'm writing a method that can load text assets from our embedded resources, creating two readonly variables and initialize them from the constructor. Here's the result:

```csharp
using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace SourceGeneration.SourceGenerator
{
    [Generator]
    public class EnumExtensionGenerator : ISourceGenerator
    {
        private readonly string _enumExtensionTemplate;
        private readonly string _enumValueInitTemplate;

        public EnumExtensionGenerator()
        {
            _enumExtensionTemplate = GetTemplate("EnumExtensionTemplate");
            _enumValueInitTemplate = GetTemplate("EnumValueInit");
        }

        private string GetTemplate(string name)
        {
            //The "SourceGeneration.SourceGenerator" is a default project namespace.
            //In a real project, this string should be extracted from the assembly instead
            //of being hard coded.
            var fullName = $"SourceGeneration.SourceGenerator.{name}.txt";

            //In real project, the assembly should be cached as well.
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName);

            if (stream == null)
            {
                throw new Exception("Template not found");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
```

Now it's time to implement the `ISourceGenerator` interface. Add the following methods to your class:

```csharp
public void Execute(GeneratorExecutionContext context)        
{                                                             
}                                                             
                                                              
public void Initialize(GeneratorInitializationContext context)
{                                                             
}
```

Now we need to find all enum declarations. We do it inside the "Execute" method:

```csharp
public void Execute(GeneratorExecutionContext context)                                                    
{                                                                                                         
    //Getting all the syntax trees                                                                        
    var trees = context.Compilation.SyntaxTrees;                                                          
                                                                                                          
    //Searching for enums                                                                                 
    var enums = trees                                                                                      
        .SelectMany(syntaxTree =>                                                                         
                syntaxTree                                                                                
                    .GetRoot() //From the root of the syntax tree                                         
                    .DescendantNodes() //taking all the descendant nodes                                  
                    .OfType<EnumDeclarationSyntax>() //and filtering them by taking only enum declarations
        ).ToArray();                                                                                      
                                                                                                          
    foreach (var @enum in enums)                                                                          
    {                                                                                                     
        CreateEnumExtension(@enum, context);                                                              
    }                                                                                                     
}
```

For each enum we've found, we need to get the values that we want to put into our template.

```csharp
private void CreateEnumExtension(EnumDeclarationSyntax @enum, GeneratorExecutionContext context)                 
{                                                                                                                
    var syntaxTree = @enum.SyntaxTree; //The syntax tree of this enum                                            
    var root = syntaxTree.GetRoot(); //Root node of the syntax tree                                              
    var semanticModel = context.Compilation.GetSemanticModel(syntaxTree); //Semantic model for the syntax tree   
                                                                                                                 
    //Searching for a namespace.                                                                                 
    //First we need to find the declaration of the namespace                                                     
    var namespaceDeclaration = root                                                                               
        .DescendantNodes() //Taking all the descendant nodes of this syntax tree                                 
        .OfType<NamespaceDeclarationSyntax>() //Filtering them and taking only the NamespaceDeclarations         
        .First(); //Taking the first one (realistically there can be multiple, so we should filter them as well) 
                                                                                                                 
    //Taking the namespace symbol from the semantic model                                                        
    var namespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceDeclaration) as INamespaceSymbol;             
                                                                                                                 
    if (namespaceSymbol == null)                                                                                 
    {                                                                                                            
        throw new Exception("Namespace not found");                                                              
    }                                                                                                            
                                                                                                                 
    //Getting the namespace string from the compilation by using the namespace symbol.                           
    var namespaceString = context.Compilation.GetCompilationNamespace(namespaceSymbol)?.ToString();              
    //Taking the name of the enum from the EnumDeclaration.                                                      
    var enumName = @enum.Identifier.ToString();                                                                  
                                                                                                                 
    //Searching for all enum values                                                                              
    var values = @enum                                                                                            
        .DescendantNodes() //All descendant nodes of the enum                                                    
        .OfType<EnumMemberDeclarationSyntax>() //filtered by EnumMemberDeclaration                               
        .ToList();                                                                                               
                                                                                                                 
    GenerateExtensionSource(namespaceString, enumName, values, context);                                         
}
```

And the last step is to replace some values and to add our code to the compilation process.

```csharp
private void GenerateExtensionSource(string namespaceString, string enumName,              
    List<EnumMemberDeclarationSyntax> values, GeneratorExecutionContext context)           
{                                                                                          
    var initializations = new StringBuilder();                                             
                                                                                           
    //Building the initialization string                                                   
    foreach (var value in values)                                                          
    {                                                                                      
        //Just replacing the placeholders with real values                                 
        var initString = _enumValueInitTemplate                                            
            .Replace("{{ENUM_NAME}}", enumName)                                            
            .Replace("{{VALUE}}", value.Identifier.ToString());                            
                                                                                           
        initializations.AppendLine(initString);                                            
    }                                                                                      
                                                                                           
    //Same here - replacing the placeholders                                               
    var source = _enumExtensionTemplate                                                    
        .Replace("{{NAMESPACE}}", namespaceString)                                         
        .Replace("{{ENUM_NAME}}", enumName)                                                
        .Replace("{{INITIALIZATION}}", initializations.ToString());                        
                                                                                           
    //Adding a C# class to the compilation.                                                
    context.AddSource(                                                                     
        $"{enumName}Extension.cs",                                                         
        SourceText.From(source, Encoding.UTF8)                                             
    );                                                                                     
}
```

### Usage

That's it. Now we can recompile the whole project and use our generated code that will generate an extension class for each enum in the project.

Let's head to our console project and try to access the generated code:

```csharp
class Program                                        
{                                                    
    static void Main(string[] args)                  
    {                                                
        Console.WriteLine(AnimalExtension.Values[0]);
    }                                                
}
```

If we'll modify the existing enum or create a new one, let's say "Vehicle", we'll be able to access the extension class the same way, without writing a single line of code.

> Note, that Rider sometimes will not recognize the generated code, but the compilation will not result in any errors. The quick fix is to relaunch Rider and to hope that JetBrains team will handle it ASAP.

### Possible issues

While the code we've written so far will work properly, it will run for every enum in the project and in most of the times this is not desired. Specially if you want to use this generator in Unity project. In Unity this can lead to weird errors, and performance degradation.

The best way to filter on what enums we want to run our Generator would be to add an Attribute.

### Filtering by attribute

The attribute we're going to use can be created multiple ways. We can create a "Source Generator" that will add an attribute to the compilation. We can create another assembly with all the required attributes. Or we can create an attribute manually in our Console class. 

And while the 2 former options are without a doubt better, for the sake of simplicity we'll use the third one (I need to count how many times I'm telling this "for the sake of simplicity" thingie).

Let's create a class in our Console application and call it `ExtendAttribute`, inherit it from the `Attribute` class and add a target via `AttributeUsage` attribute. It should look like this:

```csharp
[AttributeUsage(AttributeTargets.Enum)]         
public class ExtendAttribute : Attribute        
{                                               
                                                
}
```

Now let's head to the `EnumExtensionGenerator` and add the following method:

```csharp
private bool HasAttribute(EnumDeclarationSyntax syntax, SemanticModel semanticModel)                                                       
{                                                                                                                                          
    //If there is no attribute lists, then there is no attribute...                                                                        
    if (syntax.AttributeLists.Count == 0)                                                                                                  
    {                                                                                                                                      
        return false;                                                                                                                      
    }                                                                                                                                      
                                                                                                                                           
    const string AttributeName = "ExtendAttribute";                                                                                        
                                                                                                                                           
    return syntax.AttributeLists //From all attribute lists                                                                                
        .SelectMany(attributeList => attributeList.Attributes) //Get all attributes                                                        
        .Any(attribute => semanticModel.GetTypeInfo(attribute).Type.Name == AttributeName); //And check their name against pre-defined one.
}
```

After this we need to filter our results by using this method. Change the `Execute` method to look like this:

```csharp
public void Execute(GeneratorExecutionContext context)                                                      
{                                                                                                           
    //Getting all the syntax trees                                                                          
    var trees = context.Compilation.SyntaxTrees;                                                            
                                                                                                            
    //Searching for enums                                                                                   
    var enums = trees                                                                                        
        .SelectMany(syntaxTree =>                                                                           
                syntaxTree                                                                                  
                    .GetRoot() //From the root of the syntax tree                                           
                    .DescendantNodes() //taking all the descendant nodes                                    
                    .OfType<EnumDeclarationSyntax>() //and filtering them by taking only enum declarations  
                    **.Where(syntax => HasAttribute(syntax, context.Compilation.GetSemanticModel(syntaxTree)))**
        )                                                                                                    
        .ToArray();                                                                                         
                                                                                                            
    foreach (var @enum in enums)                                                                            
    {                                                                                                       
        CreateEnumExtension(@enum, context);                                                                
    }                                                                                                       
}
```

Now our method will work only with enums that decorated with the `Extend` attribute. Head to the Animal enum and add this attribute to it. It should look like this:

```csharp
[Extend]          
public enum Animal
{                 
    Cow,          
    Pig,          
    Chicken       
}
```

That's it!

## A bit on Roslyn API

At first glance the Roslyn API can be a bit scary, but once you've worked with it for a bit, it becomes second nature.

Basically we get a list of Syntax Trees. Each Tree is a file in our project, so by iterating on those, we can access the content of the files in the project.

For me, the best way to learn it was to install the "[Syntax Visualizer](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/syntax-visualizer?tabs=csharp)" plugin for Visual Studio and every time I want to know how to find something in the syntax tree, I look at it through this plugin.

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%2010.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%2010.png)

7

## Implementing Code Analyzer

### Intro

When we've implements a custom "Source Generator", we already did some code analysis. We searched for enums in our code and inspected their values, but in order to make it a real analyzer, we need to do something with our analysis and output some result to the programmer.

### The problem

Let's say we have an issue with naming conventions and always forget to name our classes in PascalCase. Instead, we usually name in in camelCase, and we all can agree that this is a serious problem.

So we want to create a custom analyzer to prevent it. And yeah, any modern IDE already have this analyzer built in, but for the sake of simplicity, we'll stick to this one anyway.

### The solution

To solve the issue, we'll need to write one class only. Let's start by creating a class called `ClassNamingAnalyzer`. This class should be inherited from `DiagnosticAnalizer` class and must have `DiagnosticAnalyze` attribute (both in the Microsoft.CodeAnalysis.Diagnostics namespace).

```csharp
using Microsoft.CodeAnalysis.Diagnostics;

namespace SourceGeneration.SourceGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassNamingAnalyzer : DiagnosticAnalyzer
    {
        
    }
}
```

Now let's add some constants to it (because the class will be reused and triggered many times, we should reuse the things we can in order to not slow down the compilation process).

```csharp
//Unique error code ID.
//It is public, because we're gonna use later 
public const string DiagnosticID = "TEST001"; 

//The title of the analyzer
private const string Title = "Class naming conventions analyzer"; 

//The message for the developer
private const string MessageFormat = "Class name must be written in PascalCase"; 

//Category of this analyzer
private const string Category = "Declaration"; 

//The severity of the issue
private const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;
```

Let's use those constants and create a reusable rule.

```csharp
private static readonly DiagnosticDescriptor Rule = 
    new DiagnosticDescriptor(                       
        id: DiagnosticID,                           
        title: Title,                               
        messageFormat: MessageFormat,               
        category: Category,                         
        defaultSeverity: Severity,                  
        isEnabledByDefault: true);
```

After we've created a rule, we need to implement abstract methods and properties of the base class. In our case - it's one property and one  method.

We'll start with the `SupportedDiagnostics` property. We will create an `ImmutableArray` from our one rule (we can always make more than one rule).

```csharp
public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
```

Next step is to implement the `Initialize` method, where we are registering our hook.

```csharp
public override void Initialize(AnalysisContext context)                                          
{                                                                                                 
    //Setting our analyzer modes.                                                                 
    //Basically telling the compiler that we can analyze and report diagnostics.                  
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |                   
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);         
                                                                                                  
    //Allowing multiple concurrent executions of this analyzer.                                   
    //This will increase the performance of the analyzer, but some analyzers                      
    //might not support this - make sure you are writing thread safe code.                        
    context.EnableConcurrentExecution();                                                          
                                                                                                  
    //Telling the compiler when we want our analyzer to be triggered,                             
    //and what method to use when it's triggered.                                                 
    //There are multiple register methods and each is triggered after different event is happened.
    //When writing an analyzer, make sure to use the one that suits you best.                     
    context.RegisterSemanticModelAction(AnalyzeTree);                                             
}
```

And for the final part, we need to implement the analysis.

```csharp
private void AnalyzeTree(SemanticModelAnalysisContext context)                                                
{                                                                                                             
    var trees = context.SemanticModel.Compilation.SyntaxTrees;                                                
                                                                                                              
    var classes = trees                                                                                        
        .SelectMany(syntaxTree =>                                                                             
                syntaxTree                                                                                    
                    .GetRoot() //From the root of the syntax tree                                             
                    .DescendantNodes() //taking all the descendant nodes                                      
                    .OfType<ClassDeclarationSyntax>() //and filtering them by taking only class declarations  
        ).ToArray();                                                                                          
                                                                                                              
    foreach (var @class in classes)                                                                           
    {                                                                                                         
        var className = @class.Identifier.ToString(); //Finding the class name                                
        var isNotPascalCase = char.IsLower(className[0]);                                                     
                                                                                                              
        if (isNotPascalCase)                                                                                  
        {                                                                                                     
            var location = @class.Identifier.GetLocation(); //Finding the location of the class name          
            var diagnostic = Diagnostic.Create(Rule, location); //Creating the diagnostic                     
                                                                                                              
            context.ReportDiagnostic(diagnostic); //Reporting it                                              
        }                                                                                                     
    }                                                                                                         
}
```

## Code fixes

When we highlight a syntax as an error or warning, the user expects to be able to resolve this issue with a quick shortcut. You know, the one you usually see in the context menu of the error?

Roslyn provides us with the ability to register code fixes for any issue, even if the analyzer detecting the issue is not written by us. 

Let's start by adding another nuget package to the project (the `SourceGenerator` one).  As earlier, we can do it from nuget explorer window or by editing the `csproj` file. If you decided to add it from the nuget explorer window, search for "Microsoft.CodeAnalysis.CSharp.Workspaces". 

The `csproj` file should contain the following:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <!-- Some nodes here -->

    <ItemGroup>
				<!-- Some PackageReferences here -->
        <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="3.8.0" />
    </ItemGroup>

    <!-- Some nodes here -->

</Project>
```

> When adding this package, make sure that its version is exactly the same as the version for the "Microsoft.CodeAnalysis.CSharp" package.

Now let's create a class called `ClassNamingFixProvider`. This class should be inherited from the `CodeFixProvided` class and must have the `ExportCodeFixProvider` attribute.

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace SourceGeneration.SourceGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassNamingFixProvider))]
    public class ClassNamingFixProvider : CodeFixProvider
    {
    }
}
```

Let's override the "abstracts" of `CodeFixProvider`. First we override the `FixableDiagnosticIds`. Remember the unique ID of the analyzer? The one whos constant we've made public? Let's use it here:

```csharp
public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(ClassNamingAnalyzer.DiagnosticID);
```

The next step is to register our code fix for each diagnostic.

```csharp
public override Task RegisterCodeFixesAsync(CodeFixContext context)               
{                                                                                 
    //Context is providing us with an array of diagnostics                        
    //with the ID we've registered in the 'FixableDiagnosticsIds' field.          
    //We iterate through all of them and registering a code fix.                  
    foreach (var diagnostic in context.Diagnostics)                               
    {                                                                             
        //First we create CodeAction                                              
        var codeAction = CreateCodeAction(context, diagnostic);                   
        //And then registering the fix with this action                                            
        context.RegisterCodeFix(codeAction, diagnostic);                          
                                                                                  
        //If we have more than one solution to the issue, we can register multiple
        //different fixes.                                                        
    }                                                                             
                                                                                  
    //We could have made this method async, but since we don't                    
    //have any async/await usage, it's better to just return a completed task.    
    return Task.CompletedTask;                                                    
}
```

To create an action, we use a factory:

```csharp
private CodeAction CreateCodeAction(CodeFixContext context, Diagnostic diagnostic)
{                                                                                 
    //We're creating a fix action that can be triggered from the context menu     
    var action = CodeAction.Create(                                               
        title: "Change class name to PascalCase", //Usually this should be a const
        createChangedDocument: cancellationToken =>                               
            GetEditedDocument(context.Document, diagnostic, cancellationToken),   
        equivalenceKey: nameof(ClassNamingFixProvider)                            
    );                                                                            
                                                                                  
    return action;                                                                
}
```

And finally, we're fixing the code itself:

```csharp
private async Task<Document> GetEditedDocument(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
{                                                                                                                            
    //Getting the root of the document                                                                                       
    var root = await document.GetSyntaxRootAsync(cancellationToken);                                                         
    //Finding the token we need to change by it's location provided by our diagnostic                                        
    var token = root.FindToken(diagnostic.Location.SourceSpan.Start);                                                        
                                                                                                                             
    //This is the original class name                                                                                        
    var oldName = token.ValueText;                                                                                           
    //Replacing the first char with Uppercase                                                                                
    //This will not guarantee that the class name is proper PascalCase, but this is out of our scope.                        
    var newName = char.ToUpper(oldName[0]) + oldName.Substring(1);                                                           
                                                                                                                             
    //Finding the Identifier syntax (class name syntax in our case)                                                          
    var declarationSyntax = (ClassDeclarationSyntax) token.Parent;                                                            
    //Creating new Identifier syntax                                                                                         
    var newDeclarationSyntax = declarationSyntax.WithIdentifier(SyntaxFactory.Identifier(newName));                            
                                                                                                                             
    //Replacing the old class name with the new one and getting a new root object                                            
    var newRoot = root.ReplaceNode(declarationSyntax, newDeclarationSyntax);                                                   
                                                                                                                             
    //This class can be referenced somewhere, so by changing it's name we can break the code,                                
    //so normally we should go over all of the syntax trees and replace the name everywhere,                                 
    //but this goes beyond the scope of this example                                                                         
                                                                                                                             
    //Replacing the root of the document with the one we created                                                             
    return document.WithSyntaxRoot(newRoot);                                                                                 
}
```

## Usage in Unity

### Requirements

While "Code Analyzers" are already almost fully supported in Unity since 2020.3, "Source Generators" require Roslyn 3.8.0 or higher. At the time of writing Unity works with Roslyn 3.5.0, but support for the latest stable Roslyn compiler and "Source Generators" are marked as a stretch goal for Unity 2021.2 (probably will be released a bit later than that).

### Adding "Source Generation" to Unity project

First we need to compile our project (preferably in a "Release" mode) and import the compiled DLL to any folder under the "Assets" folder in Unity project (preferably under "Plugins" folder).

After we've imported the DLL, we need to make sure it is not built into our project and used only while we code and compile. We do it by unchecking all platforms under "Select platforms for plugin" in the inspector window.

![Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%2011.png](Code%20Analyzers%20and%20Source%20Generation%2087f793b5f3a1494fac1c857b7eeef6f9/Untitled%2011.png)

8

The last step is to add `RoslynAnalyzer` label to this DLL. In the Inspector window, click on the small label icon, then in the small search bar write `RoslynAnalyzer` and press enter.

Now let's copy our `ExtendAttribute` into Unity so we'll be able to use it. By the way, this copying is exactly why this approach is not so good, but again - this is the easiest way to explain. 🤷

After the project will finish to recompile, our "Source Generator" and the "Code Analyzer" will work inside the unity project.

## Useful Links

All of the code written in this article can be found here:
[https://github.com/migus88/Tutorials-SourceGeneration](https://github.com/migus88/Tutorials-SourceGeneration)

An article on "Code Analysis" and "Code fixes":
[https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)

Introduction to "Source Generation":
[https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/)

Another nice article on "Source Generation":
[https://dominikjeske.github.io/source-generators/](https://dominikjeske.github.io/source-generators/)

Video about compile-time DI frameworks:
[https://youtu.be/nqvnqzRNBls](https://youtu.be/nqvnqzRNBls)

[Presentation (HE)](https://www.notion.so/Presentation-HE-95d7e5a372154c60bdfc528336d38a89)