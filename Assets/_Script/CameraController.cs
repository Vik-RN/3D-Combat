using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Transform mainCamera;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    private float currentZoom = 12f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 0.2f; // Kecepatan putar kamera
    public float minPitch = 10f;       // Batas bawah kamera (mendongak)
    public float maxPitch = 80f;       // Batas atas kamera (menunduk)
    
    private float yaw;   // Rotasi Horizontal (Sumbu Y)
    private float pitch = 45f; // Rotasi Vertikal (Sumbu X), default miring 45 derajat

    private PlayerControls playerControls;
    private float zoomInput;
    private Vector2 mouseDelta;
    private bool isPanning;

    void Awake()
    {
        playerControls = new PlayerControls();
    }

    void OnEnable()
    {
        playerControls.Gameplay.Enable();
        
        // Mendaftarkan event saat klik kanan ditekan dan dilepas
        playerControls.Gameplay.CameraPanHold.started += ctx => StartPanning();
        playerControls.Gameplay.CameraPanHold.canceled += ctx => StopPanning();
    }

    void OnDisable()
    {
        playerControls.Gameplay.CameraPanHold.started -= ctx => StartPanning();
        playerControls.Gameplay.CameraPanHold.canceled -= ctx => StopPanning();
        playerControls.Gameplay.Disable();
    }

    void Start()
    {
        // Set rotasi awal Pivot agar sesuai dengan nilai pitch default
        transform.rotation = Quaternion.Euler(pitch, 0, 0);
    }

    void Update()
    {
        // --- LOGIKA ZOOM ---
        zoomInput = playerControls.Gameplay.Zoom.ReadValue<float>();
        if (zoomInput != 0)
        {
            currentZoom -= Mathf.Sign(zoomInput) * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }

        // --- LOGIKA ROTASI KAMERA ---
        if (isPanning)
        {
            // Membaca pergerakan mouse saat klik kanan ditahan
            mouseDelta = playerControls.Gameplay.MouseDelta.ReadValue<Vector2>();

            yaw += mouseDelta.x * rotationSpeed;
            pitch -= mouseDelta.y * rotationSpeed; // Minus agar kontrol tidak terbalik (inverted)
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // Batasi sudut kamera agar tidak tembus tanah

            // Terapkan rotasi ke CameraPivot
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Posisi Pivot selalu mengikuti pemain
            transform.position = target.position;

            // Atur jarak kamera utama (mundur di sumbu Z lokal)
            mainCamera.localPosition = new Vector3(0, 0, -currentZoom);
        }
    }

    // --- FUNGSI KONTROL KURSOR ---

    void StartPanning()
    {
        isPanning = true;
        // Mengunci kursor ke tengah dan menyembunyikannya. 
        // Ini memberi kita ruang gerak mouse tak terbatas untuk merotasi kamera.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void StopPanning()
    {
        isPanning = false;
        // Melepas kursor dan menampilkannya kembali.
        // OS akan otomatis mengembalikan kursor ke posisi sebelum di-lock.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}