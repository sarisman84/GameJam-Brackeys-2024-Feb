using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    enum GameplayState
    {
        Intro,
        MainMenu,
        InGame,
        Paused
    }

    private GameplayState currentState;

    private void Awake()
    {
        currentState = GameplayState.Intro;
    }



}

