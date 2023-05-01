using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerInv : MonoBehaviour
{
    public InventoryObject inventory;
    public InventoryObject equipment;
 
    public void OnTriggerEnter2D(Collider2D other)
    {
            Debug.Log("item picked up");
            var item = other.GetComponent<GroundItem>();
            if (item)
            {
                //inventory.AddItem(item.item.CreateItem(), 1);
                if (inventory.AddItem(new Item(item.item), 1))
                    Destroy(other.gameObject);
            }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inventory.Save();
            equipment.Save();
        }
        else if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            inventory.Load();
            equipment.Load();
        }
    }

    public void OnApplicationQuit()
    {
        inventory.Clear();
        equipment.Clear();
    }
}