using UnityEngine;
using UnityEngine.Tilemaps;

public class GameOjectHide : MonoBehaviour
{
    public Tilemap map;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (map != null && collision != null && collision.tag == "Player")
        {
            map.color = new Color(1, 1, 1, 0.89f);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (map != null && collision != null && collision.tag == "Player")
        {
            map.color = new Color(1, 1, 1, 1f);
        }
    }
}
