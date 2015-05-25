using System;
using UnityEngine;

namespace UnityStandardAssets._2D
{
    public class PlatformerCharacter2D : MonoBehaviour
    {
        [SerializeField] private float m_MaxSpeed = 10f;                    // The fastest the player can travel in the x axis.
        [SerializeField] private float m_JumpForce = 10f;                  // Amount of force added when the player jumps.
        [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;  // Amount of maxSpeed applied to crouching movement. 1 = 100%
		[SerializeField]private float m_LadderClimbSpeed = 8f;
		[SerializeField] private bool m_AirControl = false;                 // Whether or not a player can steer while jumping;
        [SerializeField] private LayerMask m_WhatIsGround;                  // A mask determining what is ground to the character

        private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
        const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
        private bool m_Grounded;            // Whether or not the player is grounded.
		private bool m_DidDoubleJump;
		private bool m_CanClimb;
		private bool m_ClimbingLadder;
        private Transform m_CeilingCheck;   // A position marking where to check for ceilings
        const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up
        public Animator m_Anim;            // Reference to the player's animator component.
        private Rigidbody2D m_Rigidbody2D;
        private bool m_FacingRight = true;  // For determining which way the player is currently facing.

        private void Awake()
        {
			print("Hello!");
            // Setting up references.
            m_GroundCheck = transform.Find("GroundCheck");
            m_CeilingCheck = transform.Find("CeilingCheck");
            m_Anim = GetComponent<Animator>();
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
        }

		void Update(){
            m_Grounded = false;

            // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
            // This can be done using layers instead but Sample Assets will not overwrite your project settings.
            Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
            for (int i = 0; i < colliders.Length; i++)
            {
				if (colliders[i].gameObject != gameObject) {
					m_Grounded = true;
					m_DidDoubleJump = false;
				}
            }
            m_Anim.SetBool("Ground", m_Grounded);

            // Set the vertical animation
            m_Anim.SetFloat("vSpeed", m_Rigidbody2D.velocity.y);
			NormalizeSlope();
        }


        public void Move(float move, bool crouch, bool jump, float climb)
        {
            // If crouching, check to see if the character can stand up
            if (!crouch && m_Anim.GetBool("Crouch"))
            {
                // If the character has a ceiling preventing them from standing up, keep them crouching
                if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
                {
                    crouch = true;
                }
            }

            // Set whether or not the character is crouching in the animator
            m_Anim.SetBool("Crouch", crouch);

            //only control the player if grounded or airControl is turned on
            if (m_Grounded || m_AirControl)
            {
                // Reduce the speed if crouching by the crouchSpeed multiplier
                move = (crouch ? move*m_CrouchSpeed : move);

                // The Speed animator parameter is set to the absolute value of the horizontal input.
                m_Anim.SetFloat("Speed", Mathf.Abs(move));

				if (m_CanClimb && climb != 0) {
					m_ClimbingLadder = true;

					if (climb < 0 && m_Grounded) {

					}
				}

				if (m_ClimbingLadder) {
					m_Rigidbody2D.velocity = Vector2.up * climb * m_LadderClimbSpeed;
					m_Rigidbody2D.gravityScale = 0;
				}
				else
					m_Rigidbody2D.gravityScale = 3;

                // Move the character
                m_Rigidbody2D.velocity = new Vector2(move*m_MaxSpeed, m_Rigidbody2D.velocity.y);

                // If the input is moving the player right and the player is facing left...
                if (move > 0 && !m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
                    // Otherwise if the input is moving the player left and the player is facing right...
                else if (move < 0 && m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
            }

            // If the player should jump...
			if ((m_Grounded || !m_DidDoubleJump || m_ClimbingLadder) && jump)
			{
                m_Anim.SetBool("Ground", false);
				m_ClimbingLadder = false;
				m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, m_JumpForce);

				if (!m_Grounded && !m_DidDoubleJump)
					m_DidDoubleJump = true;

                m_Grounded = false;
            }
        }

		void OnTriggerEnter2D(Collider2D other) {
			if (other.CompareTag("Ladder")) 
				m_CanClimb = true;
		}

		void OnTriggerExit2D(Collider2D other) {
			if (other.CompareTag("Ladder")) {
				m_CanClimb = false;
				m_ClimbingLadder = false;
			}
		}

        private void Flip()
        {
            // Switch the way the player is labelled as facing.
            m_FacingRight = !m_FacingRight;

            // Multiply the player's x local scale by -1.
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }

		void NormalizeSlope() {
			// Attempt vertical normalization
			if (m_Grounded) {
				RaycastHit2D hit = Physics2D.Raycast(transform.position, -Vector2.up, 1f, m_WhatIsGround);

				if (hit.collider != null && Mathf.Abs(hit.normal.x) > 0.1f) {
					Rigidbody2D body = GetComponent<Rigidbody2D>();
					// Apply the opposite force against the slope force 
					// You will need to provide your own slopeFriction to stabalize movement
					body.velocity = new Vector2(body.velocity.x - (hit.normal.x * 1), body.velocity.y);

					//Move Player up or down to compensate for the slope below them
					Vector3 pos = transform.position;
					pos.y += -hit.normal.x * Mathf.Abs(body.velocity.x) * Time.deltaTime * (body.velocity.x - hit.normal.x > 0 ? 1 : -1);
					transform.position = pos;
				}
			}
		}
    }


}
