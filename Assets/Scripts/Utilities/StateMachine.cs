using System;
using System.Collections;
using System.Collections.Generic;
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
    private Coroutine routine;
    private MonoBehaviour owner;
    public event Func<NextState<TEnum>, IEnumerator> onStateChange;

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
        this.owner = owner;
        runStateMachine = true;
        routine = this.owner.StartCoroutine(Update());
    }

    public void Stop(MonoBehaviour owner)
    {
        this.owner = owner;
        runStateMachine = false;
        this.owner.StopCoroutine(routine);
    }

    public void SetState(TEnum newState)
    {
        currentState = newState;
        if (routine != null)
        {
            owner.StopCoroutine(routine);
            routine = null;
        }
        routine = owner.StartCoroutine(Update());
    }

    private IEnumerator Update()
    {
        while (Running)
        {
            if (!stateMachine.ContainsKey(currentState))
            {
                Debug.LogWarning("Could not find state! Exiting!");
                runStateMachine = false;
                yield break;
            }

            var call = stateMachine[currentState];
            var result = call();
            currentState = result.nextState;
            yield return result.yieldCommand;
        }

        Debug.LogError("LOOP EXITED!");

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


