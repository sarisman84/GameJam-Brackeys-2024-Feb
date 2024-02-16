﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class NextState<TEnum> where TEnum : Enum
{
    public TEnum nextState;
    public IEnumerator yieldCommand;

    public NextState(TEnum state, IEnumerator yield)
    {
        nextState = state;
        yieldCommand = yield;
    }
}

public class StateMachine<TEnum> where TEnum : Enum
{
    private Dictionary<TEnum, Func<NextState<TEnum>>> stateMachine;
    private TEnum currentState;
    private bool runStateMachine;

    public TEnum CurrentState => currentState;
    public NextState<TEnum> GetEvent(TEnum state) => stateMachine[state]();
    public StateMachine(Dictionary<TEnum, Func<NextState<TEnum>>> stateMachine, TEnum startingState)
    {
        this.stateMachine = stateMachine;
        currentState = startingState;
    }

    public bool Running => runStateMachine;

    public void Start(MonoBehaviour owner)
    {
        runStateMachine = true;
        owner.StartCoroutine(Update());
    }

    public void Stop(MonoBehaviour owner)
    {
        runStateMachine = false;
        owner.StopCoroutine(Update());
    }

    public void SetState(TEnum newState)
    {
        currentState = newState;
    }

    private IEnumerator Update()
    {
        while (Running)
        {
            if (!stateMachine.ContainsKey(currentState))
            {
                runStateMachine = false;
                yield break;
            }

            var call = stateMachine[currentState];
            var result = call();
            currentState = result.nextState;
            yield return result.yieldCommand;
        }

    }
}


public class StateMachine<TEnum, TNextState>
    where TEnum : Enum
    where TNextState : NextState<TEnum>
{
    private Dictionary<TEnum, Func<TNextState>> stateMachine;
    private TEnum currentState;
    private bool runStateMachine;

    public TEnum CurrentState => currentState;
    public TNextState GetEvent(TEnum state) => stateMachine[state]();

    public StateMachine(Dictionary<TEnum, Func<TNextState>> stateMachine, TEnum startingState)
    {
        this.stateMachine = stateMachine;
        currentState = startingState;
    }

    public bool Running => runStateMachine;

    public void Start(MonoBehaviour owner)
    {
        runStateMachine = true;
        owner.StartCoroutine(Update());
    }

    public void Stop(MonoBehaviour owner)
    {
        runStateMachine = false;
        owner.StopCoroutine(Update());
    }

    public void SetState(TEnum newState)
    {
        currentState = newState;
    }

    private IEnumerator Update()
    {
        while (Running)
        {
            if (!stateMachine.ContainsKey(currentState))
            {
                runStateMachine = false;
                yield break;
            }

            var call = stateMachine[currentState];
            var result = call();
            currentState = result.nextState;
            yield return result.yieldCommand;
        }

    }
}

public static class StateMachine
{
    public static IEnumerator WaitForSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    public static IEnumerator WaitUntil(Func<bool> pred)
    {
        yield return new WaitUntil(pred);
    }
}

