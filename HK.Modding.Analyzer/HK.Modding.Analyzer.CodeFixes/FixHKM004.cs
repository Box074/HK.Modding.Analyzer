using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using System.Linq;
using Microsoft.CodeAnalysis.Editing;

namespace HK.Modding.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class FixHKM004 : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("HKM0004");
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var mae = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf()
                .OfType<MemberAccessExpressionSyntax>().First();

            context.RegisterCodeFix(CodeAction.Create(
                CodeFixResources.HKM004_Title,
                createChangedDocument: c => Fix(context.Document, mae, c)
                ), diagnostic);
        }
        private async Task<Document> Fix(Document document, MemberAccessExpressionSyntax mae, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document);
            editor.SetName(mae, "UnsafeInstance");
            return editor.GetChangedDocument();
        }
    }
}
