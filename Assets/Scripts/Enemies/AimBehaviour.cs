using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[RequireComponent(typeof(WeaponHolder))]
public class AimBehaviour : MonoBehaviour
{
    public LayerMask detectionMask;
    public int maxDetectionObjects = 20;
    public float detectionRadius;
    public string targetTag;

    private WeaponHolder holder;
    private void Start()
    {
        holder = GetComponent<WeaponHolder>();
        holder.AddWeapon("enemy_gun");

    }

    public void FireAtTarget(GameObject potentialTarget)
    {
        holder.SetAimingDir(potentialTarget.transform.position - transform.position);
        holder.FireWeapon();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

