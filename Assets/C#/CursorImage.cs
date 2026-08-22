using UnityEngine;
using UnityEngine.UI;

public class CursorImage : MonoBehaviour
{
    //[SerializeField]
    public Image cursorImage;

    void Awake()
    {
        if (cursorImage == null)
            cursorImage = GetComponent<Image>();
    }

    void Start()
    {
    }

    void Update()
    {
        transform.position = Input.mousePosition;
        if (cursorImage != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                cursorImage.color = Color.red;
                transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                cursorImage.color = Color.white;
                transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
    }
}
