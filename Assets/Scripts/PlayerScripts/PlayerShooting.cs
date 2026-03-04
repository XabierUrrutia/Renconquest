using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;
    public Transform weaponPoint;
    public float fireRate = 1.0f;
    public int maxAmmo = 10;
    public string weaponName = "G3 RIFLE";

    [Header("Bullet")]
    public float bulletSpeed = 10f;
    public int bulletDamage = 1;

    [Header("Auto-Aim Settings")]
    public float detectionRange = 8f;
    public LayerMask enemyLayerMask;
    public bool autoAimEnabled = true;
    public float aimUpdateRate = 0.2f;

    [Header("Weapon Behavior")]
    public bool burstMode = false;
    public int burstCount = 3;
    public float burstDelay = 0.2f;
    public float accuracy = 0.95f;

    [Header("UI")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI weaponNameText;

    [Header("Range Visualization")]
    public bool showRangeInGame = true;
    public Color rangeColor = new Color(1f, 1f, 0f, 0.3f);
    public Color targetLineColor = Color.red;

    [Header("Sprite Settings - Derecha")]
    public Sprite idleSpriteRight;
    public Sprite aimingSpriteRight;
    public Sprite shootingSpriteRight;
    public Sprite reloadingSpriteRight;

    [Header("Sprite Settings - Izquierda")]
    public Sprite idleSpriteLeft;
    public Sprite aimingSpriteLeft;
    public Sprite shootingSpriteLeft;
    public Sprite reloadingSpriteLeft;

    [Header("Animation Timing")]
    public SpriteRenderer soldierSpriteRenderer;
    public float shootingSpriteDuration = 0.3f;
    public float aimingSpriteDuration = 0.2f;
    public float shootingDelay = 0.1f;

    // --- CAMBIO: Variable para controlar el tiempo de recarga ---
    public float reloadTime = 2.0f;
    // -----------------------------------------------------------

    public bool flipSpriteBasedOnDirection = true;

    // Dirección actual (1 = derecha, -1 = izquierda)
    private int currentDirection = 1;

    public int currentAmmo;
    private float nextFireTime;
    private Transform currentTarget;
    private Camera mainCam;
    private Coroutine aimCoroutine;
    private bool isBurstFiring = false;
    private LineRenderer rangeCircle;
    private LineRenderer targetLine;

    // forced target API
    private Transform forcedTarget = null;

    // Sprite state management
    private Coroutine spriteChangeCoroutine;
    private Coroutine shootingCoroutine;
    private bool isAiming = false;
    private bool isReloading = false;
    private bool isShowingShootingSprite = false;

    private UnitVeterancy myVeterancy;
    private Transform lastForcedTargetVoice = null;


    public void SetForcedTarget(Transform target)
    {
        forcedTarget = target;
        if (forcedTarget == null) lastForcedTargetVoice = null;
        
        if (forcedTarget != null)
        {
            currentTarget = forcedTarget;
            UpdateDirectionToTarget();
            StartAiming();

            if (forcedTarget != lastForcedTargetVoice)
            {
                PlayAttackVoiceForThisUnit();
                lastForcedTargetVoice = forcedTarget;
            }
        }

        if (aimCoroutine == null)
        {
            aimCoroutine = StartCoroutine(UpdateAimTarget());
        }
    }

    public void ClearForcedTarget()
    {
        forcedTarget = null;
        lastForcedTargetVoice = null;

        if (!autoAimEnabled)
        {
            currentTarget = null;
            if (aimCoroutine != null)
            {
                StopCoroutine(aimCoroutine);
                aimCoroutine = null;
            }

            if (targetLine != null)
                targetLine.enabled = false;

            StopAiming();
        }
    }
    void PlayAttackVoiceForThisUnit()
    {
        if (SoundColector.Instance == null) return;

        int id = gameObject.GetInstanceID();
        SoundColector.Instance.SetVoiceContextUnit(id);
        var gender = SoundColector.Instance.GetOrAssignLockedGenderForUnit(id);

        SoundColector.Instance.PlayUnitAttackVoice(gender);
    }

    void Start()
    {
        currentAmmo = maxAmmo;
        mainCam = Camera.main;

        myVeterancy = GetComponent<UnitVeterancy>();

        if (weaponPoint == null)
        {
            CreateWeaponPoint();
        }

        if (soldierSpriteRenderer == null)
        {
            soldierSpriteRenderer = GetComponent<SpriteRenderer>();
            if (soldierSpriteRenderer == null)
            {
                soldierSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        // Establecer sprite inicial
        if (soldierSpriteRenderer != null && idleSpriteRight != null)
        {
            soldierSpriteRenderer.sprite = idleSpriteRight;
            currentDirection = 1;
        }

        UpdateUI();

        if (showRangeInGame)
        {
            CreateRangeVisualization();
            CreateTargetLine();
        }

        if (autoAimEnabled || forcedTarget != null)
        {
            aimCoroutine = StartCoroutine(UpdateAimTarget());
        }
    }

    void CreateWeaponPoint()
    {
        GameObject weaponPointObj = new GameObject("WeaponPoint");
        weaponPointObj.transform.SetParent(transform);
        weaponPointObj.transform.localPosition = new Vector3(0.5f, 0.2f, 0);
        weaponPoint = weaponPointObj.transform;
    }

    void Update()
    {
        HandleShooting();

        if (targetLine != null)
        {
            if (currentTarget != null && (autoAimEnabled || forcedTarget != null))
            {
                targetLine.enabled = true;
                targetLine.SetPosition(0, weaponPoint.position);
                targetLine.SetPosition(1, currentTarget.position);

                if (!isAiming && !isReloading && !isShowingShootingSprite)
                {
                    StartAiming();
                }
            }
            else
            {
                targetLine.enabled = false;
                if (!isShowingShootingSprite && !isReloading)
                {
                    StopAiming();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleAutoAim();
        }
    }

    void UpdateDirectionToTarget()
    {
        if (currentTarget == null) return;

        Vector3 direction = currentTarget.position - transform.position;

        if (flipSpriteBasedOnDirection)
        {
            if (direction.x < 0)
            {
                soldierSpriteRenderer.flipX = true;
                currentDirection = -1;
            }
            else
            {
                soldierSpriteRenderer.flipX = false;
                currentDirection = 1;
            }
        }
        else
        {
            if (direction.x < 0) currentDirection = -1;
            else currentDirection = 1;
        }
    }

    void UpdateDirectionToMouse()
    {
        if (mainCam == null) return;

        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector3 direction = mouseWorld - transform.position;

        if (flipSpriteBasedOnDirection)
        {
            if (direction.x < 0)
            {
                soldierSpriteRenderer.flipX = true;
                currentDirection = -1;
            }
            else
            {
                soldierSpriteRenderer.flipX = false;
                currentDirection = 1;
            }
        }
        else
        {
            if (direction.x < 0) currentDirection = -1;
            else currentDirection = 1;
        }
    }

    Sprite GetSpriteForCurrentState()
    {
        if (isReloading)
        {
            return currentDirection > 0 ? reloadingSpriteRight : reloadingSpriteLeft;
        }

        if (isAiming && !isShowingShootingSprite)
        {
            return currentDirection > 0 ? aimingSpriteRight : aimingSpriteLeft;
        }

        return currentDirection > 0 ? idleSpriteRight : idleSpriteLeft;
    }

    Sprite GetShootingSprite()
    {
        return currentDirection > 0 ? shootingSpriteRight : shootingSpriteLeft;
    }

    void StartAiming()
    {
        if (isAiming || isReloading || soldierSpriteRenderer == null || isShowingShootingSprite)
            return;

        isAiming = true;

        if (currentTarget != null)
        {
            UpdateDirectionToTarget();
        }

        if (spriteChangeCoroutine != null)
            StopCoroutine(spriteChangeCoroutine);

        spriteChangeCoroutine = StartCoroutine(ShowAimingSprite());
    }

    void StopAiming()
    {
        if (!isAiming || isReloading || soldierSpriteRenderer == null || isShowingShootingSprite)
            return;

        isAiming = false;

        if (spriteChangeCoroutine != null)
            StopCoroutine(spriteChangeCoroutine);

        soldierSpriteRenderer.sprite = GetSpriteForCurrentState();
    }

    IEnumerator ShowAimingSprite()
    {
        soldierSpriteRenderer.sprite = GetSpriteForCurrentState();

        while (isAiming && !isShowingShootingSprite && !isReloading)
        {
            soldierSpriteRenderer.sprite = GetSpriteForCurrentState();
            yield return null;
        }
    }

    IEnumerator ShootingAnimation(Vector3 targetPosition, bool playSound = true)
    {
        isShowingShootingSprite = true;
        soldierSpriteRenderer.sprite = GetShootingSprite();
        yield return new WaitForSeconds(shootingDelay);

        PerformShoot(targetPosition, playSound);

        yield return new WaitForSeconds(shootingSpriteDuration - shootingDelay);
        isShowingShootingSprite = false;

        if (isReloading)
        {
            soldierSpriteRenderer.sprite = GetSpriteForCurrentState();
        }
        else if (isAiming)
        {
            soldierSpriteRenderer.sprite = GetSpriteForCurrentState();
            if (spriteChangeCoroutine != null)
                StopCoroutine(spriteChangeCoroutine);
            spriteChangeCoroutine = StartCoroutine(ShowAimingSprite());
        }
        else
        {
            soldierSpriteRenderer.sprite = GetSpriteForCurrentState();
        }
    }

    IEnumerator ShowReloadingSprite()
    {
        isReloading = true;

        // Detenemos animaciones de apuntado si las hubiera
        if (spriteChangeCoroutine != null) StopCoroutine(spriteChangeCoroutine);

        float timer = 0f;

        while (timer < reloadTime)
        {
            // 1. Permitimos que el soldado se gire mientras recarga
            if (autoAimEnabled || forcedTarget != null)
            {
                if (currentTarget != null) UpdateDirectionToTarget();
            }
            else
            {
                UpdateDirectionToMouse();
            }

            // 2. FORZAMOS el sprite de recarga según la dirección actual
            // Esto asegura que si te giras, el sprite cambia al lado correcto
            // y evita que se ponga el Idle por error.
            if (currentDirection > 0)
                soldierSpriteRenderer.sprite = reloadingSpriteRight;
            else
                soldierSpriteRenderer.sprite = reloadingSpriteLeft;

            // 3. Contamos el tiempo
            timer += Time.deltaTime;
            yield return null; // Esperamos al siguiente frame
        }

        isReloading = false;

        // Al terminar, decidimos qué sprite poner (Apuntar o Idle)
        if (isAiming)
        {
            if (spriteChangeCoroutine != null) StopCoroutine(spriteChangeCoroutine);
            spriteChangeCoroutine = StartCoroutine(ShowAimingSprite());
        }
        else
        {
            soldierSpriteRenderer.sprite = GetSpriteForCurrentState();
        }
    }

    void CreateRangeVisualization()
    {
        GameObject rangeObject = new GameObject("RangeVisualization");
        rangeObject.transform.SetParent(transform);
        rangeObject.transform.localPosition = Vector3.zero;

        rangeCircle = rangeObject.AddComponent<LineRenderer>();
        rangeCircle.material = new Material(Shader.Find("Sprites/Default"));
        rangeCircle.startColor = rangeColor;
        rangeCircle.endColor = rangeColor;
        rangeCircle.startWidth = 0.05f;
        rangeCircle.endWidth = 0.05f;
        rangeCircle.useWorldSpace = false;
        rangeCircle.loop = true;

        DrawCircle(rangeCircle, detectionRange, 50);
    }

    void CreateTargetLine()
    {
        GameObject lineObject = new GameObject("TargetLine");
        lineObject.transform.SetParent(transform);

        targetLine = lineObject.AddComponent<LineRenderer>();
        targetLine.material = new Material(Shader.Find("Sprites/Default"));
        targetLine.startColor = targetLineColor;
        targetLine.endColor = targetLineColor;
        targetLine.startWidth = 0.03f;
        targetLine.endWidth = 0.03f;
        targetLine.useWorldSpace = true;
        targetLine.positionCount = 2;
        targetLine.enabled = false;
    }

    void DrawCircle(LineRenderer lineRenderer, float radius, int segments)
    {
        lineRenderer.positionCount = segments + 1;
        float angle = 0f;
        for (int i = 0; i < segments + 1; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += 360f / segments;
        }
    }

    void HandleShooting()
    {
        // Si ya está recargando, no hace nada (sale de la función)
        if (isReloading || isShowingShootingSprite) return;

        // --- CAMBIO: RECARGA AUTOMÁTICA ---
        // Si no tenemos balas, iniciamos la recarga inmediatamente y salimos
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }
        // ----------------------------------

        if (autoAimEnabled || forcedTarget != null)
        {
            if (currentTarget != null && Time.time >= nextFireTime && currentAmmo > 0 && !isBurstFiring)
            {
                UpdateDirectionToTarget();
                if (burstMode)
                {
                    StartCoroutine(BurstFire(currentTarget.position));
                }
                else
                {
                    if (shootingCoroutine != null) StopCoroutine(shootingCoroutine);
                    shootingCoroutine = StartCoroutine(ShootingAnimation(currentTarget.position));
                    nextFireTime = Time.time + fireRate;
                }
            }
        }
        else
        {
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime && currentAmmo > 0 && !isBurstFiring)
            {
                Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = 0;
                UpdateDirectionToMouse();

                if (!isAiming) StartAiming();

                if (burstMode)
                {
                    StartCoroutine(BurstFire(mouseWorld));
                }
                else
                {
                    if (shootingCoroutine != null) StopCoroutine(shootingCoroutine);
                    shootingCoroutine = StartCoroutine(ShootingAnimation(mouseWorld));
                    nextFireTime = Time.time + fireRate;
                }
            }
        }

        // Recarga Manual con R
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        // Esta corrutina llama a ShowReloadingSprite, que es quien contiene la espera
        yield return StartCoroutine(ShowReloadingSprite());

        // Cuando termine la espera de ShowReloadingSprite, rellenamos balas
        currentAmmo = maxAmmo;
        UpdateUI();
        Debug.Log("¡Recarga completada!");
    }

    void PerformShoot(Vector3 targetPosition, bool playSound = true)
    {
        if (bulletPrefab == null || weaponPoint == null)
        {
            Debug.LogError("Faltan referencias: bulletPrefab o weaponPoint");
            return;
        }

        Vector2 direction = (targetPosition - weaponPoint.position).normalized;

        if (accuracy < 1.0f)
        {
            float inaccuracy = (1.0f - accuracy) * 2.0f;
            direction.x += Random.Range(-inaccuracy, inaccuracy);
            direction.y += Random.Range(-inaccuracy, inaccuracy);
            direction.Normalize();
        }

        GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(direction);
            b.isEnemyBullet = false;
            b.damage = bulletDamage;
            b.speed = bulletSpeed;

            if (myVeterancy != null)
            {
                b.ownerVeterancy = myVeterancy;
            }
        }
        else
        {
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direction * bulletSpeed;
            }
        }
        
        if (playSound && SoundColector.Instance != null)
        {
            Vector3 pos = transform.position;

            bool isTankShooter =
            GetComponentInParent<TankShooting>() != null ||
            GetComponentInParent<TankVisuals>() != null;

            if (isTankShooter)
                SoundColector.Instance.PlayTankShotAt(pos);
            else
                SoundColector.Instance.PlayInfantryShotAt(pos);
        }

        currentAmmo--;
        UpdateUI();
    }

    IEnumerator BurstFire(Vector3 targetPosition)
    {
        isBurstFiring = true;
        int shotsFired = 0;
        while (shotsFired < burstCount && currentAmmo > 0)
        {
            bool playSound = true;
            if (shootingCoroutine != null) StopCoroutine(shootingCoroutine);

            shootingCoroutine = StartCoroutine(ShootingAnimation(targetPosition, playSound));
            shotsFired++;

            if (shotsFired < burstCount)
            {
                yield return new WaitForSeconds(burstDelay + shootingSpriteDuration);
            }
        }
        isBurstFiring = false;
        nextFireTime = Time.time + fireRate;
    }

    IEnumerator UpdateAimTarget()
    {
        while (true)
        {
            if (forcedTarget != null)
            {
                if (forcedTarget.gameObject == null)
                {
                    forcedTarget = null;
                    lastForcedTargetVoice = null;
                    currentTarget = null;
                    StopAiming();
                }
                else
                {
                    currentTarget = forcedTarget;
                    UpdateDirectionToTarget();
                    if (!isAiming && !isReloading && !isShowingShootingSprite)
                        StartAiming();
                    yield return new WaitForSeconds(aimUpdateRate);
                    continue;
                }
            }

            FindNearestEnemy();
            yield return new WaitForSeconds(aimUpdateRate);
        }
    }

    void FindNearestEnemy()
    {
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayerMask);
        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider2D enemyCollider in enemiesInRange)
        {
            if (enemyCollider.CompareTag("Enemy"))
            {
                // Raycast para verificar visión
                // NOTA: Asegúrate de tener la LAYER 'Obstacle' creada o esto podría fallar si el nombre no existe
                int layerMask = enemyLayerMask | (1 << LayerMask.NameToLayer("Obstacle"));

                RaycastHit2D hit = Physics2D.Raycast(
                    transform.position,
                    enemyCollider.transform.position - transform.position,
                    detectionRange,
                    layerMask
                );

                if (hit.collider != null && hit.collider.CompareTag("Enemy"))
                {
                    float distance = Vector2.Distance(transform.position, enemyCollider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemyCollider.transform;
                    }
                }
            }
        }

        currentTarget = nearestEnemy;

        if (currentTarget != null && !isAiming && !isReloading && !isShowingShootingSprite)
        {
            UpdateDirectionToTarget();
            StartAiming();
        }
        else if (currentTarget == null && isAiming && forcedTarget == null && !isShowingShootingSprite)
        {
            StopAiming();
        }
    }

    void ToggleAutoAim()
    {
        autoAimEnabled = !autoAimEnabled;
        if (autoAimEnabled)
        {
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            aimCoroutine = StartCoroutine(UpdateAimTarget());
            Debug.Log("Auto-aim ACTIVADO");
        }
        else
        {
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            currentTarget = null;
            if (targetLine != null) targetLine.enabled = false;
            StopAiming();
            Debug.Log("Auto-aim DESACTIVADO - Modo manual");
        }
    }

    void UpdateUI()
    {
        if (ammoText != null) ammoText.text = $"{currentAmmo}/{maxAmmo}";
        if (weaponNameText != null) weaponNameText.text = weaponName;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (currentTarget != null && autoAimEnabled && weaponPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(weaponPoint.position, currentTarget.position);
        }

        if (weaponPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(weaponPoint.position, 0.1f);
        }
    }

    void OnDestroy()
    {
        if (aimCoroutine != null) StopCoroutine(aimCoroutine);
        if (spriteChangeCoroutine != null) StopCoroutine(spriteChangeCoroutine);
        if (shootingCoroutine != null) StopCoroutine(shootingCoroutine);
    }
}