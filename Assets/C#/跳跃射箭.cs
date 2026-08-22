using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 跳跃 + 鼠标瞄准 + 射箭（对应参考图：玩家跳起向目标射箭，HUD 显示箭矢数量 4 / 5）。
/// 挂到玩家(英雄)上即可。若玩家身上同时挂了 基础移动，请禁用 基础移动，
/// 否则它每帧写入 rb.linearVelocity，会把跳跃/射箭的速度覆盖掉。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class 跳跃射箭 : MonoBehaviour
{
    [Header("移动与跳跃")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float gravityScale = 1f;
    public LayerMask groundLayers = ~0;
    public Transform groundCheck;
    public float groundCheckDistance = 0.25f;

    [Header("瞄准与射击")]
    public Camera aimCamera;
    public Transform firePoint;
    public GameObject arrowPrefab;
    public Sprite arrowSprite;
    public Color arrowTint = Color.white;
    public float arrowSpeed = 14f;
    public float arrowDamage = 10f;
    public float arrowLifetime = 2.5f;
    public float fireCooldown = 0.35f;

    [Header("箭矢数量(对应图中 4/5)")]
    [Min(1)] public int maxAmmo = 5;
    public float ammoRefillDelay = 1.2f;
    public Text ammoText;

    [Header("动画参数(不存在会自动跳过)")]
    public string jumpTrigger = "Jump";
    public string shootTrigger = "Shoot";

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D playerCollider;
    private Vector2 aimDirection = Vector2.right;
    private int ammo;
    private float nextFireTime;
    private float nextRefillTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        aimCamera ??= Camera.main;
        firePoint ??= transform;
        rb.gravityScale = gravityScale;

        if (GetComponent<基础移动>() != null)
        {
            Debug.LogWarning($"[{name}] 检测到基础移动组件，两者都会写速度；请禁用基础移动，否则跳跃会被覆盖。", this);
        }
    }

    private void Start()
    {
        ammo = maxAmmo;
        EnsureAmmoText();
        RefreshAmmoView();
    }

    private void Update()
    {
        HandleAim();
        HandleJump();
        HandleShoot();
        RefillAmmo();
    }

    private void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;
        velocity.x = Input.GetAxisRaw("Horizontal") * moveSpeed;
        rb.linearVelocity = velocity;
    }

    private void HandleAim()
    {
        if (aimCamera == null)
        {
            return;
        }

        Vector3 mouseWorld = aimCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -aimCamera.transform.position.z));
        Vector2 direction = (Vector2)mouseWorld - (Vector2)transform.position;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        aimDirection = direction.normalized;
        if (sr != null && Mathf.Abs(aimDirection.x) > 0.1f)
        {
            sr.flipX = aimDirection.x < -0.001f;
        }
    }

    private void HandleJump()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || !IsGrounded())
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        PlayerAnimationPresenter.SetTriggerIfExists(anim, jumpTrigger);
    }

    private void HandleShoot()
    {
        if (!Input.GetMouseButtonDown(0) || Time.time < nextFireTime || ammo <= 0)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;
        FireArrow();
    }

    private void FireArrow()
    {
        ammo--;
        nextRefillTime = Time.time + ammoRefillDelay;
        RefreshAmmoView();

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Vector3 spawnPosition = firePoint != null
            ? firePoint.position
            : transform.position + (Vector3)(aimDirection * 0.8f);

        GameObject arrow;
        if (arrowPrefab != null)
        {
            arrow = Instantiate(arrowPrefab, spawnPosition, Quaternion.Euler(0f, 0f, angle));
        }
        else
        {
            arrow = BuildArrow();
            arrow.transform.position = spawnPosition;
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        PlayerAnimationPresenter.SetTriggerIfExists(anim, shootTrigger);

        Arrow mover = arrow.GetComponent<Arrow>();
        if (mover == null)
        {
            mover = arrow.AddComponent<Arrow>();
        }

        mover.Init(transform, aimDirection, arrowSpeed, arrowDamage, arrowLifetime);
    }

    private GameObject BuildArrow()
    {
        GameObject arrow = new GameObject("Arrow");

        SpriteRenderer renderer = arrow.AddComponent<SpriteRenderer>();
        renderer.sprite = arrowSprite != null ? arrowSprite : GeneratedArrowSprite();
        renderer.color = arrowTint;
        renderer.sortingOrder = 5;

        Rigidbody2D body = arrow.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D collider = arrow.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.12f;

        return arrow;
    }

    private bool IsGrounded()
    {
        Vector2 origin = transform.position;
        if (groundCheck != null)
        {
            origin = groundCheck.position;
        }
        else if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            origin = new Vector2(bounds.center.x, bounds.min.y);
        }
        else
        {
            origin += Vector2.down * 0.55f;
        }

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayers);
        if (hit.collider == null || hit.collider.isTrigger)
        {
            return false;
        }

        return hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform);
    }

    private void RefillAmmo()
    {
        if (ammo >= maxAmmo || Time.time < nextRefillTime)
        {
            return;
        }

        ammo++;
        nextRefillTime = Time.time + ammoRefillDelay;
        RefreshAmmoView();
    }

    private void EnsureAmmoText()
    {
        if (ammoText != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject textObject = new GameObject("AmmoText");
        textObject.transform.SetParent(canvas.transform, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(220f, 50f);

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        ammoText = text;
    }

    private void RefreshAmmoView()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{ammo} / {maxAmmo}";
        }
    }

    private static Sprite _generatedArrow;

    private static Sprite GeneratedArrowSprite()
    {
        if (_generatedArrow != null)
        {
            return _generatedArrow;
        }

        Texture2D texture = new Texture2D(16, 8, TextureFormat.RGBA32, false);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool shaft = x >= 6 && (y == 3 || y == 4);
                bool head = x < 6 && y >= 2 && y <= 5 && x >= 1;
                texture.SetPixel(x, y, shaft || head ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        _generatedArrow = Sprite.Create(texture, new Rect(0f, 0f, 16f, 8f), new Vector2(0.5f, 0.5f), 100f);
        _generatedArrow.name = "GeneratedArrow";
        return _generatedArrow;
    }

    /// <summary>箭矢行为：沿发射方向飞行，命中敌人造成伤害后销毁，超时自动销毁。</summary>
    public class Arrow : MonoBehaviour
    {
        private Vector2 velocity;
        private float damage;
        private float lifetime;
        private Transform owner;
        private Rigidbody2D body;

        public void Init(Transform owner, Vector2 direction, float speed, float damage, float lifetime)
        {
            this.owner = owner;
            velocity = direction.normalized * speed;
            this.damage = damage;
            this.lifetime = lifetime;

            body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.gravityScale = 0f;
                body.linearVelocity = velocity;
            }
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
            {
                return;
            }

            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                enemy.ReceiveDamage(damage, owner);
                Destroy(gameObject);
            }
        }
    }
}
