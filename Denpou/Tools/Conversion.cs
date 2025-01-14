﻿using System.Collections.Generic;
using System.Reflection;
using Denpou.Base;

namespace Denpou.Tools;

public static class Conversion
{
    public static void CustomConversionChecks(FormBase form, KeyValuePair<string, object> p, PropertyInfo f)
    {
        //Newtonsoft Int64/Int32 converter issue
        if (f.PropertyType == typeof(int))
        {
            var i = 0;
            if (int.TryParse(p.Value.ToString(), out i)) f.SetValue(form, i);
            return;
        }

        //Newtonsoft Double/Decimal converter issue
        if (f.PropertyType == typeof(decimal) || f.PropertyType == typeof(decimal?))
        {
            decimal d = 0;
            if (decimal.TryParse(p.Value.ToString(), out d)) f.SetValue(form, d);
        }
    }
}