using System.Data;
using System.Data.Common;
using Dapper;

namespace Web.Models;

/// <summary>
/// Dapper type handler that converts typed-string values to SQL parameters via ToString().
/// Each concrete typed-string registers an instance in its own static constructor.
/// </summary>
public class TypedStringTypeHandler<T> : SqlMapper.TypeHandler<T>
  where T : TypedString
{
  public override T? Parse(object value)
  {
    if (value is not string stringValue)
      throw new InvalidOperationException(
        $"TypedStringTypeHandler<{typeof(T).Name}>: expected SQL type to be a string."
      );

    var constructor =
      typeof(T).GetConstructor([typeof(string)])
      ?? throw new InvalidOperationException(
        $"TypedStringTypeHandler<{typeof(T).Name}>: public string constructor not found on '{typeof(T).FullName}'."
      );
    return constructor.Invoke([stringValue]) as T;
  }

  public override void SetValue(IDbDataParameter parameter, T value) =>
    parameter.Value = (string)value;
}
