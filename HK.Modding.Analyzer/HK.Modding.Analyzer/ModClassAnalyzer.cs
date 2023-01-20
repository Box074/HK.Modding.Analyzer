using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace HK.Modding.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ModClassAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor GetModOnCtor = new("HKM003",
            Utils.GetResourceString(nameof(Resources.HKM003_Title)),
            Utils.GetResourceString(nameof(Resources.HKM003_Format)),
            "",
            DiagnosticSeverity.Warning,
            true
            );
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(GetModOnCtor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(SymbolAnalyzerModClass, SymbolKind.NamedType);
        }
        private static void SymbolAnalyzerCtor(IMethodSymbol method, SymbolAnalysisContext context)
        {
            var md = method.Locations[0].SourceTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
            if (md is null) return;
            if (md is not ConstructorDeclarationSyntax) return;
            var seg = context.Compilation.GetSemanticModel(md.SyntaxTree, false);
            foreach (var node in md.DescendantNodes(x => !(x is (LambdaExpressionSyntax or LocalFunctionStatementSyntax))))
            {
                if(node is InvocationExpressionSyntax invoke)
                {
                    var cm = (IMethodSymbol)seg.GetSymbolInfo(invoke).Symbol;
                    if (cm is null) continue;
                    if (cm.Name != "GetMod") continue;
                    if (cm.ContainingType.GetFullName() != "Modding.ModHooks") continue;
                    context.ReportDiagnostic(Diagnostic.Create(GetModOnCtor, node.GetLocation()));
                }
            }
        }
        private static void SymbolAnalyzerModClass(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (!type.AllInterfaces.Any(x => x.GetFullName() == "Modding.IMod")) return;
            foreach (var ctor in type.GetMembers(".ctor").Concat(type.GetMembers(".cctor")).OfType<IMethodSymbol>())
            {
                SymbolAnalyzerCtor(ctor, context);
            }
        }
    }
}
