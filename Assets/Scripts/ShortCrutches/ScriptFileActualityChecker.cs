using UnityEngine;
using UnityEngine.UI;

using System.Collections;

/// <summary>
/// In <see cref="Dropdown.onValueChanged"/> checks wether selected script file exists in file system.
/// </summary>
[RequireComponent(typeof(Dropdown))]
public class ScriptFileActualityChecker : MonoBehaviour
{
    Dropdown dropdown;
    Tank.DummyLogger logger;
    // Use this for initialization
    void Start()
    {
        dropdown = GetComponent<Dropdown>();
        logger = new Tank.DummyLogger();
        dropdown.onValueChanged.AddListener((inx) =>
        {
            var rn = dropdown.options[inx].text;
            EditedTank.LoadScript(rn, logger);
        });
    }
}
