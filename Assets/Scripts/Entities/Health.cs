using System;
using UnityEngine;
public class Health : MonoBehaviour
{
    public int maxHealth;
    private int currentHealth;
    public int CurrentHealth => currentHealth;
    public bool HasDied { get; private set; }

    public event Action onDeathEvent;
    public event Action onDamageTakenEvent;
    public event Action onHealRecievedEvent;
    public event Action onRevivedEvent;


    private void Awake()
    {
        currentHealth = maxHealth;
    }
    public void Revive()
    {
        HasDied = false;
        currentHealth = maxHealth;
        onRevivedEvent?.Invoke();
    }
    public void OnDamageTaken(int damage)
    {
        currentHealth -= damage;
        onDamageTakenEvent?.Invoke();
        if (currentHealth <= 0)
        {
            OnDeath();
            HasDied = true;
        }
    }

    public void OnDeath()
    {
        onDeathEvent?.Invoke();
    }

    public void Heal(int amount)
    {
        PFXManager.SpawnFX("healeffect", transform.position, Quaternion.identity);
        onHealRecievedEvent?.Invoke();
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }
}

