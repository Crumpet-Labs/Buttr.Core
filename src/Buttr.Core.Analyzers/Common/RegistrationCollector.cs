using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Buttr.Core.Analyzers {
    public sealed class RegistrationCollector {
        public RegistrationModel Model { get; } = new();

        private readonly Dictionary<SyntaxNode, Registration> m_ByCallSite = new();
        private readonly List<IInvocationOperation> m_PendingAliases = new();

        public void RegisterCallbacks(CompilationStartAnalysisContext context) {
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        public RegistrationModel FinalizeModel() {
            ResolvePendingAliases();
            return Model;
        }

        private void AnalyzeInvocation(OperationAnalysisContext context) {
            var operation = (IInvocationOperation)context.Operation;
            var method = operation.TargetMethod;

            if (method.Name == "As") {
                lock (Model) m_PendingAliases.Add(operation);
                return;
            }

            if (method.Name is not ("AddSingleton" or "AddTransient")) return;
            if (method.TypeArguments.Length == 0) return;

            var receiverType = operation.Instance?.Type;
            if (receiverType is null) return;

            var containerContext = ClassifyReceiver(receiverType, operation);
            if (containerContext is null) return;

            string keyTypeFullName;
            string concreteTypeFullName;
            string keyType;
            string concreteType;

            if (method.TypeArguments.Length == 1) {
                keyTypeFullName = method.TypeArguments[0].ToDisplayString();
                concreteTypeFullName = keyTypeFullName;
                keyType = method.TypeArguments[0].Name;
                concreteType = keyType;
            }
            else {
                keyTypeFullName = method.TypeArguments[0].ToDisplayString();
                concreteTypeFullName = method.TypeArguments[1].ToDisplayString();
                keyType = method.TypeArguments[0].Name;
                concreteType = method.TypeArguments[1].Name;
            }

            var lifetime = method.Name == "AddSingleton" ? Lifetime.Singleton : Lifetime.Transient;

            var registration = new Registration {
                KeyType = keyType,
                KeyTypeFullName = keyTypeFullName,
                ConcreteType = concreteType,
                ConcreteTypeFullName = concreteTypeFullName,
                Container = containerContext.Value.Kind,
                ScopeKey = containerContext.Value.ScopeKey,
                Lifetime = lifetime,
                Visibility = containerContext.Value.Visibility,
                CallSite = operation.Syntax.GetLocation(),
                BuilderSymbol = TryGetBuilderSymbol(operation)
            };

            lock (Model) {
                m_ByCallSite[operation.Syntax] = registration;

                switch (registration.Container) {
                    case ContainerKind.Application:
                        Model.ApplicationRegistrations.Add(registration);
                        break;
                    case ContainerKind.DI:
                        Model.DIRegistrations.Add(registration);
                        break;
                    case ContainerKind.Scope:
                        if (!Model.ScopeRegistrations.ContainsKey(registration.ScopeKey!))
                            Model.ScopeRegistrations[registration.ScopeKey!] = new();
                        Model.ScopeRegistrations[registration.ScopeKey!].Add(registration);
                        break;
                }
            }
        }

        private void ResolvePendingAliases() {
            lock (Model) {
                foreach (var aliasOperation in m_PendingAliases) CaptureAlias(aliasOperation);
                m_PendingAliases.Clear();
            }
        }

        private void CaptureAlias(IInvocationOperation aliasOperation) {
            var aliasMethod = aliasOperation.TargetMethod;
            if (aliasMethod.TypeArguments.Length != 1) return;

            var rootSyntax = FindRootRegistrationSyntax(aliasOperation);
            if (rootSyntax is null) return;

            Registration? registration;
            lock (Model) {
                m_ByCallSite.TryGetValue(rootSyntax, out registration);
            }

            if (registration is null) return;

            var aliasKey = new AliasKey {
                AliasTypeFullName = aliasMethod.TypeArguments[0].ToDisplayString(),
                CallSite = GetAliasNameLocation(aliasOperation)
            };

            lock (Model) registration.Aliases.Add(aliasKey);
        }

        private static Location GetAliasNameLocation(IInvocationOperation aliasOperation) {
            if (aliasOperation.Syntax is InvocationExpressionSyntax invocation
                && invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Name is GenericNameSyntax generic) {
                return generic.GetLocation();
            }

            return aliasOperation.Syntax.GetLocation();
        }

        private static SyntaxNode? FindRootRegistrationSyntax(IInvocationOperation aliasOperation) {
            var current = aliasOperation.Instance;
            while (current is IInvocationOperation innerInvocation) {
                var name = innerInvocation.TargetMethod.Name;
                if (name is "AddSingleton" or "AddTransient")
                    return innerInvocation.Syntax;
                if (name != "As" && name != "WithConfiguration" && name != "WithFactory")
                    return null;
                current = innerInvocation.Instance;
            }

            return null;
        }

        private readonly struct ReceiverContext {
            public ReceiverContext(ContainerKind kind, Visibility visibility, string? scopeKey) {
                Kind = kind;
                Visibility = visibility;
                ScopeKey = scopeKey;
            }

            public ContainerKind Kind { get; }
            public Visibility Visibility { get; }
            public string? ScopeKey { get; }
        }

        private static ReceiverContext? ClassifyReceiver(ITypeSymbol receiverType, IInvocationOperation operation) {
            ITypeSymbol? builderType = receiverType;
            IOperation? builderOperation = operation.Instance;
            var visibility = Visibility.Public;

            if (operation.Instance is IPropertyReferenceOperation propertyRef) {
                if (propertyRef.Property.Name == "Hidden") visibility = Visibility.Hidden;
                builderOperation = propertyRef.Instance;
                builderType = propertyRef.Instance?.Type;
            }

            if (builderType is null) return null;
            var builderName = builderType.ToDisplayString();

            if (builderName.Contains("ApplicationBuilder")) {
                return new ReceiverContext(ContainerKind.Application, visibility, null);
            }

            if (builderName.Contains("ScopeBuilder")) {
                return new ReceiverContext(ContainerKind.Scope, visibility, TryExtractScopeKey(builderOperation));
            }

            if (builderName.Contains("DIBuilder") || builderName.EndsWith(".DIBuilder")) {
                return new ReceiverContext(ContainerKind.DI, visibility, null);
            }

            return null;
        }

        private static ISymbol? TryGetBuilderSymbol(IInvocationOperation operation) {
            IOperation? current = operation.Instance;
            if (current is IPropertyReferenceOperation prop) current = prop.Instance;
            return current switch {
                ILocalReferenceOperation l => l.Local,
                IFieldReferenceOperation f => f.Field,
                IParameterReferenceOperation p => p.Parameter,
                _ => null
            };
        }

        private static string? TryExtractScopeKey(IOperation? builderOperation) {
            if (builderOperation is ILocalReferenceOperation localRef) {
                var local = localRef.Local;
                foreach (var syntaxRef in local.DeclaringSyntaxReferences) {
                    if (syntaxRef.GetSyntax() is VariableDeclaratorSyntax {
                            Initializer.Value: ObjectCreationExpressionSyntax creation
                        }
                        && creation.ArgumentList?.Arguments.Count > 0
                        && creation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal
                        && literal.Token.Value is string key) {
                        return key;
                    }
                }
            }

            if (builderOperation is IObjectCreationOperation objCreation
                && objCreation.Arguments.Length > 0
                && objCreation.Arguments[0].Value is ILiteralOperation lit
                && lit.ConstantValue.Value is string directKey) {
                return directKey;
            }

            return null;
        }
    }
}
