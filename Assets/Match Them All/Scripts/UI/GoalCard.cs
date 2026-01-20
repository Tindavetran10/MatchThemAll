using System;
using TMPro;
using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    public class GoalCard : MonoBehaviour
    {
       [Header(" Elements ")]
       [SerializeField] private TextMeshProUGUI amountText;

       private void Start()
       {
           
       }

       private void Update()
       {
           
       }

       public void Configure(int initialAmount)
       {
           amountText.text = initialAmount.ToString();
       }

       public void UpdateAmount(int amount)
       {
           amountText.text = amount.ToString();
       }

       public void Complete()
       {
           gameObject.SetActive(false);
       }
    }
}