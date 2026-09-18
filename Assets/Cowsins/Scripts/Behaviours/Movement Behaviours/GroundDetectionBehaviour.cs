using UnityEngine;
using System.Collections;

namespace cowsins
{
    public class GroundDetectionBehaviour
    {
        private MovementContext context;
        private Rigidbody rb;
        private InputManager inputManager;
        private IPlayerMovementStateProvider playerMovement;
        private IPlayerMovementEventsProvider playerEvents;

        private PlayerOrientation orientation => playerMovement?.Orientation;

        private CapsuleCollider playerCapsuleCollider;

        private PlayerMovementSettings playerSettings;

        private Vector3 playerScale;

        private Coroutine ungroundCoroutine;
        private MonoBehaviour coroutineRunner;

        // Slopes
        private float maxSlopeAngle = 60f;

        // Landing Cooldown
        private float lastLandingTime = -1f;
        private const float landingCooldown = 0.2f;

        // Ground Buffer
        private int groundedFrameCount = 0;
        private int notGroundedFrameCount = 0;
        private const int requiredFrames = 2;

        private bool cancellingGrounded;

        public GroundDetectionBehaviour(MovementContext context)
        {
            this.context = context;
            this.rb = context.Rigidbody;
            this.inputManager = context.InputManager;
            this.playerMovement = context.Dependencies.PlayerMovementState;
            this.playerEvents = context.Dependencies.PlayerMovementEvents;

            this.playerSettings = context.Settings;
            this.playerCapsuleCollider = context.Capsule;

            this.coroutineRunner = context.Transform.GetComponent<MonoBehaviour>();

            this.playerScale = context.Transform.localScale;

            playerEvents.Events.OnJump.AddListener(NegateCancellingGrounded);
        }

        public void Tick()
        {
            if (IsSliding())
                playerMovement.IsSliding = true;

            // Perform ground check every frame
            bool foundGroundThisFrame = PerformGroundCheck(out RaycastHit hit);

            context.IsPlayerOnSlope = IsPlayerOnSlope(hit, foundGroundThisFrame);

            // Store the latest ground hit so other behaviours can use the slope normal
            context.SlopeHit = hit;

            // Buffered grounding system
            bool wasGrounded = playerMovement.Grounded;
            bool newGroundedState = UpdateGroundedState(foundGroundThisFrame, wasGrounded);

            if (newGroundedState != wasGrounded)
            {
                // Just landed
                if (newGroundedState)
                {
                    HandleLanding();
                }
                else // Just left ground
                {
                    HandleLeavingGround();
                }
            }

            playerMovement.Grounded = newGroundedState;
        }

        private bool UpdateGroundedState(bool foundGroundThisFrame, bool wasGrounded)
        {
            if (foundGroundThisFrame)
            {
                groundedFrameCount++;
                notGroundedFrameCount = 0;

                // Cancel any pending unground coroutine
                if (ungroundCoroutine != null)
                {
                    coroutineRunner.StopCoroutine(ungroundCoroutine);
                    ungroundCoroutine = null;
                    cancellingGrounded = false;
                }

                // Become grounded after consistent detection
                if (!wasGrounded && groundedFrameCount >= requiredFrames)
                {
                    return true;
                }
                else if (wasGrounded)
                {
                    return true; // Stay grounded
                }
            }
            else
            {
                notGroundedFrameCount++;
                groundedFrameCount = 0;

                // Only start leaving ground process if we were previously grounded
                if (wasGrounded && !cancellingGrounded)
                {
                    cancellingGrounded = true;
                    if (ungroundCoroutine == null)
                    {
                        ungroundCoroutine = coroutineRunner.StartCoroutine(StopGroundedCoroutine());
                    }
                }
            }

            // No state change
            return wasGrounded;
        }

        private void HandleLanding()
        {
            if (Time.time - lastLandingTime < landingCooldown)
            {
                return;
            }

            lastLandingTime = Time.time;

            cancellingGrounded = false;

            playerEvents.Events.OnLand?.Invoke();
            playerSettings.events.OnLand.Invoke();
            SoundManager.Instance.PlaySound(playerSettings.sounds.landSFX, 0, 0, false);
        }

        private void HandleLeavingGround()
        {
            context.CoyoteTimer = context.CoyoteJumpTime;
            cancellingGrounded = false;
        }

        private void ResetGroundingState()
        {
            playerMovement.Grounded = true;
            cancellingGrounded = false;
            groundedFrameCount = requiredFrames;
            notGroundedFrameCount = 0;

            if (ungroundCoroutine != null)
            {
                coroutineRunner.StopCoroutine(ungroundCoroutine);
                ungroundCoroutine = null;
            }
        }

        private IEnumerator StopGroundedCoroutine()
        {
            yield return new WaitForSeconds(0.1f);

            // Check if we are grounded
            bool stillHasGround = PerformGroundCheck(out RaycastHit hit);

            if (!stillHasGround)
            {
                playerMovement.Grounded = false;
                context.CoyoteTimer = context.CoyoteJumpTime;
            }

            cancellingGrounded = false;
            ungroundCoroutine = null;
        }

        /// <summary>
        /// Determines whether the player is on a slope.
        /// </summary>
        private bool IsPlayerOnSlope(RaycastHit hit, bool isGrounded)
        {
            if (isGrounded)
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                return angle < maxSlopeAngle && angle != 0;
            }
            return false;
        }


        private bool IsSliding()
        {
            return playerMovement.IsCrouching && rb.linearVelocity.magnitude > playerMovement.WalkSpeed;
        }

        private void NegateCancellingGrounded() => cancellingGrounded = false;

        /// <summary>
        /// Checks if the player is grounded through physics raycast.
        /// </summary>
        private bool PerformGroundCheck(out RaycastHit hit)
        {
            // Get capsule center
            Vector3 center = playerCapsuleCollider.transform.TransformPoint(playerCapsuleCollider.center);

            float height = playerMovement.IsCrouching
                ? playerCapsuleCollider.height * 0.5f
                : playerCapsuleCollider.height;

            float radius = playerCapsuleCollider.radius * 0.8f;
            float halfHeight = Mathf.Max(0, (height * 0.5f) - radius);

            // Calculate capsule endpoints from center
            Vector3 bottom = center - Vector3.up * halfHeight;
            Vector3 top = center + Vector3.up * halfHeight;

            // Add small offset
            float startOffset = 0.1f;
            bottom += Vector3.up * startOffset;
            top += Vector3.up * startOffset;

            // Perform the cast
            float castDistance = playerSettings.groundCheckDistance + startOffset;
            bool foundGround = Physics.CapsuleCast(
                top,
                bottom,
                radius,
                Vector3.down,
                out hit,
                castDistance,
                context.WhatIsGround
            );

            if (foundGround && CowsinsUtilities.IsFloor(hit.normal, maxSlopeAngle))
            {
                return true;
            }

            // Fallbacks to Raycast
            Vector3 rayOrigin = center + Vector3.up * startOffset;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit,
                castDistance, context.WhatIsGround))
            {
                return CowsinsUtilities.IsFloor(hit.normal, maxSlopeAngle);
            }

            return false;
        }
    }
}