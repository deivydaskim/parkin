namespace Parkin.Api.Web;

public interface ICurrentUser
{
  Guid? Id { get; }

  Guid RequiredId { get; }
}
