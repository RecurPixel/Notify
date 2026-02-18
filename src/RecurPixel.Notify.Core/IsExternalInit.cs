// This shim is required to use 'init' setters in netstandard2.1.
// The type is built into .NET 5+ but must be declared manually for netstandard2.1.
// This file can be removed if the target framework is ever raised to net5.0+.

namespace System.Runtime.CompilerServices;

internal static class IsExternalInit { }