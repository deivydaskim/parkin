namespace Parkin.Api.Domain.Services;

public interface IEntryDecisionService
{
  EntryDecision Decide(EntryDecisionContext context);
}
