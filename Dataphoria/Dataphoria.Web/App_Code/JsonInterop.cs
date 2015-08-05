using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Alphora.Dataphor.DAE.NativeCLI;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.Dataphoria.Web
{
	public static class JsonInterop
	{
		public static Dictionary<string, object> JsonArgsToNative(JObject args)
		{
			// TODO: allow complex object to be passed

			if (args != null && args.Count > 0)
			{
				var result = new Dictionary<string, object>(args.Count);
				foreach (var p in args.Properties())
					result.Add(p.Name, ((JValue)p.Value).Value);
				return result;
			}
			else
				return null;
		}

		public static JToken NativeToJson(object result)
		{
			if (result == null)
				return new JValue((object)null);

			var resultType = result.GetType();

			return JToken.FromObject(result);
		}

        public static JToken NativeScalarValueToJson(NativeScalarValue value)
        {
            if (value == null)
                return new JValue((object)null);

            return JToken.FromObject(value.Value);
        }

        public static JToken NativeRowValueToJson(NativeRowValue value)
        {
            var result = new JObject();

            for (int i = 0; i < value.Columns.Length; i++)
            {
                result.Add(value.Columns[i].Name, NativeValueToJson(value.Values[i]));
            }

            return result;
        }

        public static JToken NativeListValueToJson(NativeListValue value)
        {
            var result = new JArray();

            for (int i = 0; i < value.Elements.Length; i++)
            {
                result.Add(NativeValueToJson(value.Elements[i]));
            }
            
            return result;
        }

        public static JToken UntypedNativeValueToJson(object value)
        {
            var row = value as NativeRowValue;
            if (row != null)
            {
                return NativeRowValueToJson(row);
            }

            var list = value as NativeListValue;
            if (list != null)
            {
                return NativeListValueToJson(list);
            }

            var table = value as NativeTableValue;
            if (table != null)
            {
                return NativeTableValueToJson(table);
            }

            return NativeToJson(value);
        }

        public static JToken NativeTableValueToJson(NativeTableValue value)
        {
            var result = new JArray();

            for (int r = 0; r < value.Rows.Length; r++)
            {
                var row = value.Rows[r];
                var resultRow = new JObject();

                for (int c = 0; c < value.Columns.Length; c++)
                {
                    resultRow.Add(value.Columns[c].Name, UntypedNativeValueToJson(row[c]));
                }

                result.Add(resultRow);
            }

            return result;
        }

        public static JToken NativeValueToJson(NativeValue value)
        {
            var scalar = value as NativeScalarValue;
            if (scalar != null)
            {
                return NativeScalarValueToJson(scalar);
            }

            var row = value as NativeRowValue;
            if (row != null)
            {
                return NativeRowValueToJson(row);
            }

            var list = value as NativeListValue;
            if (list != null)
            {
                return NativeListValueToJson(list);
            }

            var table = value as NativeTableValue;
            if (table != null)
            {
                return NativeTableValueToJson(table);
            }

			return null;
        }

        public static JToken NativeResultToJson(NativeResult result)
        {
            return NativeValueToJson(result.Value);
        }
	}
}