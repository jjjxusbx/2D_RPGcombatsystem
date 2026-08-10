using UnityEngine;
using UnityEngine.Tilemaps;

public class GameoOjectHide : MonoBehaviour
{
    public Tilemap map;
    // colider 
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            map.color = new Color(1, 1, 1, 0.89f);
        }

    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            map.color = new Color(1, 1, 1, 1f);
        }

    }

}
