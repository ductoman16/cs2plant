namespace cs2plant.Models;

public sealed record ClassInfo(
    string Name,
    string Namespace,
    bool IsSealed,
    bool IsRecord,
    IReadOnlyList<string> BaseTypes,
    IReadOnlyList<PropertyInfo> Properties,
    IReadOnlyList<MethodInfo> Methods);

public sealed record PropertyInfo(
    string Name,
    string Type,
    bool HasGet,
    bool HasSet,
    bool HasInit);

public sealed record MethodInfo(
    string Name,
    string ReturnType,
    bool IsAsync,
    bool IsStatic,
    bool IsOverride,
    IReadOnlyList<ParameterInfo> Parameters);

public sealed record ParameterInfo(
    string Name,
    string Type); 