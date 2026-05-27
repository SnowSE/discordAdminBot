using Dapper;

namespace Web.Models;

public class TypedIntTypeHandler<TTypedInt> : SqlMapper.TypeHandler<TTypedInt>
  where TTypedInt : TypedInt
{
  public override void SetValue(System.Data.IDbDataParameter parameter, TTypedInt? value)
  {
    if (value is null)
      throw new InvalidOperationException(
        $"Cannot set null value for non-nullable type '{typeof(TTypedInt).FullName}' in database parameter '{parameter.ParameterName}'."
      );
    parameter.Value = (int)value.Value;
  }

  public override TTypedInt Parse(object field)
  {
    if (field == null || field is System.DBNull)
    {
      throw new InvalidOperationException(
        $"Cannot parse a null database value into non-nullable {typeof(TTypedInt).FullName}."
      );
    }

    var intValue = Convert.ToInt32(field);
    return Activator.CreateInstance(typeof(TypedInt), [intValue]) as TTypedInt
      ?? throw new InvalidOperationException(
        $"Failed to create {typeof(TTypedInt).FullName} from database value '{intValue}'."
      );
  }
}
