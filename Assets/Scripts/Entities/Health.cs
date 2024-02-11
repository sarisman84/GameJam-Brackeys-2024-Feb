using System;
using UnityEngine;
public class Health : MonoBehaviour
{
    public int maxHealth;
    private int currentHealth;

    public event Action<GameObject> onDeathEvent;

    private void Awake()
    {
        currentHealth = maxHealth;
    }
    public void OnDamageTaken(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            OnDeath(gameObject);
        }
    }

    public void OnDeath(GameObject owner)
    {
        onDeathEvent?.Invoke(owner);
    }
}

