using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Empowers UI that allows spawning tanks using <see cref="TankSpawner"/>
/// </summary>
[RequireComponent(typeof(TankSpawner))]
public class TankSpawnerUI : MonoBehaviour
{
    public Dropdown scriptChoise;
    public Dropdown sideChoise;
    public Button add;

    TankSpawner spawner;
    // Start is called before the first frame update
    void Start()
    {
        spawner = GetComponent<TankSpawner>();
        EditedTank.PopulateSavedScriptDropdown(scriptChoise);
        EditedTank.toRepopulate.Add(scriptChoise);
        sideChoise.options.Clear();
        foreach (var item in spawner.spawnpoints)
        {
            sideChoise.options.Add(new Dropdown.OptionData(item.sideId.ToString()));
        }
        sideChoise.value = -1;
        sideChoise.value = 0;

        add.onClick.AddListener(() =>
        {
            var code = EditedTank.LoadScript(scriptChoise.options[scriptChoise.value].text, new Tank.DummyLogger());
            if (!string.IsNullOrEmpty(code))
                spawner.Spawn(int.Parse(sideChoise.options[sideChoise.value].text), code);
        });
    }

}
