using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DigitalNewsPaperController : MonoBehaviour
{
    public void Close ()
    {
        GameInstance.HUD.EnableDigitalNewsPaper(false);
    }
}
