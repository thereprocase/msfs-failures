namespace MsfsFailures.App.ViewModels;

public sealed record LiveStateVm(
    string Phase,
    int Altitude,
    int Ias,
    int Gs,
    int Hdg,
    int Oat,
    int FuelKg,
    IReadOnlyList<double> N1,
    IReadOnlyList<int> Itt,
    IReadOnlyList<int> Torque,
    IReadOnlyList<int> OilTemp,
    IReadOnlyList<int> OilPress,
    string Gear,
    int Flaps,
    double HobbsStart,
    double CurrentHobbs)
{
    public double HobbsDelta => CurrentHobbs - HobbsStart;
}
