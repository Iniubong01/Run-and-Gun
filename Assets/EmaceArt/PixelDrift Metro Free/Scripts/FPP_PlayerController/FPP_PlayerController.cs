using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace PlayerControllers
{
    [RequireComponent(typeof(CharacterController))]
    public class FPP_PlayerController : MonoBehaviour
    {
        // -- Movement
        public float walkSpeed = 3f;
        public float crouchSpeed = 1.5f;
        public float gravity = -12f;

        // -- Jump
        public bool canJump = false;
        public float jumpHeight = 1.2f;

        [Header("Mobile Movement")]
        public Joystick movementJoystick;

        public float crouchTransitionSpeed = 10f;

        [Header("View (Camera)")]
        public Transform cameraTransform;
        public float mouseSensitivity = 2f;
        public float maxLookAngle = 80f;
        public bool invertMouseY = false;

        [Header("Third Person Mode (TPP)")]
        public bool isThirdPerson = false;
        public Vector3 tppOffset = new Vector3(0f, 1.8f, -3.0f);
        public KeyCode viewToggleKey = KeyCode.V;

        [Header("Head Bobbing (FPP only)")]
        public bool headBobEnabled = true;
        public float bobFrequency = 1.8f;
        public float bobAmplitude = 0.04f;

        private CharacterController _cc;
        private float _verticalVelocity;
        private float _cameraPitch;
        private float _bobTimer;
        private Vector3 _cameraLocalOrigin;
        private float _defaultHeight;
        private bool _isCrouching;

        private Animator animator;

        // -- Ammunition Handling / Shooting
        public float ammoCount = 5;
        public float clipCount = 25;
        public int health = 100;
        private int defaultHealth;

        public float damage = 20, shootRange = 50, impactForce = 30f;

        public bool canShoot = true, isReloading = false, ammoExhausted = false, isDead;

        public GameObject hitEffect, explosionEffect, thirdPersonViewImage, firstPersonViewImage;

        public ParticleSystem noozleFlash, healingFx;

        public Transform particleParent;

        private bool criticalShown;

        // -- UI
        public Slider healthSlider;
        public TextMeshProUGUI ammoText, clipText;

        // -- Reference
        private CrosshairController ch_controller;

        [Header("Item Effects")]
        [SerializeField] private int healthItemRestore = 300;
        [SerializeField] private int ammoItemRestore = 1;
        [SerializeField] private int maxHealth = 100;

        [Header("Torch")]
        [SerializeField] private Button torchButton;
        [SerializeField] private Image torchFill;
        [SerializeField] private GameObject torchObject;
        public bool pauseTorchRoutine;

        private bool isTorchOn;
        private bool isCoolingDown;

        private Coroutine torchCoroutine;
        private Coroutine torchRefillCoroutine;

        private float torchRemainingTime;
        
        [Header("Teleport")]
        [SerializeField] private Button teleportButton;
        [SerializeField] private Image teleportFill;

        // TELEPORT POWERUP
        [Header("Teleport Powerup")]
        public bool canTeleport = true;
        public bool isTeleporting = false;

        public float teleportDistance = 5f;
        public float teleportDuration = 0.5f;
        public float teleportCooldown = 3f;

        [Tooltip("Assign the character model, NOT the player root.")]
        public Transform teleportVisual;

        [Tooltip("Optional teleport particles.")]
        public ParticleSystem teleportOutFX;
        public ParticleSystem teleportInFX;


        [Tooltip("Layers that should block teleporting.")]
        public LayerMask teleportObstacleLayers = ~0;

        public float teleportWallPadding = 0.5f;

        private Vector3 _teleportVisualOriginalScale;

        // Original player states for pooling
        private Vector3 originalPlayerPosition;
        private Quaternion originalPlayerRotation;
        private Vector3 originalCharacterCenter;

        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;

        private float originalAmmoCount;
        private float originalClipCount;

        private bool originalThirdPersonMode;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();

            // Cache original player position
            originalPlayerPosition = transform.position;
            originalPlayerRotation = transform.rotation;

            originalCharacterCenter = _cc.center;

            originalAmmoCount = ammoCount;
            originalClipCount = clipCount;

            originalThirdPersonMode = isThirdPerson;

            if (cameraTransform != null)
            {
                originalCameraPosition = cameraTransform.localPosition;
                originalCameraRotation = cameraTransform.localRotation;
            }

            _defaultHeight = _cc.height;

            GameObject crosshairObject = GameObject.Find("CrossHair");

            if (crosshairObject != null)
                ch_controller = crosshairObject.GetComponent<CrosshairController>();

            if (cameraTransform != null)
                _cameraLocalOrigin = cameraTransform.localPosition;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (animator == null)
                animator = GetComponent<Animator>();

            defaultHealth = health;
            maxHealth = Mathf.Max(maxHealth, defaultHealth);

            health = Mathf.Clamp(health, 0, maxHealth);

            if (teleportVisual != null)
                _teleportVisualOriginalScale = teleportVisual.localScale;

            UpdateTexts();
        }

        void Start()
        {
            healthSlider = GameObject.Find("Health Slider").GetComponent<Slider>();
            movementJoystick = GameObject.Find("Floating Joystick").GetComponent<FloatingJoystick>();
            UpdateHealthSlider();
            ammoText = GameObject.Find("Ammo Text").GetComponent<TextMeshProUGUI>();
            clipText = GameObject.Find("Clip Text").GetComponent<TextMeshProUGUI>();

            // Torch Initialization
            if (torchFill != null)
                torchFill.fillAmount = 1f;

            if (UI_Manager.Instance != null)
            {
                torchRemainingTime = UI_Manager.Instance.torchDuration;

                if (UI_Manager.Instance.torchSlider != null)
                    UI_Manager.Instance.torchSlider.value = 1f;
            }

            if (torchButton != null)
            {
                torchButton.onClick.RemoveListener(ToggleTorch);
                torchButton.onClick.AddListener(ToggleTorch);
            }

            if (torchObject != null)
                torchObject.SetActive(false);

            // TELEPORT BUTTON
            if (teleportButton != null)
            {
                teleportButton.onClick.RemoveListener(UseTeleport);
                teleportButton.onClick.AddListener(UseTeleport);
            }

            UpdateTeleportUI();
        }

        public void UseTeleport()
        {
            Debug.Log("Teleport button pressed.");

            if (isDead)
            {
                Debug.LogWarning("Teleport failed: Player is dead.");
                return;
            }

            if (isTeleporting)
            {
                Debug.LogWarning("Teleport failed: Player is already teleporting.");
                return;
            }

            if (!canTeleport)
            {
                Debug.LogWarning("Teleport failed: Teleport is on cooldown.");
                return;
            }

            if (UI_Manager.Instance.teleportCharge < 0.2f)
            {
                return;
            }

            ActivateTeleport();
        }

        #region Update
        void Update()
        {
            if (isDead)
                return;

            // Teleporting
            if (isTeleporting)
            {
                UpdateHealthSlider();
                return;
            }

            HandleCursorLock();
            HandleViewToggle();
            HandleMouseLook();
            HandleMovement();
            UpdateCameraView();
            // HandleShooting();

            UpdateHealthSlider();
            UpdateCrosshairTarget();
        }
        #endregion

        public void HandleViewToggle()
        {
            if (Input.GetKeyDown(viewToggleKey))
            {
                ToggleView();
            }
        }

        // -- CAMERA VIEW TOGGLE
        public void ToggleView()
        {
            isThirdPerson = !isThirdPerson;
            thirdPersonViewImage.SetActive(!isThirdPerson);
            firstPersonViewImage.SetActive(isThirdPerson);
        }

        //? I might not use this, but the above method rather on a button
        public void SetThirdPersonView(bool value)
        {
            isThirdPerson = value;
        }

        public void HandleShooting()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Shoot();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                StartCoroutine(Reload());
            }
        }

        void UpdateTexts()
        {
            if (ammoText != null)
                ammoText.text = ammoCount.ToString();

            if (clipText != null)
                clipText.text = clipCount.ToString();
        }

        public void UpdateHealthSlider()
        {
            if (healthSlider != null)
            {
                healthSlider.value = health;
            }
        }

        private void UpdateCrosshairTarget()
        {
            if (cameraTransform == null || ch_controller == null)
                return;

            Ray ray = new Ray(
                cameraTransform.position,
                cameraTransform.forward
            );

            bool enemyDetected = false;

            if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
            {
                enemyDetected = hit.collider.CompareTag("Enemy");
            }

            ch_controller.SetEnemyTarget(enemyDetected);
        }

        void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (UI_Manager.Instance.isUIMode)
                return;

            if (Cursor.lockState != CursorLockMode.Locked &&
                (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;

            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity *
                            (invertMouseY ? 1f : -1f);

            transform.Rotate(Vector3.up, mouseX);
            _cameraPitch = Mathf.Clamp(
                _cameraPitch + mouseY,
                -maxLookAngle,
                maxLookAngle
            );

            if (cameraTransform != null)
                cameraTransform.localEulerAngles =
                    new Vector3(_cameraPitch, 0f, 0f);
        }

        void HandleMovement()
        {
            bool isGrounded = _cc.isGrounded;

            if (isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            // Keyboard and Joystick controls
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (movementJoystick != null)
            {
                h += movementJoystick.Horizontal;
                v += movementJoystick.Vertical;
            }

            h = Mathf.Clamp(h, -1f, 1f);
            v = Mathf.Clamp(v, -1f, 1f);

            bool crouchingNow = _isCrouching;

            float speed = crouchingNow ? crouchSpeed : walkSpeed;

            // Input direction
            Vector3 move = transform.right * h + transform.forward * v;
            move = Vector3.ClampMagnitude(move, 1f);

            // Animation
            float forwardBack = Mathf.Abs(v) > 0.1f ? Mathf.Sign(v) : 0f;
            float leftRight = Mathf.Abs(h) > 0.1f ? Mathf.Sign(h) : 0f;

            if (animator != null)
            {
                animator.SetFloat("Speed", forwardBack);
                animator.SetFloat("Strafe", leftRight);
                animator.SetBool("Crouching", crouchingNow);
            }

            // Jump
            HandleJumpInput();

            // Movement
            move *= speed;

            _verticalVelocity += gravity * Time.deltaTime;

            move.y = _verticalVelocity;

            _cc.Move(move * Time.deltaTime);
        }

        // Keyboard jump handling
        private void HandleJumpInput()
        {
            if (Input.GetButtonDown("Jump"))
            {
                Jump();
            }
        }

        // -- JUMP
        public void Jump()
        {
            if (!canJump || isDead || isTeleporting)
                return;

            if (!_cc.isGrounded || _isCrouching)
                return;

            _verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);

            if (animator != null)
                animator.SetTrigger("Jump");

            SoundManager.Instance.PlayJumpSound();
        }

        void UpdateCameraView()
        {
            if (cameraTransform == null)
                return;

            if (isThirdPerson)
            {
                cameraTransform.localPosition = Vector3.Lerp(
                    cameraTransform.localPosition,
                    tppOffset,
                    Time.deltaTime * crouchTransitionSpeed
                );
            }
            else
            {
                if (headBobEnabled)
                    HandleHeadBob();
                else
                    cameraTransform.localPosition = Vector3.Lerp(
                        cameraTransform.localPosition,
                        _cameraLocalOrigin,
                        Time.deltaTime * crouchTransitionSpeed
                    );
            }
        }

        void HandleHeadBob()
        {
            bool isMoving = _cc.velocity.magnitude > 0.2f &&
                            _cc.isGrounded;

            if (isMoving)
            {
                _bobTimer += Time.deltaTime * bobFrequency;

                float bobOffset = Mathf.Sin(
                    _bobTimer * Mathf.PI * 2f
                ) * bobAmplitude;

                cameraTransform.localPosition =
                    _cameraLocalOrigin + Vector3.up * bobOffset;
            }
            else
            {
                _bobTimer = 0f;

                cameraTransform.localPosition = Vector3.Lerp(
                    cameraTransform.localPosition,
                    _cameraLocalOrigin,
                    Time.deltaTime * 8f
                );
            }
        }

        #region Shoot + Take Damage

        public void Shoot()
        {
            if (isDead || isTeleporting || isReloading || !canShoot)
                return;

            if (cameraTransform == null)
                return;

            // No ammo in the current clip
            if (clipCount <= 0)
            {
                SoundManager.Instance.PlayEmptyClipSound();

                // No spare ammo either
                if (ammoCount <= 0)
                {
                    ammoExhausted = true;
                    return;
                }

                StartCoroutine(Reload());
                return;
            }

            // Shoot
            SoundManager.Instance.PlayShootSound();

            if (noozleFlash != null)
                noozleFlash.Play();

            clipCount--;

            // Raycast from the camera/crosshair
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
            {
                // Damage anything with a damageable component
                Health targetHealth = hit.collider.gameObject.GetComponent<Health>();

                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damage);
                }

                if (hit.collider.CompareTag("Enemy"))
                {
                    Enemy enemy = hit.collider.GetComponent<Enemy>();

                    if (enemy != null && !enemy.isDead)
                    {
                        enemy.ReduceEnemyHealth((int)damage);
                        enemy.GetHit();
                    }
                }

                if (hit.rigidbody != null)
                {
                    hit.rigidbody.AddForce(
                        -hit.normal * impactForce,
                        ForceMode.Impulse
                    );
                }

                if (hitEffect != null)
                {
                    if (particleParent != null)
                    {
                        Instantiate(
                            hitEffect,
                            hit.point,
                            Quaternion.LookRotation(hit.normal),
                            particleParent
                        );
                    }
                    else
                    {
                        Instantiate(
                            hitEffect,
                            hit.point,
                            Quaternion.LookRotation(hit.normal)
                        );
                    }
                }
            }

            UpdateTexts();

            if (animator != null)
                animator.SetTrigger("Shoot");

            // Automatically reload after the last bullet
            if (clipCount <= 0 && ammoCount > 0)
            {
                StartCoroutine(Reload());
            }
        }

        public void PlayerTakeDamage(int damage)
        {
            if (isDead || damage <= 0)
                return;

            health -= damage;

            // CRITICAL HEALTH
            if (health <= 80 && !criticalShown)
            {
                criticalShown = true;
                UI_Manager.Instance.ShowCritical();
            }

            if (health <= 0)
            {
                health = 0;
                isDead = true;
                Die();
            }

            UpdateHealthSlider();
        }

        #endregion


        #region Die + Reload + Restart + Reset

        public void Die()
        {
            UI_Manager.Instance.PlayGameOverSequence();

            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();

            if (animator != null)
                animator.SetTrigger("Die");

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ManualReload()
        {
            StartCoroutine(Reload());
        }

        IEnumerator Reload()
        {
            if (isReloading || ammoCount <= 0 || isDead)
                yield break;

            canShoot = false;
            isReloading = true;

            if (animator != null)
                animator.SetTrigger("Reloading");

            SoundManager.Instance.PlayReloadSound();

            yield return new WaitForSeconds(1.10f);

            if (isDead)
            {
                isReloading = false;
                canShoot = false;
                yield break;
            }

            ammoCount--;
            clipCount = 25;

            ammoExhausted = false;
            canShoot = true;
            isReloading = false;

            UpdateTexts();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game Scene");
        }

        public void LoadUIScene()
        {
            UI_Manager.Instance.LoadUIScene();
        }

        public void LoadSettings()
        {
            UI_Manager.Instance.OpenSettings();
        }

        // Pause Game
        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        // Resume Game
        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        public void PlayClickSound()
        {
            SoundManager.Instance.PlayCkickSound();
        }

        #endregion

        void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Collectible"))
            {
                other.gameObject.SetActive(false);

                health = Mathf.Min(health + 100, maxHealth);

                if (healingFx != null)
                    healingFx.Play();

                SoundManager.Instance.PlayReviveSound();
                UI_Manager.Instance.ShowBigSave();

                UpdateHealthSlider();
            }
        }

        // TELEPORT POWERUP
        public void ActivateTeleport()
        {
            if (!CanActivateTeleport())
                return;

            StartCoroutine(Teleport(true));
        }

        private bool CanActivateTeleport()
        {
            return !isDead && !isTeleporting && canTeleport;
        }

        private IEnumerator Teleport(bool consumeUICharge)
        {
            if (consumeUICharge && UI_Manager.Instance.teleportCharge < 0.2f)
                yield break;

            isTeleporting = true;

            // Determine teleport destination.
            Vector3 teleportDirection = transform.forward;

            Vector3 destination = transform.position + teleportDirection * teleportDistance;

            // Check for obstacles.
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, teleportDirection,  out RaycastHit hit,
                teleportDistance, teleportObstacleLayers, QueryTriggerInteraction.Ignore))
            {
                destination = transform.position + teleportDirection * Mathf.Max(0f, hit.distance - teleportWallPadding);
            }

            // Do not consume a charge if teleportation is impossible.
            if (Vector3.Distance(transform.position, destination) < 0.1f)
            {
                isTeleporting = false;
                UpdateTeleportUI();
                yield break;
            }

            // Teleport can no longer be activated during this cooldown.
            canTeleport = false;

            // Consume exactly ONE charge after validating the destination.
            if (consumeUICharge)
                UI_Manager.Instance.ConsumeTeleportCharge();

            UpdateTeleportUI();



            // Hide character model.
            if (teleportVisual != null)
            {
                float elapsed = 0f;
                float halfDuration = Mathf.Max(0.01f, teleportDuration / 2f);

                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;

                    float progress = Mathf.Clamp01(elapsed / halfDuration);

                    teleportVisual.localScale = Vector3.Lerp(
                        _teleportVisualOriginalScale,
                        Vector3.zero,
                        progress
                    );

                    yield return null;
                }

                teleportVisual.localScale = Vector3.zero;
            }
            else
            {
                yield return new WaitForSeconds(teleportDuration / 2f);
            }

            if (teleportOutFX != null)
            teleportOutFX.Play();

            if (animator != null)
            {
                animator.ResetTrigger("Die");
                animator.ResetTrigger("Reloading");
                animator.ResetTrigger("Shoot");
                animator.ResetTrigger("Jump");
                animator.ResetTrigger("Idle");

                animator.SetTrigger("Teleport");
            }

            // Move player.
            _cc.enabled = false;
            transform.position = Vector3.Lerp(transform.position, destination, teleportDuration);
            _cc.enabled = true;

            if (teleportInFX != null)
                teleportInFX.Play();

            // Restore character model.
            if (teleportVisual != null)
            {
                float elapsed = 0f;
                float halfDuration = Mathf.Max(0.01f, teleportDuration / 2f);

                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;

                    float progress = Mathf.Clamp01(elapsed / halfDuration);

                    teleportVisual.localScale = Vector3.Lerp(
                        Vector3.zero,
                        _teleportVisualOriginalScale,
                        progress
                    );

                    yield return null;
                }

                teleportVisual.localScale = _teleportVisualOriginalScale;
            }
            else
            {
                yield return new WaitForSeconds(teleportDuration / 2f);
            }

            isTeleporting = false;

            if (animator != null)
                animator.ResetTrigger("Teleport");

            UpdateTeleportUI();

            // Wait for teleport cooldown.
            yield return new WaitForSeconds(teleportCooldown);

            canTeleport = true;

            UpdateTeleportUI();
        }

            
        // Torch
        public void ToggleTorch()
        {
            if (isDead || isCoolingDown)
                return;

            if (UI_Manager.Instance == null)
                return;

            // TORCH IS CURRENTLY ON: TURN IT OFF AND PAUSE DRAINING.
            if (isTorchOn)
            {
                isTorchOn = false;
                pauseTorchRoutine = true;

                if (torchObject != null)
                    torchObject.SetActive(false);

                UI_Manager.Instance.ShowNotification(
                    UI_Manager.Instance.torchOffCanvas,
                    UI_Manager.Instance.notificationStayDuration
                );

                return;
            }

            // No remaining charge.
            if (torchRemainingTime <= 0f)
                return;

            // TURN TORCH ON OR RESUME ITS PREVIOUS DRAIN.
            isTorchOn = true;
            pauseTorchRoutine = false;

            if (torchObject != null)
                torchObject.SetActive(true);

            UI_Manager.Instance.ShowNotification(
                UI_Manager.Instance.torchOnCanvas,
                UI_Manager.Instance.notificationStayDuration * 1.5f
            );

            UI_Manager.Instance.FadeTorchSlider(true);

            // Only start a new coroutine if one isn't already running.
            if (torchCoroutine == null)
                torchCoroutine = StartCoroutine(TorchRoutine());
        }

        private IEnumerator TorchRoutine()
        {
            float duration = Mathf.Max(0.01f, UI_Manager.Instance.torchDuration);

            while (torchRemainingTime > 0f)
            {
                // Pause without losing the remaining time.
                if (pauseTorchRoutine || !isTorchOn)
                {
                    yield return null;
                    continue;
                }

                torchRemainingTime -= Time.deltaTime;

                torchRemainingTime = Mathf.Max(0f, torchRemainingTime);

                float progress = Mathf.Clamp01(
                    torchRemainingTime / duration
                );

                UpdateTorchFill(progress);

                yield return null;
            }

            // TORCH CHARGE IS EMPTY.
            torchRemainingTime = 0f;

            isTorchOn = false;
            pauseTorchRoutine = false;

            if (torchObject != null)
                torchObject.SetActive(false);

            UpdateTorchFill(0f);

            torchCoroutine = null;

            // Automatically begin refilling.
            if (torchRefillCoroutine == null)
                torchRefillCoroutine = StartCoroutine(RefillRoutine());
        }
        private IEnumerator RefillRoutine()
        {
            isCoolingDown = true;

            if (torchButton != null)
                torchButton.interactable = false;

            if (UI_Manager.Instance != null)
                UI_Manager.Instance.FadeTorchSlider(false);

            float refillDuration = Mathf.Max(
                0.01f,
                UI_Manager.Instance.torchRefillDuration
            );

            float timer = 0f;

            while (timer < refillDuration)
            {
                timer += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    timer / refillDuration
                );

                UpdateTorchFill(progress);

                yield return null;
            }

            // FULLY REFILLED.
            torchRemainingTime = UI_Manager.Instance.torchDuration;

            UpdateTorchFill(1f);

            if (UI_Manager.Instance != null)
            {
                UI_Manager.Instance.ShowNotification(
                    UI_Manager.Instance.torchReady,
                    UI_Manager.Instance.notificationStayDuration * 1.5f
                );
            }

            isCoolingDown = false;
            torchRefillCoroutine = null;

            if (torchButton != null)
                torchButton.interactable = true;
        }


        // Keeps both torch fill indicators synchronized.
        private void UpdateTorchFill(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (torchFill != null)
                torchFill.fillAmount = progress;

            if (UI_Manager.Instance != null &&
                UI_Manager.Instance.torchSlider != null)
            {
                UI_Manager.Instance.torchSlider.value = progress;
            }
        }

        #region Items Usage

        // Use health item
        public void UseHealthItem()
        {
            if (isDead || health >= maxHealth)
                return;

            if (StoreManager.Instance == null)
                return;

            if (!StoreManager.Instance.UseItem(StoreItemType.Health))
                return;

            health = Mathf.Min(health + healthItemRestore, maxHealth);

            if (health > 50)
                criticalShown = false;

            if (healingFx != null)
                healingFx.Play();

            SoundManager.Instance.PlayReviveSound();

            UpdateHealthSlider();

            // BIG SAVE NOTIFICATION
            if (UI_Manager.Instance != null)
                    UI_Manager.Instance.ShowBigSave();
        }

        // Use teleport item
        public void UseTeleportItem()
        {
            if (isDead || isTeleporting)
                return;

            // Don't consume an inventory item if already at six charges.
            if (!UI_Manager.Instance.CanReceiveTeleportCharge())
            {
                UI_Manager.Instance.ShowTeleportFull();
                return;
            }

            // Consume one inventory item.
            if (!StoreManager.Instance.UseItem(StoreItemType.Teleport))
                return;

            // Add one teleport charge.
            UI_Manager.Instance.AddTeleportCharge();

            UpdateTeleportUI();
        }

        // Updates teleport UI
        public void UpdateTeleportUI()
        {
            float currentCharges = UI_Manager.Instance.GetTeleportCharge();

            if (teleportFill != null)
            {
                teleportFill.fillAmount = currentCharges;
            }

            if (teleportButton != null)
            {
                teleportButton.interactable =
                    currentCharges >= 0.2f &&
                    canTeleport &&
                    !isTeleporting &&
                    !isDead;
            }

            Debug.Log($"Teleport UI Updated | Charges: {(currentCharges):P0}"
            );
        }

        // Use extra life
        public void UseExtraLifeItem()
        {
            if (isDead)
                return;

            if (StoreManager.Instance == null)
                return;

            if (!StoreManager.Instance.UseItem(StoreItemType.ExtraLife))
                return;

            StoreManager.Instance.AddItemQuantity(StoreItemType.Health, 1);
        }

        // Use ammo item
        public void UseAmmoItem()
        {
            if (isDead)
                return;

            if (StoreManager.Instance == null)
                return;

            if (!StoreManager.Instance.UseItem(StoreItemType.Ammo))
                return;

            ammoCount += ammoItemRestore;
            ammoExhausted = false;

            UpdateTexts();
        }

        #endregion
    }
}