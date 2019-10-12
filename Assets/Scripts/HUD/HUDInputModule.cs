#pragma warning disable 414, 0649

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using CurvedHUD;

[ExecuteInEditMode]
public class UIInputModule : StandaloneInputModule
{
    //SETTINGS-------------------------------------------------//
    #region --- Fields ---
   
    public static UIInputModule Singleton { get; private set; }

    [SerializeField]
    string submitButtonName = "Fire1";

    //Gaze
    [SerializeField]
    bool gazeUseTimedClick = false;
    [SerializeField]
    float gazeClickTimer = 2.0f;
    [SerializeField]
    float gazeClickTimerDelay = 1.0f;
    [SerializeField]
    Image gazeTimedClickProgressImage;

    //World Space Mouse
    [SerializeField]
    float worldSpaceMouseSensitivity = 1;

    //hidden
    static bool disableOtherInputModulesOnStart = true; //default true


    //Support Variables - common
    static UIInputModule instance;
    GameObject currentDragging;
    GameObject currentPointedAt;

    //Support Variables - gaze
    float gazeTimerProgress;

    //Support variables - custom ray
    Ray customControllerRay;

    //support variables - other
    float dragThreshold = 10.0f;
    bool pressedDown = false;
    bool pressedLastFrame = false;

    protected override void Awake()
    {
        if (!Application.isPlaying) return;

        Singleton = this;
        base.Awake();

    }

    protected override void Start()
    {
        if (!Application.isPlaying) return;

        base.Start();
    }

    /// <summary>
    /// Sends trigger down / trigger released events to gameobjects under the pointer.
    /// </summary>
    protected virtual void ProcessDownRelease(PointerEventData eventData, bool down, bool released)
    {
        var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

        // PointerDown notification
        if (down)
        {
            eventData.eligibleForClick = true;
            eventData.delta = Vector2.zero;
            eventData.dragging = false;
            eventData.useDragThreshold = true;
            eventData.pressPosition = eventData.position;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, eventData);

            if (eventData.pointerEnter != currentOverGo)
            {
                // send a pointer enter to the touched element if it isn't the one to select...
                HandlePointerExitAndEnter(eventData, currentOverGo);
                eventData.pointerEnter = currentOverGo;
            }

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);


            float time = Time.unscaledTime;

            if (newPressed == eventData.lastPress)
            {
                var diffTime = time - eventData.clickTime;
                if (diffTime < 0.3f)
                    ++eventData.clickCount;
                else
                    eventData.clickCount = 1;

                eventData.clickTime = time;
            }
            else
            {
                eventData.clickCount = 1;
            }

            eventData.pointerPress = newPressed;
            eventData.rawPointerPress = currentOverGo;

            eventData.clickTime = time;

            // Save the drag handler as well
            eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (eventData.pointerDrag != null)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
        }

        // PointerUp notification
        if (released)
        {
            // Debug.Log("Executing pressup on: " + pointer.pointerPress);
            ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

            // see if we mouse up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (eventData.pointerPress == pointerUpHandler && eventData.eligibleForClick)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
            }
            else if (eventData.pointerDrag != null && eventData.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.dropHandler);
            }

            eventData.eligibleForClick = false;
            eventData.pointerPress = null;
            eventData.rawPointerPress = null;

            if (eventData.pointerDrag != null && eventData.dragging)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

            eventData.dragging = false;
            eventData.pointerDrag = null;

            if (eventData.pointerDrag != null)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

            eventData.pointerDrag = null;

            // send exit events as we need to simulate this on touch up on touch device
            ExecuteEvents.ExecuteHierarchy(eventData.pointerEnter, eventData, ExecuteEvents.pointerExitHandler);
            eventData.pointerEnter = null;
        }
    }

    #endregion
}
