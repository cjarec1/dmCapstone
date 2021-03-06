﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649
/// <summary>
/// Handles menu navigation in our Title Scene
/// </summary>
public class TitleUIManager : UIManager
{
    public static HostJoinRoomMenu HostJoinRoomMenu { get; set; }
    public static RoomSessionMenu RoomSessionMenu { get; set; }

    private static TitleUIManager Instance;

    [SerializeField] private AudioSource buttonAudio;
    [SerializeField] private HostJoinRoomMenu hostJoinRoomMenu;
    [SerializeField] private RoomSessionMenu roomSessionMenu;
    
    protected override void Start()
    {
        base.Start();

        if (Instance != null)
        {
            Debug.LogError("Multiple TitleUIManager objects exist.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HostJoinRoomMenu = hostJoinRoomMenu;
        RoomSessionMenu = roomSessionMenu;
        NetworkWrapper.currentScene = NetworkWrapper.Scene.MainMenu;
    }

    public static void PlayButtonSound()
    {
        Instance.buttonAudio.Play();
    }
    
    public static void Navigate_HostJoinRoomMenu()
    {
        Instance.Navigate(Instance.hostJoinRoomMenu);
    }

    public static void Navigate_RoomSessionMenu()
    {
        Instance.Navigate(Instance.roomSessionMenu);
    }

    public void TEST_OtherMenuClicked()
    {
        if (Instance.hostJoinRoomMenu.gameObject.activeInHierarchy)
        {
            Navigate_RoomSessionMenu();
        }
        else
        {
            Navigate_HostJoinRoomMenu();
        }
    }
}
#pragma warning restore 0649