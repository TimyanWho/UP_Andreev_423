using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace UP_Andreev_423
{
    public partial class App : Application
    {
    }

    public static class DbUtil
    {
        public static object Get(object obj, params string[] names)
        {
            if (obj == null) return null;

            foreach (var name in names)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                    return prop.GetValue(obj);
            }

            return null;
        }

        public static string Str(object obj, params string[] names)
        {
            return Get(obj, names)?.ToString() ?? string.Empty;
        }

        public static int Int(object obj, params string[] names)
        {
            var value = Get(obj, names);
            if (value == null) return 0;

            try { return Convert.ToInt32(value); }
            catch { return 0; }
        }

        public static bool Bool(object obj, params string[] names)
        {
            var value = Get(obj, names);
            if (value == null) return false;

            if (value is bool b) return b;
            if (bool.TryParse(value.ToString(), out bool parsedBool)) return parsedBool;
            if (int.TryParse(value.ToString(), out int parsedInt)) return parsedInt != 0;
            return false;
        }

        public static void Set(object obj, object value, params string[] names)
        {
            if (obj == null) return;

            foreach (var name in names)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        if (value == null)
                        {
                            prop.SetValue(obj, null);
                            return;
                        }

                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        prop.SetValue(obj, Convert.ChangeType(value, targetType));
                        return;
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static IEnumerable Items(object obj, params string[] names)
        {
            var value = Get(obj, names);
            return value as IEnumerable;
        }
    }
}