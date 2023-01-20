using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HK.Modding.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MonoModHookAnalyzer : DiagnosticAnalyzer
    {
        public static readonly string RuleId = "HKM001";

        private static readonly DiagnosticDescriptor NoCallOrig = new("HKM001",
            Utils.GetResourceString(nameof(Resources.HKM001_Title)),
            Utils.GetResourceString(nameof(Resources.HKM001_Format)),
            "",
            DiagnosticSeverity.Warning,
            true
            );
        private static readonly DiagnosticDescriptor AssignOrig = new("HKM002",
            Utils.GetResourceString(nameof(Resources.HKM002_Title)),
            Utils.GetResourceString(nameof(Resources.HKM002_Format)),
            "",
            DiagnosticSeverity.Warning,
            true
            );
        private static readonly DiagnosticDescriptor AnalyzerExcpetion = new("HKMERROR",
            Utils.GetResourceString(nameof(Resources.HKM_Error_Title)),
            Utils.GetResourceString(nameof(Resources.HKM_Error_Msg)),
            "",
            DiagnosticSeverity.Info,
            true
            );
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(NoCallOrig, AssignOrig, AnalyzerExcpetion);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }
        private static void AnalyzeSymbol(IMethodSymbol method, Compilation compilation,
            SymbolAnalysisContext context, SyntaxNode md)
        {
            try
            {
                if (!method.IsDefinition) return;
                
                if (md is null)
                {
                    md = method.Locations[0].SourceTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
                    if(md is null) return;
                    if (md is not MethodDeclarationSyntax) return;
                }
                var seg = compilation.GetSemanticModel(md.SyntaxTree, false);
                foreach (var c in md.DescendantNodes())
                {
                    if (c is LambdaExpressionSyntax lambda)
                    {
                        var m = (IMethodSymbol)seg.GetSymbolInfo(lambda).Symbol;
                        AnalyzeSymbol(m, compilation, context, c);

                    }
                    if (c is LocalFunctionStatementSyntax lf)
                    {
                        var m = (IMethodSymbol)seg.GetSymbolInfo(lf).Symbol;
                        if(m is null) return;
                        AnalyzeSymbol(m, compilation, context, c);
                    }
                }
                if (method.Parameters.Length == 0) return;
                var first = method.Parameters[0];
                var origType = first.Type;

                if (origType.TypeKind != TypeKind.Delegate) return;
                if (!origType.Name.StartsWith("orig_")) return;


                var callOrig = !string.IsNullOrEmpty(first.Name) && first.Name.StartsWith("_");
                foreach (var c in md.DescendantNodes())
                {
                    if (c is InvocationExpressionSyntax)
                    {
                        var id = c.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                        if (id == null) continue;
                        if (seg.GetSymbolInfo(id).Symbol is not IParameterSymbol p) continue;
                        if (SymbolEqualityComparer.Default.Equals(p, first))
                        {
                            callOrig = true;
                        }
                    }
                    if (c is AssignmentExpressionSyntax assign)
                    {
                        if (assign.Right is not IdentifierNameSyntax id) continue;
                        if (seg.GetSymbolInfo(id).Symbol is not IParameterSymbol p) continue;
                        if (seg.GetSymbolInfo(assign.Left).Symbol is not IFieldSymbol and IPropertySymbol and IEventSymbol) continue;
                        if (SymbolEqualityComparer.Default.Equals(p, first))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(AssignOrig, id.GetLocation(), first.Name));
                        }
                    }
                }
                if (!callOrig) context.ReportDiagnostic(Diagnostic.Create(NoCallOrig, first.Locations[0], first.Name));
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(AnalyzerExcpetion, method.Locations[0], 0, e.ToString()));
            }
        }
        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            AnalyzeSymbol((IMethodSymbol)context.Symbol, context.Compilation, context, null);
        }
    }
}
