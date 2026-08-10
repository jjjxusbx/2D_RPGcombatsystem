using UnityEngine;

public class Test : MonoBehaviour
{
    //public void OnMovePerformend(InputAction.CallbackContext context)
    //{
    //    移动动作 = context.ReadValue<Vector2>();
    //}

    /*轴映射
       
      //Debug.Log("h:" + 水平 + "v:" + 上下);
    */
    public Rigidbody2D rb;
    public Animation am;
    public SpriteRenderer sr;
    public Transform pos1, pos2;
    public float 移动速度 = 5f;
    public Transform targetTransform  ;//

    private void Start()
    {
        //Debug.Log("Hw");
        targetTransform = pos1;
    }
    //
    private void Update()
    {
        
    }


}
