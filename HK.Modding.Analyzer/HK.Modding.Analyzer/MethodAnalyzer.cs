using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace HK.Modding.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MethodAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor CheckHeroOrGameManager = new("HKM004",
            Utils.GetResourceString(nameof(Resources.HKM004_Title)),
            Utils.GetResourceString(nameof(Resources.HKM004_Format)),
            "",
            DiagnosticSeverity.Info,
            true
            );
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(CheckHeroOrGameManager);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(HC_GM_Check, SyntaxKind.EqualsExpression, 
                SyntaxKind.NotEqualsExpression);
        }

        private static void HC_GM_Check(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as BinaryExpressionSyntax;
            if (context.SemanticModel.GetSymbolInfo(syntax).Symbol is not IMethodSymbol symbol) return;
            if (symbol.Parameters.Length != 2) return;
            var c = new[] { syntax.Left, syntax.Right };
            var className = "";
            Location propLoc = null;
            if (!c.Any(x =>
            {
                if (x is not LiteralExpressionSyntax l) return false;
                return l.IsKind(SyntaxKind.NullLiteralExpression);
            })) return;
            if (!c.Any(x =>
            {
                if (x is not MemberAccessExpressionSyntax i) return false;
                if (context.SemanticModel.GetSymbolInfo(i).Symbol is not IPropertySymbol m) return false;
                if (m.Name != "instance") return false;
                var fn = m.ContainingType.GetFullName();
                if (fn != "HeroController" && fn != "GameManager") return false;
                className = fn;
                propLoc = i.GetLocation();
                return true;
            })) return;
            context.ReportDiagnostic(Diagnostic.Create(CheckHeroOrGameManager, syntax.GetLocation(), className));
        }
    }
}
