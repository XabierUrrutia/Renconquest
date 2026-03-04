using System;

public static class GameEvents
{
    public static event Action<int> OnUnitsSelected;
    public static void RaiseUnitsSelected(int unitCount) => OnUnitsSelected?.Invoke(unitCount);

    public static event Action<int, int> OnUnitsMoveCommand;
    public static void RaiseUnitsMoveCommand(int infantryCount, int tankCount) => OnUnitsMoveCommand?.Invoke(infantryCount, tankCount);

    public static event Action<int, int> OnUnitsAttackCommand;
    public static void RaiseUnitsAttackCommand(int infantryCount, int tankCount) => OnUnitsAttackCommand?.Invoke(infantryCount, tankCount);

    public static event Action<int, int, int> OnUnitEasterEgg;
    public static void RaiseUnitEasterEgg(int eggIndex, int infantryCount, int tankCount)=> OnUnitEasterEgg?.Invoke(eggIndex, infantryCount, tankCount);

    public static event Action OnUnitUnderAttack;
    public static void RaiseUnitUnderAttack() => OnUnitUnderAttack?.Invoke();

    public static event Action OnUnitUpgraded;
    public static void RaiseUnitUpgraded() => OnUnitUpgraded?.Invoke();

    public static event Action OnBaseUnderAttack;
    public static void RaiseBaseUnderAttack() => OnBaseUnderAttack?.Invoke();

    public static event Action OnBuildingSelected;
    public static void RaiseBuildingSelected() => OnBuildingSelected?.Invoke();

    public static event Action OnBuildingCaptured;
    public static void RaiseBuildingCaptured() => OnBuildingCaptured?.Invoke();

    public static event Action OnBuildingLost;
    public static void RaiseBuildingLost() => OnBuildingLost?.Invoke();

    public static event Action OnInsufficientResources;
    public static void RaiseInsufficientResources() => OnInsufficientResources?.Invoke();

    public static event Action OnInvalidCommand;
    public static void RaiseInvalidCommand() => OnInvalidCommand?.Invoke();

    public static event Action OnLowResources;
    public static void RaiseLowResources() => OnLowResources?.Invoke();

    public static event Action OnLowPower;
    public static void RaiseLowPower() => OnLowPower?.Invoke();

    public static event Action OnBuildingCaptureStarted;
    public static void RaiseBuildingCaptureStarted() => OnBuildingCaptureStarted?.Invoke();

    public static event Action OnBuildingCaptureCompleted;
    public static void RaiseBuildingCaptureCompleted() => OnBuildingCaptureCompleted?.Invoke();

    public static event Action OnBuildingCaptureFailed;
    public static void RaiseBuildingCaptureFailed() => OnBuildingCaptureFailed?.Invoke();

    public static event Action OnMedikitPickedUp;
    public static void RaiseMedikitPickedUp() => OnMedikitPickedUp?.Invoke();

    public static event Action OnTechLevelUp;
    public static void RaiseTechLevelUp() => OnTechLevelUp?.Invoke();
}
