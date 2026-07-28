using UnityEngine;

/*
 * Adding a new Category:
 * Add it to BuildingCategory
 * Add the SpecialBuildingType(s) (Ensure everything is a/has a Building script!)
 * Update OnPlaced()
 * Update CycleType()
 * Update ResetTypeSelection();
 * Update GetCurrentBuildPrice();
 */

public class RoadTool : IBuildTool
{
    public string GetMainCategoryName() => "Road Building";
    public string GetSubTypeName() => ""; //Empty

    public void OnSelected() { }
    public void OnErased(Vector2Int gridPos, GridManager gridManager)
    {
        gridManager.eraseRoadElement(gridPos);
    }
    public void OnPlaced(Vector2Int gridPos, GridManager gridManager)
    {
        gridManager.createRoadOnGrid(gridPos, GameManager.instance.freeGridExpansion);
    }
    public void CycleCategory() { }
    public void CycleType() { }
}

public enum SpecialBuildingTypes
{
    WaterTower,
    CoalStation,
    NuclearStation,

    Fire,
    Police,
    Hospital,

    Turret
}
public class ZoningTool : IBuildTool
{
    private ZoneType currentZone = ZoneType.Residential;

    public string GetMainCategoryName() => "Zoning";
    public string GetSubTypeName() => currentZone.ToString();

    public void OnSelected() { }
    public void OnErased(Vector2Int gridPos, GridManager gridManager)
    {
        gridManager.removeZoneFromGrid(gridPos);
    }
    public void OnPlaced(Vector2Int gridPos, GridManager gridManager)
    {
        gridManager.zoneTileOnGrid(gridPos, currentZone);
    }
    public void CycleCategory() {
        int totalZones = System.Enum.GetValues(typeof(ZoneType)).Length;

        int nextZone = (int)currentZone + 1;
        if (nextZone >= totalZones)
        {
            nextZone = 1; //Skip none
        }

        currentZone = (ZoneType)nextZone;
    }
    public void CycleType() { }
}

public class BuildingTool : IBuildTool
{
    private enum BuildingCategory
    {
        Utilities,
        Emergency,
        Military
    }

    private BuildingCategory activeCategory = BuildingCategory.Utilities;

    private SpecialBuildingTypes activeBuilding = SpecialBuildingTypes.WaterTower;

    public string GetMainCategoryName() => $"Buildings {activeCategory}";
    public string GetSubTypeName() => activeBuilding.ToString();

    public void OnSelected() => ResetTypeSelection();

    public void OnErased(Vector2Int gridPos, GridManager gridManager)
    {
        GridPlayerManager.instance.buildingSpecialFx?.Invoke(gridPos);
    }

    public void OnPlaced(Vector2Int gridPos, GridManager gridManager)
    {
        GameObject prefabToPlace = null;

        switch (activeBuilding)
        {
            //Utilities
            case SpecialBuildingTypes.WaterTower:
                prefabToPlace = GridPlayerManager.instance.waterTowerPrefab;
                break;
            case SpecialBuildingTypes.CoalStation:
                prefabToPlace = GridPlayerManager.instance.coalPowerStationPrefab;
                break;
            case SpecialBuildingTypes.NuclearStation:
                prefabToPlace = GridPlayerManager.instance.nuclearPowerStationPrefab;
                break;

            //Emergency
            case SpecialBuildingTypes.Fire:
                prefabToPlace = GridPlayerManager.instance.fireStationPrefab;
                break;
            case SpecialBuildingTypes.Police:
                prefabToPlace = GridPlayerManager.instance.policeStationPrefab;
                break;
            case SpecialBuildingTypes.Hospital:
                prefabToPlace = GridPlayerManager.instance.hospitalPrefab;
                break;


            //Military
            case SpecialBuildingTypes.Turret:
                prefabToPlace = MilitaryManager.instance.turretPrefab;
                break;
            default:
                break;
        }

        int buildCost = GetCurrentBuildPrice();

        if (prefabToPlace == null) { Debug.LogError("Couldn't find a prefab to place!"); return; }
        if (FinanceManager.instance == null) { Debug.LogError("Couldn't find Finance Manager!"); return; }

        if (FinanceManager.instance.currentMoney < buildCost)
        {
            GameManager.instance.UserNotification?.Invoke("Not enough money!", true);
            return;
        }

        if (GridManager.instance.createSpecialBuildingOnGrid(gridPos, prefabToPlace))
        {
            GameManager.instance.GainExperience(50);

            FinanceManager.instance.Purchase(buildCost);
            return;
        }

        GameManager.instance.UserNotification?.Invoke("Cannot place building here!", true);
    }
    public void CycleCategory()
    {
        activeCategory = (BuildingCategory)(((int)activeCategory + 1) % System.Enum.GetValues(typeof(BuildingCategory)).Length);
        ResetTypeSelection();
    }
    public void CycleType() { 
        switch (activeCategory)
        {
            case BuildingCategory.Utilities:
                switch (activeBuilding)
                {
                    case SpecialBuildingTypes.WaterTower:
                        activeBuilding = SpecialBuildingTypes.CoalStation;
                        break;
                    case SpecialBuildingTypes.CoalStation:
                        activeBuilding = SpecialBuildingTypes.NuclearStation;
                        break;
                    default:
                        activeBuilding = SpecialBuildingTypes.WaterTower;
                        break;
                }
                break;

            case BuildingCategory.Emergency:
                switch (activeBuilding)
                {
                    case SpecialBuildingTypes.Hospital:
                        activeBuilding = SpecialBuildingTypes.Police;
                        break;
                    case SpecialBuildingTypes.Police:
                        activeBuilding = SpecialBuildingTypes.Fire;
                        break;
                    default:
                        activeBuilding = SpecialBuildingTypes.Hospital;
                        break;
                }
                break;

            case BuildingCategory.Military:
                switch (activeBuilding)
                {
                    case SpecialBuildingTypes.Turret:
                        activeBuilding = SpecialBuildingTypes.Turret;
                        break;
                    default:
                        activeBuilding = SpecialBuildingTypes.Turret;
                        break;
                }
                break;

            default:
                activeCategory = BuildingCategory.Utilities;
                Debug.LogError("How did we get here? Invalid activeCategory");
                break;
        }
    }

    private void ResetTypeSelection()
    {
        switch (activeCategory)
        {
            case BuildingCategory.Utilities: activeBuilding = SpecialBuildingTypes.WaterTower; break;
            case BuildingCategory.Emergency: activeBuilding = SpecialBuildingTypes.Hospital; break;
            case BuildingCategory.Military: activeBuilding = SpecialBuildingTypes.Turret; break;
            default: Debug.LogError("How did we get here? Invalid activeCategory ResetTypeSelection()"); break;
        }
    }

    public int GetCurrentBuildPrice()
    {
        switch (activeBuilding)
        {
            //Utilities
            case SpecialBuildingTypes.WaterTower:
                return 3000;
            case SpecialBuildingTypes.CoalStation:
                return 80_000;
            case SpecialBuildingTypes.NuclearStation:
                return 80_000;//fix when add later

            //Emergency
            case SpecialBuildingTypes.Fire:
                return 80_000;
            case SpecialBuildingTypes.Police:
                return 40_000;
            case SpecialBuildingTypes.Hospital:
                return 50_000;


            //Military
            case SpecialBuildingTypes.Turret:
                return 150_000;
            default:
                return 0;
        }
    }
}