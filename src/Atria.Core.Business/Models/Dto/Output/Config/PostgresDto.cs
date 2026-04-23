using Atria.Core.Business.Mapper.Attributes;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Outputs.Config;

namespace Atria.Core.Business.Models.Dto.Output.Config;

[ConfigMapping(OutputType.Postgres, typeof(PostgresOutputConfig), typeof(PostgresDto))]
public class PostgresDto : ConfigBaseDto
{
    public string ConnectionString { get; set; }

    public string TableName { get; set; }

    public string Schema { get; set; } = "public";

    public Dictionary<string, string> ColumnMappings { get; set; } = new();

    public bool CreateTableIfNotExists { get; set; } = true;

    public int BatchSize { get; set; } = 1000;

    public int TimeoutSeconds { get; set; } = 30;
}
