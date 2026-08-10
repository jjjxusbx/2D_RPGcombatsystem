using UnityEngine;

public class WeaponPivot : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
		Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 direction = mousePos - transform.position;// 
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;// 

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
