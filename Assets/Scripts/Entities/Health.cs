using System;
using UnityEngine;
public class Health : MonoBehaviour
{
    public int maxHealth;
    private int currentHealth;
    public int CurrentHealth => currentHealth;
    public bool HasDied { get; private set; }

    public event Action<GameObject> onDeathEvent;
    public event Action<GameObject> onDamageTakenEvent;


    private void Awake()
    {
        currentHealth = maxHealth;
    }
    public void Revive()
    {
        HasDied = false;
        currentHealth = maxHealth;
    }
    public void OnDamageTaken(int damage)
    {
        currentHealth -= damage;
        onDamageTakenEvent?.Invoke(gameObject);
        if (currentHealth <= 0)
        {
            OnDeath(gameObject);
            HasDied = true;
        }
    }

    public void OnDeath(GameObject owner)
    {
        onDeathEvent?.Invoke(owner);
    }
}

