namespace Parkin.Api.Infrastructure.Data.Queries;

internal static class LikePattern
{
  public static string Containing(string term) =>
    "%" + term.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
}
