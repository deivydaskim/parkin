namespace Parkin.Api.Configurations;

public class DatabaseOptions
{
  public const string SectionName = "DatabaseOptions";

  public bool RecreateOnStartup { get; set; }

  public bool SeedDemoData { get; set; }
}
