using System.Reflection;
using System.Text;

namespace SupportCenter.Extensions
{
    public static class ObjectExtensions
    {
        public static string ToStringCustom(this object obj, int indentLevel = 0)
        {
            if (obj == null)
            {
                return "null";
            }

            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var sb = new StringBuilder();
            var indent = new string(' ', indentLevel * 2);
            sb.AppendLine($"{indent}{type.Name} {{");

            foreach (var property in properties)
            {
                var value = property.GetValue(obj, null);
                if (value != null && !IsSimpleType(value.GetType()))
                {
                    sb.AppendLine($"{indent}{property.Name}:");
                    sb.Append(value.ToStringCustom(indentLevel + 1));
                }
                else
                {
                    sb.AppendLine($"{indent}{property.Name}: {value}");
                }
            }

            sb.AppendLine($"{indent}}}");
            return sb.ToString();
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid);
        }
    }
}
