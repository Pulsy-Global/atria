using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Data.Entities.Outputs.Config;

public class PostgresOutputConfig : OutputConfigBase
{
    public override OutputType OutputType => OutputType.Postgres;

    public string ConnectionString { get; set; }

    public string TableName { get; set; }

    public string Schema { get; set; } = "public";

    public Dictionary<string, string> ColumnMappings { get; set; } = new();

    public bool CreateTableIfNotExists { get; set; } = true;

    public int BatchSize { get; set; } = 1000;

    public int TimeoutSeconds { get; set; } = 30;
}
