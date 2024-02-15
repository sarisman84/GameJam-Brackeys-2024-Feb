using System;
using UnityEngine;
public class Health : MonoBehaviour
{
    public int maxHealth;
    private int currentHealth;

    public bool HasDied { get; private set; }

    public event Action<GameObject> onDeathEvent;

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

