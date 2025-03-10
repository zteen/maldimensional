﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

#pragma warning disable
    [SerializeField]
    private Rigidbody2D rigidbody;
    [SerializeField]
    private BoxCollider2D collider;
    [SerializeField]
    private Transform spawnPoint;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private float speed = 1.0f;
    [SerializeField]
    private float jumpForceMagnitude = 1.0f;
    [SerializeField]
    private float jetpackSpeed = 1.0f;
    [SerializeField]
    private Slider fuelSlider;
    [SerializeField]
    private float fuelConsumptionRate = 0.1f;
    [SerializeField]
    private float fuelRechargeRate = 0.2f;
    [SerializeField]
    private float movementSmoothing = 0.05f;
    [SerializeField]
    private float feetExtentY;
    [SerializeField]
    private PlatformSpawner platformSpawner;
    [SerializeField]
    private ScrambleBlinker blinker;
    [SerializeField]
    private GameObject dustPrefab;
    [SerializeField]
    private Transform dustSpawnPoint;
    [SerializeField]
    private GameObject smokePrefab;
    [SerializeField]
    private Transform smokeSpawnPoint;
#pragma warning restore

    private AudioManager audioManager;
    private bool facingRight = true;
    private bool isGrounded = true;
    private bool jetPacking = false;
    private bool wasBoosting = false;
    private bool paused = true;
    private LayerMask groundMask;
    private Vector3 velocity;

    private void OnEnable() {
        LoadingFader.onLoadIsFinished += UnPause;
    }

    private void OnDisable() {
        LoadingFader.onLoadIsFinished -= UnPause;
    }

    private void UnPause() {
        paused = false;
    }

    private void Awake() {
        groundMask = LayerMask.GetMask(new string[] { "Ground" });
    }

    private void Start() {
        audioManager = AudioManager.instance;
        audioManager.Play("Theme");

        transform.position = spawnPoint.position;
    }

    void Update() {
        if (paused) return;

        if (Input.GetKeyDown(KeyCode.Escape)) {
            audioManager.Stop("Theme");
            audioManager.Play("BackToMenu");
            SceneManager.LoadScene("StartScene");
        }

        float move = 0.0f;
        bool jump = false;
        bool holdJump = false;

        move = Input.GetAxis("Horizontal");
        jump = Input.GetKeyDown(KeyCode.Space);
        holdJump = Input.GetKey(KeyCode.Space);

        Move(move, jump, holdJump);

        if (isGrounded) {
            fuelSlider.value += fuelRechargeRate * Time.deltaTime;
        }
    }

    private void FixedUpdate() {
        if (paused) return;

        bool wasGrounded = isGrounded;
        isGrounded = false;

        RaycastHit2D raycastHit = Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, Vector2.down, feetExtentY, groundMask);
        if (raycastHit.collider != null && raycastHit.normal == Vector2.up) {
            isGrounded = true;
            jetPacking = false;

            if (!wasGrounded) {
                audioManager.Play("Landing");
                SpawnDust();
            }
        }
        
        if (Application.isEditor) {
            Color debugDrawColor = (raycastHit.collider != null) ? Color.green : Color.red;
            Debug.DrawRay(collider.bounds.center + new Vector3(collider.bounds.extents.x, 0), Vector2.down * (collider.bounds.extents.y + feetExtentY), debugDrawColor);
            Debug.DrawRay(collider.bounds.center - new Vector3(collider.bounds.extents.x, 0), Vector2.down * (collider.bounds.extents.y + feetExtentY), debugDrawColor);
            Debug.DrawRay(collider.bounds.center - new Vector3(collider.bounds.extents.x, collider.bounds.extents.y + feetExtentY), Vector2.right * (collider.bounds.size.x), debugDrawColor);
        }
    }

    private void Move(float move, bool jump, bool holdJump) {

        Vector3 targetVelocity = new Vector2(move * speed, rigidbody.velocity.y);
        rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, targetVelocity, ref velocity, movementSmoothing);

        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("Boosting", false);
        animator.SetBool("HasFuel", FuelLeft());
        animator.SetFloat("HorizontalSpeed", Mathf.Abs(rigidbody.velocity.x));

        if ((move < 0 && facingRight) || (move > 0 && !facingRight)) {
            Flip();
        }

        if (isGrounded && jump) {
            audioManager.Play("Jump");
            SpawnDust();
            rigidbody.AddForce(Vector2.up * jumpForceMagnitude);
        } else if (jump) {
            jetPacking = true;
            Scramble();
        }

        if (!isGrounded && jetPacking && holdJump) {
            if (FuelLeft()) {
                fuelSlider.value -= fuelConsumptionRate * Time.deltaTime;

                Vector3 jetpackTargetVelocity = new Vector2(rigidbody.velocity.x, jetpackSpeed);
                rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, jetpackTargetVelocity, ref velocity, movementSmoothing);

                animator.SetBool("Boosting", true);
                audioManager.Play("Boosting");

                if (!wasBoosting) {
                    SpawnSmoke();
                }

                wasBoosting = true;
            } else {
                audioManager.Stop("Boosting");
                wasBoosting = false;
            }
        } else {
            audioManager.Stop("Boosting");
            wasBoosting = false;
        }
    }

    private bool FuelLeft() {
        return fuelSlider.value > fuelSlider.minValue;
    }

    private void Scramble() {
        platformSpawner.RespawnPlatforms();
        blinker.ScrambleBlink();

        audioManager.Play("Scramble");
    }

    private void Flip() {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        facingRight = !facingRight;
    }

    private void SpawnDust() {
        Instantiate(dustPrefab, dustSpawnPoint.position, Quaternion.identity);
    }

    private void SpawnSmoke() {
        Instantiate(smokePrefab, smokeSpawnPoint.position, Quaternion.identity);
    }
}
