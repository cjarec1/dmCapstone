﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Controls the UI in the object named "Host-Join Room Menu".
/// All button clicks etc. are handled by this script.
/// </summary>
public class HostJoinRoomMenu : NavigationMenu
{
#pragma warning disable CS0649
    [SerializeField] private GameObject roomButtonTemplate;
#pragma warning restore CS0649

    private List<GameObject> roomButtons = new List<GameObject>();
    private Coroutine listenRoutine;

    public override void NavigateTo()
    {
        base.NavigateTo();
        listenRoutine = StartCoroutine(TryListen());
    }

    public override void NavigateFrom()
    {
        StopCoroutine(listenRoutine);
        NetworkWrapper.discovery.StopListening();
        base.NavigateFrom();
    }

    private IEnumerator TryListen()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (!NetworkWrapper.discovery.running)
            {
                NetworkWrapper.discovery.ListenForLANServers();
            }
        }
    }

    /// <summary>
    /// Adds a room to the list of available rooms
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public GameObject AddRoom(string text)
    {
        GameObject btn = Instantiate(roomButtonTemplate, roomButtonTemplate.transform.parent);

        Text txt = btn.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = text;
        }

        btn.SetActive(true);
        roomButtons.Add(btn);

        return btn;
    }

    /// <summary>
    /// Removes this room from our list when it is no longer discovered on the network
    /// </summary>
    /// <param name="btn"></param>
    public void RemoveRoom(GameObject btn)
    {
        roomButtons.Remove(btn);
        Destroy(btn);
    }

    /// <summary>
    /// Button click method (set in the inspector) for buttons on the list of rooms
    /// </summary>
    /// <param name="buttonClicked"></param>
    public void RoomSelected(GameObject buttonClicked)
    {
        Text t = buttonClicked.GetComponentInChildren<Text>();
        if (t != null)
        {
            string ipAddress = NetworkWrapper.discovery.GetAddressOfRoom(t.text);
            if (ipAddress != null)
            {
                NetworkWrapper.ConnectToServer(ipAddress);
                NavigateToRoomSessionMenu(t.text);
            }
            else
            {
                Debug.LogErrorFormat("No room of name {0}", t.text);
            }
        }
        else
        {
            Debug.LogError("Cannot get name of room selected.");
        }
    }

    /// <summary>
    /// Button click method for "Create Room"
    /// </summary>
    /// <param name="roomName">Textbox for the name of the room</param>
    public void CreateRoomButtonClicked(InputField roomName)
    {
        if (roomName.text.Length == 0)
            return;

        NetworkWrapper.StartServer(roomName.text);
        NavigateToRoomSessionMenu(roomName.text);
    }

    private void NavigateToRoomSessionMenu(string roomName)
    {
        TitleUIManager.RoomSessionMenu.roomName = roomName;
        TitleUIManager.Navigate_RoomSessionMenu();
    }
}
