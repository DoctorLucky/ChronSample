using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ELVISController : MonoBehaviour
{
    //Initial setup, getting components
    private Animator animator;
    private Rigidbody2D rb2d;
    public GameObject Bianca;
    public GameObject bulletPrefab;
    public GameObject bulletSpawn;
    private AudioSource hitSound;
    public Transform groundCheck;
    public LayerMask whatIsGround;
    public LayerMask whatIsPunchable;

    //Set up player variables
    public float bulletSpeed;
    public float knockback;
    public float speed;
    private bool jump;
    private bool hasJumped;
    public float jumpheight;
    private bool faceright;
    private int health;
    public int jumpNum;
    private float gravityScale;
    private float healthScale;
    public Image healthBar;
    private int invulnTime;
    private int invulnTimeMax;
    bool gravity = false;
    private int attackCounter;
    private int attackDelay;

    void Awake()
    {
        healthScale = healthBar.rectTransform.localScale.x;
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        attackDelay = 30;
        faceright = true;
        jumpNum = 2;
        health = 5;
        gravityScale = 4;
        rb2d = GetComponent<Rigidbody2D>();
        hitSound = GetComponents<AudioSource>()[1];
        SetHealthText();
    }

    void Update()
    {
        JumpCheck(); //checks to see if spacebar is pressed, sets 'jump' to true if any jumps left. Adds 2 jumps if on the ground.
        PunchCheck(); //Checks to see if an attack key is pressed, fires a blast if so
        GravityToggle(); //Disables gravity if touching the ground
    }

    void FixedUpdate()
    {
        if (attackCounter > 0)
            attackCounter--;
        if (invulnTime > 0)
            invulnTime--;
        
        float vert = rb2d.velocity.y;
        if (jump == true)
        {
            rb2d.velocity = new Vector2(rb2d.velocity.x, 0); //cancel vertical velocity when jump starts
            jump = false;
            jumpNum--;
            vert = jumpheight;
            animator.SetBool("Jumping", true);
        }
        float horiz = 0f;
        if (Input.GetKey(KeyCode.Keypad4))
        {
            horiz = -1;
        }
        if (Input.GetKey(KeyCode.Keypad6))
        {
            horiz = 1;
        }
        rb2d.AddForce(new Vector2((speed * horiz), vert));
        Vector2 currentVel = rb2d.velocity;
        rb2d.velocity = new Vector2((currentVel.x * 0.8f), currentVel.y); //a constant horizontal brake
        //control animator based on current movement status
        bool isJumping = animator.GetBool("Jumping");
        if (horiz != 0 && isJumping == false)
            animator.SetBool("Running", true);
        else
            animator.SetBool("Running", false);
        //Change sprite facing, if necessary by inverting xscale
        if ((horiz < 0) && (faceright == true))
        {
            faceright = false;
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        }
        else if ((horiz > 0) && (faceright == false))
        {
            faceright = true;
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        }
    }

    void OnTriggerEnter2D(Collider2D other) //If either player touches the bottom of the camera bounding box, teleport them to the other player's location
    {
        if (other.gameObject.CompareTag("BoundBot"))
        {
            transform.position = Bianca.transform.position;
            rb2d.velocity = Bianca.GetComponent<Rigidbody2D>().velocity;
            jumpNum = 0;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if ((other.gameObject.CompareTag("Damaging")) && (invulnTime <= 0)) //If the player is touching something damaging, take damage etc.
        {
            health--;
            invulnTime = invulnTimeMax;
            SetHealthText();
            hitSound.Play();
            Vector2 dirVector = (transform.position - other.transform.position);
            dirVector.Normalize();
            rb2d.AddForce(dirVector * knockback);
        }
    }

    void PunchCheck() //Technically ShootCheck, but the other character originally used her fists and I kept the name to maintain similarities for easier maintenance
    {
        if (((Input.GetMouseButton(0)) | Input.GetKey(KeyCode.Keypad9)) && (attackCounter <= 0)) //Left click and numpad-9 both shoot towards the cursor
        {
            attackCounter = attackDelay;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); //converts a point in the camera view into world coordinates
            Vector2 bulletDir = new Vector2((mousePos.x - transform.position.x),(mousePos.y - transform.position.y));
            bulletDir.Normalize();

            GameObject bulletInstance = (GameObject)Instantiate(bulletPrefab, bulletSpawn.transform.position, new Quaternion(0,0,0,0));
            bulletInstance.GetComponent<Rigidbody2D>().velocity = bulletDir * bulletSpeed;
            AudioSource.PlayClipAtPoint(GetComponent<AudioSource>().clip, transform.position);
        }
    }

    void JumpCheck()
    {
        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            CheckGround();
            if (jumpNum > 0)
            {
                jump = true;
            }
        }
    }

    void SetHealthText() //If player has health left, reduce the bar to reflect new value. Otherwise, return to main menu.
    {
        if (health > 0)
        {
            healthBar.rectTransform.localScale = new Vector3((health * (healthScale / 5)), healthBar.rectTransform.localScale.y, healthBar.rectTransform.localScale.z);
        }
        else
        {
            healthBar.gameObject.SetActive(false);
            Application.LoadLevel("Menu");
        }
    }

    bool CheckGround() //checks to see if a bounding circle around ELVIS's feet is touching the ground layer
    {
        Vector2 position = groundCheck.position;
        bool grounded = Physics2D.OverlapCircle(position, 0.205f, whatIsGround);
        return (grounded);
    }

    //Originally didn't have this, but the physics engine was causing ELVIS to gently slide down slopes, and instead of
    //increasing friction I figured it'd be easier to just turn off gravity when he's touching the ground.
    void GravityToggle()
    {
        bool grounded = CheckGround();
        if ((grounded == true) && (gravity == true))
        {
            rb2d.gravityScale = 0;
            gravity = false;
            jumpNum = 2;
            animator.SetBool("Jumping", false);
        }
        else if ((grounded == false) && (gravity == false))
        {
            rb2d.gravityScale = gravityScale;
            gravity = true;
        }
    }
}