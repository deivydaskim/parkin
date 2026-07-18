namespace Parkin.Api.Domain.DriverAggregate;

public static class PlateNormalizer
{
  public static string Normalize(string rawPlate)
    => rawPlate.Trim().ToUpperInvariant().Replace(" ", "");
}
