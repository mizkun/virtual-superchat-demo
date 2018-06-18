using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandControllerInput : MonoBehaviour
{
    public SteamVR_TrackedObject trackedObj;
    public SteamVR_Controller.Device device;
    public GameObject player;

    //Walking
    public Transform playerCam;
    public float moveSpeed = 4f;
    private Vector3 movementDirection;

    // Use this for initialization
    void Start()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
    }

    // Update is called once per frame
    void Update()
    {
        device = SteamVR_Controller.Input((int)trackedObj.index);
        if (device.GetPress(SteamVR_Controller.ButtonMask.Touchpad))
        {
            movementDirection = playerCam.transform.forward;
            movementDirection = new Vector3(movementDirection.x,
                                             0,
                                             movementDirection.z);
            movementDirection *= moveSpeed * Time.deltaTime;
            player.transform.position += movementDirection;
        }
    }
}