using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CollideAndSlideController : MonoBehaviour
{

    //private PlayerMovementState currentMoveState;
    [SerializeField] private GameObject playerRenderObject;
    float timeStamp;
    
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private AnimationCurve accelerationCurve;
    [SerializeField] private AnimationCurve deaccelerationCurve;
    [SerializeField] private float directionChangeSmoothing;
    private float currentSpeed;

    [SerializeField] private float maxSlopeAngle = 55f;
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float skinWidth = 0.015f;
    [SerializeField] private float rotSpeed;
    [SerializeField] private float maxSprintMod;
    [SerializeField] private float maxCrouchSpeed;
    [SerializeField] private float maxAirSpeedMod = 1;
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float gravityScale = 1;
    
    float verticalVel;
    private float currentSprintMod = 1;

    [Header("Stair Climbing")]
    [SerializeField] private float maxStairheight;
    [SerializeField] private float stepSmoothing;
   
    

    [Header("Components")]
    private CapsuleCollider col;
    private Rigidbody rb;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collideLayer;
    
    private Vector3 moveAmount;
    private Vector3 p1, p2;

    private Vector3 inputDir = Vector3.zero;//direction vector from the input
    private Vector3 moveDir = Vector3.zero;//the direction the player moves in

    private bool airControlOff = false;

    public bool canMove = true;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        rb.isKinematic = true;
       // coolDown = new WaitForSeconds(wallRunCoolDown);
    }
    
    void Update()
    {
        // SetLookRotations(look);
        LookTowardsMovement();
    }
    private void FixedUpdate()
    {

        
            Move();
        //Debug.Log(IsGrounded());
        if (IsGrounded() && verticalVel < 0) verticalVel = 0;

    }
    public void MoveDir(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if(context.performed && inputDir.magnitude == 0) timeStamp = Time.time;
        if(input.sqrMagnitude == 0 ) timeStamp = Time.time;
        inputDir = new Vector3(-input.y, 0, input.x);
        
    }
    private void LookTowardsMovement()
    {
        if (!canMove) return;
        
        if (moveDir == Vector3.zero) return;
        Quaternion rotateTo = Quaternion.LookRotation(moveDir, Vector3.up);
        playerRenderObject.transform.rotation = Quaternion.Slerp(playerRenderObject.transform.rotation, rotateTo, rotSpeed * Time.deltaTime);
    }
    //if there is input, lerp the move direction towards the input direction
    //if there is no input, lerp the move direction towards 0
    //this creates a smooth transition between the inputs and 
    //allows for realistic drifting after input is released
    private void CalculateMoveDir()
    {
        if (airControlOff)
        {
            if (IsGrounded()) airControlOff = false;
            else return;
        }
        if (inputDir != Vector3.zero)
        {
            moveDir = Vector3.Lerp(moveDir, inputDir, Time.fixedDeltaTime * directionChangeSmoothing);
        }
        else if(moveDir != Vector3.zero)
        {
            moveDir = Vector3.Lerp(moveDir, Vector3.zero, Time.fixedDeltaTime * directionChangeSmoothing);
            if(moveDir.sqrMagnitude <= 0.1)
            {
                moveDir = Vector3.zero;
            }
        }
    }
    //calculates speed based on the acceleration and deacceleration curves
    private void CalculateSpeed()
    {
        if (inputDir != Vector3.zero)
        {
            currentSpeed = (maxSpeed * currentSprintMod) * accelerationCurve.Evaluate(Time.time - timeStamp);
            if (currentSpeed > maxSpeed)
            {
                currentSpeed = maxSpeed * currentSprintMod * maxAirSpeedMod;
            }
        }
        else
        {
            currentSpeed = (maxSpeed * currentSprintMod * maxAirSpeedMod) * deaccelerationCurve.Evaluate(Time.time - timeStamp);
            if (currentSpeed < 0)
            {
                currentSpeed = 0;
            }
        }
    }
    private void Move()
    {
        // Horizontal movement
        if (!canMove)return;
        
        CalculateMoveDir();//find the direction
        CalculateSpeed();//find the speed
        
        //multiply by rotation to ensure it is based on forward of the player
         moveAmount = transform.rotation * moveDir.normalized * currentSpeed;

       
        //adjusts vector to follow along the normal of a slope if any
        Vector3 foundNormal;
        if (OnSlope(out foundNormal))
        {
            moveAmount = ProjectAndScale(moveAmount, foundNormal);
        }
        
        //collide and slide baby
        moveAmount = CollideAndSlide(moveAmount, transform.position, 0, false, moveAmount);

        //cielings stop vertical momentum
        if (verticalVel > 0 && CielingCheck())
        {
            verticalVel = 0;
        }

        // Gravity
        verticalVel += (Physics.gravity.y * 2f * Time.fixedDeltaTime * gravityScale);
        Vector3 gravityMove = new Vector3(0, verticalVel * Time.fixedDeltaTime, 0);
        moveAmount += CollideAndSlide(gravityMove, transform.position + moveAmount, 0, true, gravityMove);

        //allows stair climbing without gravity interrupting
        //checks for slopes to prevent weird jittering when climbing
        //runs after gravity simulation because gravity was preventing the smooth motion
        
        if (moveDir.magnitude > 0) 
        {
            if (IsGrounded())
            {
                if (!OnSlope(out foundNormal) && CanClimbStep(col.bounds.min.y + 0.02f, maxStairheight)) moveAmount.y = stepSmoothing;
            }
        }

        // Apply movement

        rb.MovePosition(transform.position + moveAmount);
        
    }

    
    RaycastHit[] groundDetection = new RaycastHit[3];
    private bool IsGrounded() 
    {
        Vector3 center = transform.position + transform.rotation * col.center;
        float radius = col.radius * 0.9f;
        Vector3 origin = new Vector3(center.x, col.bounds.min.y + radius + 0.01f, center.z);
        Debug.DrawLine(origin, origin + Vector3.down * (radius + 0.3f),Color.red,0.5f);
        return (Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundDetection, 0.3f, collideLayer) > 0);
    }

    Collider[] cielingDetection = new Collider[3];
    private bool CielingCheck()
    {
        Vector3 center = transform.position + transform.rotation * col.center;
        float radius = col.radius;
        Vector3 origin = new Vector3(center.x, col.bounds.max.y-0.01f, center.z);
        //bool cieling = Physics.SphereCast(origin, radius, Vector3.up, out _, 0.3f, collideLayer);
        // Physics.over
        // Debug.Log(cieling);
        return (Physics.OverlapSphereNonAlloc(origin, radius, cielingDetection ,collideLayer) > 0);
    }

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 startPos, int depth, bool gravityPass, Vector3 velInit)
    {
       // Debug.Log("collide and slide");
        Vector3 center = startPos;
        float height = Mathf.Max(col.height, col.radius * 2);
        float halfHeight = (height / 2f) - col.radius;
        Vector3 up = transform.up;

        p1 = center + up * halfHeight;
        p2 = center - up * halfHeight;

        if (depth >= maxBounces)
            return Vector3.zero;

        float dist = vel.magnitude + skinWidth;
        if (Physics.CapsuleCast(p1, p2, col.radius, vel.normalized, out RaycastHit hit, dist, collideLayer))
        {
            Vector3 snapToSurface = vel.normalized * (hit.distance-skinWidth);
            Vector3 leftOver = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            if (snapToSurface.magnitude <= skinWidth)
                snapToSurface = Vector3.zero;

            if (angle <= maxSlopeAngle)
            {
                if (gravityPass)
                {
                  //  Debug.Log("snap");
                    return snapToSurface;
                }

                leftOver = ProjectAndScale(leftOver, hit.normal);
            }
            else
            {
                //scale based on angle against wall
                float scale = 1-Vector3.Dot(new Vector3(hit.normal.x,0,hit.normal.z).normalized, - new Vector3(velInit.x,0,velInit.z).normalized);
                // Stop against vertical walls
                if (IsGrounded() && !gravityPass)
                {
                    leftOver = ProjectAndScale(new Vector3(leftOver.x,0,leftOver.z),new Vector3(hit.normal.x,0,hit.normal.z).normalized) ;
                    leftOver *= scale ;
                }
                else
                {
                    
                    leftOver = ProjectAndScale(leftOver, hit.normal)*scale;
                }
            }

            return snapToSurface + CollideAndSlide(leftOver, startPos + snapToSurface, depth + 1, gravityPass, velInit);
        }
      //  Debug.Log("end of loop");
        return vel;
    }

    private Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
    {
        vec = Vector3.ProjectOnPlane(vec, normal);
        return vec;
    }

    private bool OnSlope(out Vector3 foundNormal)
    {
        
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, col.height + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            foundNormal = slopeHit.normal;
            return angle < maxSlopeAngle && angle != 0;
        }
        foundNormal = Vector3.zero;
        return false;
    }

    private bool CanClimbStep(float originY, float maxHeight)
    {
        Vector3 center = transform.position + transform.rotation * col.center;
        Vector3 origin = new Vector3(center.x, originY, center.z);
        Vector3 upper45 =  Quaternion.AngleAxis( 45f, Vector3.up) * transform.forward;
        Vector3 lower45 =  Quaternion.AngleAxis( -45f, Vector3.up) * transform.forward;

        if(DetectEdgeAndSurface(origin,transform.forward,maxHeight) ||  DetectEdgeAndSurface(origin,upper45, maxHeight) || DetectEdgeAndSurface(origin, lower45, maxHeight))
        {
            return true;
        }
        return false;
        
    }
    //checks if there is something blocking path, if there is check if its at the reach height
    private bool DetectEdgeAndSurface(Vector3 origin,  Vector3 direction, float height)
    {
        RaycastHit minHit;
        Debug.DrawRay(origin, direction * (0.2f + col.radius), Color.red, 0.1f);
        if (Physics.Raycast(origin, direction, out minHit, (0.2f + col.radius), collideLayer))
        {
            RaycastHit maxHit;
            Debug.DrawRay(origin + Vector3.up * height, direction * (0.3f + col.radius), Color.yellow, 0.1f);
            if (!Physics.Raycast(origin + Vector3.up * height, direction, out maxHit, (0.3f + col.radius), collideLayer))
            {
                return true;
            }
        }
        return false;
    }

    
    
    public void SprintToggle(InputAction.CallbackContext context)
    {
        if (context.started)
        { 
            currentSprintMod = maxSprintMod;
           // Camera.main.fieldOfView = 80;

        }
        if (context.canceled)
        {
            currentSprintMod = 1;
           // Camera.main.fieldOfView = 60;
        }

    }
    public void CrouchToggle(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            currentSprintMod = maxCrouchSpeed;
            // Camera.main.fieldOfView = 80;

        }
        if (context.canceled)
        {
            currentSprintMod = 1;
            // Camera.main.fieldOfView = 60;
        }

    }
   
   
    public void Jump(InputAction.CallbackContext context)
    {
        if(context.performed && IsGrounded() && !CielingCheck())
        {
            verticalVel = jumpVelocity;
        }
        
    }


}
