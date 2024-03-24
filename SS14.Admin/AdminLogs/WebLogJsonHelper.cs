using Dapper;
using System.Data;
using System.Text.Json;

namespace SS14.Admin.AdminLogs
{
    public class WebLogJsonHelper
    {
        public class JsonTypeHandler : SqlMapper.TypeHandler<JsonDocument>
        {
            public override JsonDocument Parse(object value)
            {
                if (value is string json)
                {
                    return JsonDocument.Parse(json);
                }

                return default;
            }

            public override void SetValue(IDbDataParameter parameter, JsonDocument? value)
            {
                parameter.Value = value?.ToString();
            }
        }
    }
}
