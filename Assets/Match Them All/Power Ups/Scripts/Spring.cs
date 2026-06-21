using System;
using UnityEngine;

namespace Match_Them_All.Scripts.Power_Ups
{
    public class Spring : Powerup
    {
        public Animator animator;
        [Header("Actions")] public static Action Started;
        
        private void TriggerPowerupStart() => Started?.Invoke();

        public void Play() => animator.Play("Activate");
    }
}
