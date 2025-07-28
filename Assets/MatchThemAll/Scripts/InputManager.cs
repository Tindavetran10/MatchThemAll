using System;
using MatchThemAll.Scripts;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static Action<Item> ItemClicked;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
            HandleMouseDown();    
    }

    private void HandleMouseDown()
    {
        Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100);
        
        if(hit.collider == null)
            return;
        
        if(!hit.collider.TryGetComponent(out Item item))
            return;
        
        Debug.Log("Hit" + hit.collider.name);
        
        ItemClicked?.Invoke(item);
    }
}
