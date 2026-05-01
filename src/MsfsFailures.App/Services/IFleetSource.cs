using MsfsFailures.App.ViewModels;

namespace MsfsFailures.App.Services;

public interface IFleetSource
{
    IReadOnlyList<AirframeVm> GetAirframes();
    IReadOnlyList<SquawkVm> GetSquawks();
}
