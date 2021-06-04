using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SourceGeneration.SourceGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassNamingAnalyzer : DiagnosticAnalyzer
    {
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

        //The rule
        private static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                id: DiagnosticID,
                title: Title,
                messageFormat: MessageFormat,
                category: Category,
                defaultSeverity: Severity,
                isEnabledByDefault: true);


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
    }
}