using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;
using Parkin.Api.Domain.ParkingSessionAggregate;
using Parkin.Api.Domain.ParkingSessionAggregate.Specifications;
using Parkin.Api.Domain.Services;

namespace Parkin.Api.OccupancyFeatures.GetLotOccupancy;

public record GetLotOccupancyQuery(ParkingLotId LotId) : IQuery<Result<LotOccupancyDto>>;

// All the arithmetic lives in OccupancyCalculator; this handler only materializes its inputs.
// Unlike ingestion, it takes no lot lock - a polled view tolerates a slightly stale count.
public class GetLotOccupancyHandler(
  IReadRepository<ParkingLot> lotRepository,
  IReadRepository<ParkingSession> sessionRepository,
  IOccupancyCalculator occupancyCalculator)
  : IQueryHandler<GetLotOccupancyQuery, Result<LotOccupancyDto>>
{
  public async ValueTask<Result<LotOccupancyDto>> Handle(
    GetLotOccupancyQuery request, CancellationToken cancellationToken)
  {
    var lot = await lotRepository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot is null) return Result.NotFound();

    var generalUsed = await sessionRepository.CountAsync(
      new ActiveSessionCountByLotPoolSpec(lot.Id, SessionPool.General), cancellationToken);
    var reservedOccupied = await sessionRepository.CountAsync(
      new ActiveSessionCountByLotPoolSpec(lot.Id, SessionPool.Reserved), cancellationToken);

    var occupancy = occupancyCalculator.Calculate(
      OccupancyContext.Create(lot.Capacity, generalUsed, reservedOccupied));

    var reservedSpaceCount = lot.Spaces.Count(space =>
      space.Status == SpaceStatus.Active && space.Type == SpaceType.Reserved);

    return new LotOccupancyDto(
      lot.Id,
      occupancy.GeneralCapacity,
      occupancy.GeneralUsed,
      occupancy.GeneralFree,
      occupancy.IsGeneralPoolFull,
      occupancy.IsOverCapacity,
      reservedSpaceCount,
      occupancy.ReservedCount,
      DateTimeOffset.UtcNow);
  }
}
