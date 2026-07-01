using UnityEngine;

public interface IBuildTool
{
    string GetMainCategoryName();
    string GetSubTypeName();
    void OnSelected();
    void OnErased(Vector2Int gridPos, GridManager gridManager);
    void OnPlaced(Vector2Int gridPos, GridManager gridManager);

    void CycleCategory();
    void CycleType();
}
