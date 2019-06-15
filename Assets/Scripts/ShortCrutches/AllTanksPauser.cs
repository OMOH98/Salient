using UnityEngine;

public class AllTanksPauser : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Pause"))
            Tank.TogglePauseAll();
    }
}
