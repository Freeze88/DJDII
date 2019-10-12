using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockController : MonoBehaviour
{
    public delegate void EventHandler(LockController sender);

    public event EventHandler OnUnlock;

    [Header("Bobby Pin")]
    public GameObject bobbyPin;
    [SerializeField]
    private Vector2 maxMinRotation = Vector2.zero;
    [Header("Lock")]
    public GameObject lockPick;
    [Header("Buttons")]
    public GameObject quitButton;
    [Header("Sound FX")]
    public AudioSource enterSFX;
    public AudioClip[] lockpickMovements = new AudioClip[0];
    public AudioSource lockpickMovement;
    public AudioSource unlockSFX;
    public AudioSource bobbyPinBreak;
  
    private Animator bobbyPinAnimator;
    private bool picking = false;
    private bool active = false;

    private float Tolerance { get; set; }
    private float BobbyPinAngle { get; set; } = 0;
    private float Angle { get; set; }
    private float Damage { get; set; }
    public Interactable Interactable { get; set; }
    public PlayerController PlayerController { get; set; }

    private void Start()
    {
        bobbyPinAnimator = bobbyPin.GetComponent<Animator>();

        OnUnlock += (LockController sender) =>
        {
            if (quitButton != null)
                quitButton.SetActive(false);

            unlockSFX.Play();

            Interactable.Locked = false;
            StartCoroutine(SwitchToLootInventory());
        };
    }

    private IEnumerator SwitchToLootInventory()
    {
        active = false;

        yield return new WaitForSecondsRealtime(2f);

        gameObject.SetActive(false);

        if (Interactable is LootInteractable)
            GameInstance.HUD.EnableObjectInventory((LootInteractable)Interactable, PlayerController);
        else
            GameInstance.GameState.Paused = false;
    }

    public void Initialize()
    {
        if (quitButton != null)
            quitButton.SetActive(true);

        Angle = Random.Range(maxMinRotation.x, maxMinRotation.y);
        Tolerance = 1f;
        Damage = 0;
        active = true;

        if (lockPick != null)
            lockPick.transform.localRotation = Quaternion.identity;

        if (bobbyPin != null)
            bobbyPin.transform.localRotation = Quaternion.identity;
    }

    public void PlayEnterSound ()
    {
        if (enterSFX != null)
            enterSFX.Play();
    }

    public void Close ()
    {
        active = false;

        GameInstance.GameState.Paused = false;

        gameObject.SetActive(false);
    }

    private float ClampAngle(float currentValue)
    {
        float angle = currentValue - 180;

        while (angle < 0)
            angle += 360;

        angle = Mathf.Repeat(angle, 360);
        return Mathf.Clamp(angle - 180, maxMinRotation.x, maxMinRotation.y) + 360;
    }

    private void UpdateBobbyPin ()
    {
        if (picking)
            return;

        Vector3 lookAt = Input.mousePosition;
        float lastBobbyPinAngle = BobbyPinAngle; 
        BobbyPinAngle = ClampAngle(Mathf.Atan2(lookAt.y - bobbyPin.transform.position.y, lookAt.x - bobbyPin.transform.position.x) * Mathf.Rad2Deg - 90f);
        bobbyPin.transform.localRotation = Quaternion.Euler(0f, 0f, BobbyPinAngle);

        float d = Random.Range(0, 100);
        if (d > 30 && !lockpickMovement.isPlaying && Mathf.Abs(BobbyPinAngle - lastBobbyPinAngle) > 2f)
        {
            int randomClip = Random.Range(0, lockpickMovements.Length);
            lockpickMovement.clip = lockpickMovements[randomClip];
            lockpickMovement.Play();
        }
    }

    private void UpdateLock ()
    {
        float angle;

        if (!picking)
        {
            angle = Mathf.LerpAngle(lockPick.transform.localEulerAngles.z, 0, Time.unscaledDeltaTime * 8f);
            lockPick.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            return;
        }

        float distance = Mathf.Abs (Angle - (BobbyPinAngle - 360));
        float maxDistance = Mathf.Max(Mathf.Abs (90 + Angle), Mathf.Abs(90 - Angle));
        float influence = Mathf.Abs (3f + 90f * (1 - distance / maxDistance));
        angle = Mathf.LerpAngle(lockPick.transform.localEulerAngles.z, influence, Time.unscaledDeltaTime * 4f);
        lockPick.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (lockPick.transform.localEulerAngles.z > 90 - Tolerance)
            OnUnlock?.Invoke(this);
        else if (Mathf.Abs(angle - influence) < 2f)
        {
            lockPick.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 5f);
            Damage += 0.2f;
        }

        if (Damage > 3)
        {
            active = false;
            bobbyPin.SetActive(false);
            bobbyPinBreak.Play();
            StartCoroutine(Reset());
        }
    }

    private IEnumerator Reset ()
    {
        yield return new WaitForSecondsRealtime(1f);

        bobbyPin.SetActive(true);
        active = true;
        Damage = 0;
    }

    private void Update()
    {
        bobbyPinAnimator.SetFloat("Damage", Damage);

        if (!active)
            return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            Close();
            return;
        }

        picking = Input.GetKey(KeyCode.Space);

        UpdateBobbyPin();

        UpdateLock();
    }
}
