using UnityEngine;
// µÐÈË¹¥»÷´¥·¢Æ÷
public class SlimeATKTri : MonoBehaviour
{
    [SerializeField] private int damage = 5;
    public Transform owner;
    private void Start()
    {
        Destroy(gameObject, 0.3f);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<»ù´¡ÒÆ¶¯>().TakeDamage(damage,owner);
        }
    }
    public void SetDamage(int Num,Transform Owner)
    {
        damage = Num;
        owner = Owner;
    }
    
}
