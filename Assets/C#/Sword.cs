using UnityEngine;
using UnityEngine.Tilemaps;

public class Sword : MonoBehaviour
{
    //public Tilemap map;
    // colider 
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
        }

    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
        }

    }
}
