using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGeneration.SourceGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassNamingFixProvider))]
    public class ClassNamingFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(ClassNamingAnalyzer.DiagnosticID);
        
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
            var identifierSyntax = (ClassDeclarationSyntax) token.Parent;
            //Creating new Identifier syntax
            var newIdentifierSyntax = identifierSyntax.WithIdentifier(SyntaxFactory.Identifier(newName));

            //Replacing the old class name with the new one and getting a new root object
            var newRoot = root.ReplaceNode(identifierSyntax, newIdentifierSyntax);
            
            //This class can be referenced somewhere, so by changing it's name we can break the code,
            //so normally we should go over all of the syntax trees and replace the name everywhere,
            //but this goes beyond the scope of this example

            //Replacing the root of the document with the one we created
            return document.WithSyntaxRoot(newRoot);
        }
        
    }
}