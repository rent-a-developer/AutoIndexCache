// ReSharper disable once CheckNamespace

namespace System.Runtime.CompilerServices;

// The type CallerArgumentExpressionAttribute is missing in .NET Standard 2.1, so we have to define it here.
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class CallerArgumentExpressionAttribute(String parameterName) : Attribute
{
    public String ParameterName { get; } = parameterName;
}