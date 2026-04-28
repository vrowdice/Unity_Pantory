using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainBlueprintSavedBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _label;

    private List<PlacedBuildingSaveData> _buildings;

    public IReadOnlyList<PlacedBuildingSaveData> Buildings => _buildings;

    public void Initialize(List<PlacedBuildingSaveData> buildings)
    {
        _buildings = buildings;
        if (_label != null)
            _label.text = buildings.Count.ToString();
    }
}
