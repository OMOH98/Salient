﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TankSpawner))]
public class TankSpawnerUI : MonoBehaviour
{
    public TMP_Dropdown scriptChoise;
    public TMP_Dropdown sideChoise;
    public Button add;

    TankSpawner spawner;
    // Start is called before the first frame update
    void Start()
    {
        spawner = GetComponent<TankSpawner>();
        EditedTank.PopulateSavedScriptDropdown(scriptChoise);
        sideChoise.options.Clear();
        foreach (var item in spawner.spawnpoints)
        {
            sideChoise.options.Add(new TMP_Dropdown.OptionData(item.sideId.ToString()));
        }
        sideChoise.value = -1;
        sideChoise.value = 0;

        add.onClick.AddListener(() =>
        {
            spawner.Spawn(int.Parse(sideChoise.options[sideChoise.value].text), EditedTank.LoadScript(scriptChoise.options[scriptChoise.value].text, new Tank.DummyLogger()));
        });
    }

}