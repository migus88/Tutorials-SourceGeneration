using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
                            .Where(syntax => HasAttribute(syntax, context.Compilation.GetSemanticModel(syntaxTree)))
                )
                .ToArray();

            foreach (var @enum in enums)
            {
                CreateEnumExtension(@enum, context);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG

            /*if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }*/
#endif
        }


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