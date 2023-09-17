using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.UI.ContentSizeFitter;

public class MsPacman : MonoBehaviour
{
    public enum directionEnum { None, North, South, East, West };
    public enum orientationEnum { Horizontal, Vertical };

    private const float PLAYER_SPEED = 250.0f;
    private const float ROTATION_SPEED = 1080f;

    public Vector3 initialPosition;
    public Vector3 position;
    public Quaternion initialRotation;
    public Quaternion rotation;
    private Quaternion targetRotation;
    private bool isRotating = false;
    public directionEnum initialDirection;
    public directionEnum direction;
    private directionEnum potentialDirection; // For casting rays to help with queueing
    private directionEnum currentDirection;
    private Vector3 currentRayDirection;
    public Vector3 nodePosition;
    public Vector3 rayDirection;
    public Queue<directionEnum> directionQueue = new Queue<directionEnum>();
    public Queue<Vector3> rayDirectionQueue = new Queue<Vector3>();
    private bool isOverlappingWithNode = false;
    orientationEnum orientation;
    bool flag = false, flag2 = false; // For debugging purposes to display the results of the directionqueue only once.
    private Node currentCollidedNode = null;
    private DirectionalNode currentCollidedDirectionalNode = null;
    private float rayDistance = 30.0f;
    private float moveSpeed;
    [SerializeField] private AudioSource pelletChomp;
    [SerializeField] private AudioSource powerPellet;
    [SerializeField] private GameControl gameControl;
    private Animator anim;
    private bool isPowerPelletActive = false;
    private Coroutine powerPelletCoroutine = null;

    void OnTriggerEnter(Collider collision)
    {
        int layerOfCollidingObject = collision.gameObject.layer;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Pellet"))
        {
            Destroy(collision.gameObject);
            if (gameControl.pelletsLeft > 0)
                gameControl.pelletsLeft -= 1;
            else
                gameControl.pelletsLeft = 0;

            if (pelletChomp != null)
                pelletChomp.Play();
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Power Pellet"))
        {
            Destroy(collision.gameObject);
            if (gameControl.pelletsLeft > 0)
                gameControl.pelletsLeft -= 1;
            else
                gameControl.pelletsLeft = 0;

            if (powerPellet != null)
                powerPellet.Play();

            if (isPowerPelletActive && powerPelletCoroutine != null)
            {
                StopCoroutine(powerPelletCoroutine);
                powerPellet.Stop();
            }

            isPowerPelletActive = true;
            powerPellet.Play();
            powerPelletCoroutine = StartCoroutine(StopSoundAfterSeconds(powerPellet, 5f));
        }
    }

    IEnumerator StopSoundAfterSeconds(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.Stop();
        isPowerPelletActive = false;
    }

    private Vector3 GetDirectionVector(directionEnum direction)
    {
        switch (direction)
        {
            case directionEnum.North:
                return Vector3.forward;
            case directionEnum.South:
                return Vector3.back;
            case directionEnum.East:
                return Vector3.right;
            case directionEnum.West:
                return Vector3.left;
            default:
                return Vector3.zero;
        }
    }

    private directionEnum GetDirectionFromRayDirection(Vector3 rayDirection)
    {
        if (rayDirection.z > 0f)
            return directionEnum.North;
        else if (rayDirection.z < 0f)
            return directionEnum.South;
        else if (rayDirection.x > 0f)
            return directionEnum.East;
        else if (rayDirection.x < 0f)
            return directionEnum.West;
        else
            return directionEnum.None;
    }

    public bool CastRay(directionEnum direction)
    {
        Vector3 rayDirection = GetDirectionVector(direction);

        RaycastHit hit;

        int layerMask = 1 << LayerMask.NameToLayer("Directional Node");

        if (Physics.Raycast(transform.position, rayDirection, out hit, rayDistance, layerMask))
        {
            DirectionalNode hitNode = hit.collider.GetComponent<DirectionalNode>();
            if (hitNode)
            {
                //Renderer sphereRenderer = hitNode.GetComponent<Renderer>();
                //if (sphereRenderer)
                //{
                //    sphereRenderer.material.color = Color.blue;  // Change color to red. Adjust to desired color.
                //}

                // The ray hit a Node
                return true;
            }
        }
        return false;
    }

    public bool CastRayDebug(directionEnum direction, out DirectionalNode hitNode)
    {
        Vector3 rayDirection = GetDirectionVector(direction);

        RaycastHit hit;
        Debug.DrawRay(transform.position, GetDirectionVector(direction) * rayDistance, Color.red, 1f);
        int layerMask = 1 << LayerMask.NameToLayer("Directional Node");

        if (Physics.Raycast(transform.position, rayDirection, out hit, rayDistance, layerMask))
        {
            hitNode = hit.collider.GetComponent<DirectionalNode>();
            if (hitNode)
            {
                //Renderer sphereRenderer = hitNode.GetComponent<Renderer>();
                //if (sphereRenderer)
                //{
                //    sphereRenderer.material.color = Color.blue;  // Change color to red. Adjust to desired color.
                //}

                // The ray hit a Node
                return true;
            }
            else
            {
                hitNode = null;
                return false;
            }
                
        }
        hitNode = null;
        return false;
        
    }

    private bool IsOppositeOrientation(directionEnum first, directionEnum second)
    {
        return ((first == directionEnum.North || first == directionEnum.South) && (second == directionEnum.East || second == directionEnum.West)) ||
               ((first == directionEnum.East || first == directionEnum.West) && (second == directionEnum.North || second == directionEnum.South));
    }

    private bool IsSameOrientation(directionEnum first, directionEnum second)
    {
        return ((first == directionEnum.North || first == directionEnum.South) && (second == directionEnum.North || second == directionEnum.South)) ||
               ((first == directionEnum.East || first == directionEnum.West) && (second == directionEnum.East || second == directionEnum.West));
    }

    private bool IsHorizontalOrientation(directionEnum direction)
    {
        return direction == directionEnum.East || direction == directionEnum.West;
    }

    private bool IsVerticalOrientation(directionEnum direction)
    {
        return direction == directionEnum.North || direction == directionEnum.South;
    }

    void HandleQueue(directionEnum newlyIntroducedDirection, Vector3 newlyIntroducedRayDirection)
    {
        ////////////////////////////////////////////////////////////////////////
        // The direction queue is designed to hold only two queues, and no more.
        // Although technically, the queue is handling three queues. The end
        // result will be the queue in question to end with only one or two
        // queues. Once the direction queue obtains a direction, it can never
        // be empty again. It will always have at least one direction despite
        // it being empty at the very start.

        // The direction queue's first queue will always represent the direction
        // the player is moving and/or facing.

        // The second queue will represent a newly introduced direction that the
        // player will travel once the conditions are met.

        // You will never see the first and second queue within the direction
        // queue be the same direction.

        // You will never see the first and second queue be the same orientation.

        // The third future queue is complicated. It will either replace the
        // second queue if it has the same orientation as the direction within
        // the second queue or, if the same orientation as the first, will erase
        // the entire queue list and insert the new direction into the queue.
        // But, in the end, it will never truely be third in the queue.
        ////////////////////////////////////////////////////////////////////////

        if (IsHorizontalOrientation(newlyIntroducedDirection))
            orientation = orientationEnum.Horizontal;
        else if (IsVerticalOrientation(newlyIntroducedDirection))
            orientation = orientationEnum.Vertical;

        // If the queue is empty, just add the direction
        if (directionQueue.Count == 0)
        {
            if (CastRay(potentialDirection)) // Casting a ray is only needed for the initial movement.
            {
                isRotating = true;
                directionQueue.Enqueue(newlyIntroducedDirection);
                rayDirectionQueue.Enqueue(newlyIntroducedRayDirection);
            }

        }
        else if (directionQueue.Count == 1)
        {
            directionEnum directionCurrentlyMoving = directionQueue.Peek();

            // If new direction is of the same orientation as the current direction, replace it.
            // Example: If you are going East, and want to go West, it replaces it. And vice versa.
            // Example: If you are going North, and want to go South, it replaces it. And vice versa.
            if (IsSameOrientation(directionCurrentlyMoving, newlyIntroducedDirection))
            {
                directionQueue.Dequeue();
                directionQueue.Enqueue(newlyIntroducedDirection);

                rayDirectionQueue.Dequeue();
                rayDirectionQueue.Enqueue(newlyIntroducedRayDirection);
                isRotating = true;
            }
            else // If different orientation, add it as future direction
            {
                directionQueue.Enqueue(newlyIntroducedDirection);
                rayDirectionQueue.Enqueue(newlyIntroducedRayDirection);
            }
        }
        // If two items in the queue
        else if (directionQueue.Count == 2)
        {
            directionEnum directionCurrentlyMoving = directionQueue.Dequeue();
            directionEnum potentialDirectionInQueue = directionQueue.Dequeue();

            Vector3 directionCurrentlyMovingRay = rayDirectionQueue.Dequeue();
            Vector3 potentialDirectionInQueueRay = rayDirectionQueue.Dequeue();

            if (IsOppositeOrientation(directionCurrentlyMoving, potentialDirectionInQueue))
            {
                // If new directionEnum is of the same orientation as the current directionEnum
                if (IsSameOrientation(directionCurrentlyMoving, newlyIntroducedDirection))
                {
                    directionQueue.Enqueue(newlyIntroducedDirection);
                    rayDirectionQueue.Enqueue(newlyIntroducedRayDirection);
                }

                // If new direction is of the same orientation as the future direction
                if (IsSameOrientation(potentialDirectionInQueue, newlyIntroducedDirection))
                {
                    directionQueue.Enqueue(directionCurrentlyMoving);
                    directionQueue.Enqueue(newlyIntroducedDirection);

                    rayDirectionQueue.Enqueue(directionCurrentlyMovingRay);
                    rayDirectionQueue.Enqueue(potentialDirectionInQueueRay);
                }
            }
        }
    }

    private bool CanMoveInDirection(directionEnum dir)
    {
        if (currentCollidedDirectionalNode == null) return true; // No node, free movement

        switch (dir)
        {
            case directionEnum.North:
                return currentCollidedDirectionalNode.canMoveNorth;
            case directionEnum.South:
                return currentCollidedDirectionalNode.canMoveSouth;
            case directionEnum.East:
                return currentCollidedDirectionalNode.canMoveEast;
            case directionEnum.West:
                return currentCollidedDirectionalNode.canMoveWest;
            default:
                return false;
        }
    }

    private void CheckForNodeOverlap()
    {
        Collider[] overlaps = Physics.OverlapSphere(transform.position, 0.1f);
        DirectionalNode overlappedDirectionalNode = null;

        // Loop through the overlaps and check for a Node
        foreach (var col in overlaps)
        {
            DirectionalNode directionalNodeComponent = col.GetComponent<DirectionalNode>();

            if (directionalNodeComponent != null)
            {
                overlappedDirectionalNode = directionalNodeComponent;
                break;  // Exit the loop once a Node is found
            }
        }

        if (overlappedDirectionalNode != null)
        {
            currentCollidedDirectionalNode = overlappedDirectionalNode;

            if (directionQueue.Count == 1)
            {
                if (!CanMoveInDirection(direction))
                {
                    moveSpeed = 0.0f;
                    anim.SetTrigger("Stop");
                }
                else
                {
                    moveSpeed = PLAYER_SPEED;
                    anim.SetTrigger("Chomp");
                }

            }
            else if (directionQueue.Count == 2)
            {
                if (IsOppositeOrientation(direction, potentialDirection))
                {
                    if (CanMoveInDirection(potentialDirection))
                    {
                        isRotating = true;
                        directionQueue.Dequeue();
                        rayDirectionQueue.Dequeue();
                    }
                }

            }

            if (flag2 == false)
            {
                flag2 = true;
                Debug.Log("Node Collision - Queue contents: " + string.Join(", ", directionQueue.ToArray()) + ": " + string.Join(", ", rayDirectionQueue.ToArray()));
            }

        }
        else
        {
            moveSpeed = PLAYER_SPEED;
            anim.SetTrigger("Chomp");
            flag2 = false;
        }
    }

    bool CheckForClearedLevel()
    {
        if (gameControl.pelletsLeft == 0)
        {
            return true;
        }

        return false;
    }

    private void ProcessDirectionQueue()
    {
        currentDirection = direction;
        currentRayDirection = rayDirection;
        Vector3 potentialRayDirection = Vector3.zero;

        if (directionQueue.Count > 0)
        {
            direction = directionQueue.Peek();

            if (rayDirectionQueue.Count > 0)
            {
                rayDirection = rayDirectionQueue.Peek();
            }
        }
    }


    private directionEnum GetDirectionFromRotation(float rotationY)
    {
        directionEnum result = directionEnum.North;

        if (rotationY >= 315.0f || rotationY < 45.0f)
        {
            result = directionEnum.North;
        }
        else if (rotationY >= 45.0f && rotationY < 135.0f)
        {
            result = directionEnum.East;
        }
        else if (rotationY >= 135.0f && rotationY < 225.0f)
        {
            result = directionEnum.South;
        }
        else if (rotationY >= 225.0f && rotationY < 315.0f)
        {
            result = directionEnum.West;
        }

        return result;
    }

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.position;
        position = initialPosition;
        initialRotation = transform.rotation;
        rotation = initialRotation;
        targetRotation = initialRotation;
        initialDirection = GetDirectionFromRotation(initialRotation.eulerAngles.y);
        direction = initialDirection;
        nodePosition = transform.position;
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        gameControl.levelCleared = CheckForClearedLevel();

        if (gameControl.lockControls == false)
        {
            if (!gameControl.levelCleared)
            {
                // Check key presses
                if (Input.GetKeyDown(KeyCode.W))
                {
                    targetRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    potentialDirection = directionEnum.North;
                    HandleQueue(directionEnum.North, Vector3.forward);
                    if (flag == false)
                    {
                        //Debug.Log("KeyDown - Queue contents: " + string.Join(", ", directionQueue.ToArray()) + ": " + string.Join(", ", rayDirectionQueue.ToArray()));
                        flag = true;
                    }
                }

                else if (Input.GetKeyDown(KeyCode.S))
                {
                    targetRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                    potentialDirection = directionEnum.South;
                    HandleQueue(directionEnum.South, Vector3.back);
                    if (flag == false)
                    {
                        //Debug.Log("KeyDown - Queue contents: " + string.Join(", ", directionQueue.ToArray()) + ": " + string.Join(", ", rayDirectionQueue.ToArray()));
                        flag = true;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.A))
                {
                    targetRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
                    potentialDirection = directionEnum.West;
                    HandleQueue(directionEnum.West, Vector3.left);
                    if (flag == false)
                    {
                        //Debug.Log("KeyDown - Queue contents: " + string.Join(", ", directionQueue.ToArray()) + ": " + string.Join(", ", rayDirectionQueue.ToArray()));
                        flag = true;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.D))
                {
                    targetRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                    potentialDirection = directionEnum.East;
                    HandleQueue(directionEnum.East, Vector3.right);
                    if (flag == false)
                    {
                        //Debug.Log("KeyDown - Queue contents: " + string.Join(", ", directionQueue.ToArray()) + ": " + string.Join(", ", rayDirectionQueue.ToArray()));
                        flag = true;
                    }
                }

                if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
                {
                    if (directionQueue.Count == 2)
                    {
                        directionEnum directionCurrentlyMoving = directionQueue.Dequeue();
                        directionEnum potentialDirectionInQueue = directionQueue.Dequeue();

                        Vector3 directionCurrentlyMovingRay = rayDirectionQueue.Dequeue();
                        Vector3 potentialDirectionInQueueRay = rayDirectionQueue.Dequeue();

                        if (IsOppositeOrientation(directionCurrentlyMoving, potentialDirectionInQueue))
                        {
                            directionQueue.Enqueue(directionCurrentlyMoving);
                            rayDirectionQueue.Enqueue(directionCurrentlyMovingRay);
                        }
                    }

                    if (flag == true)
                    {
                        flag = false;
                        //Debug.Log("KeyRelease - Queue contents: " + string.Join(", ", directionQueue.ToArray()) + ": " + string.Join(", ", rayDirectionQueue.ToArray()));
                    }
                }

                // Continuously rotate towards the target rotation on every frame, even if key is released
                if (isRotating)
                {
                    rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, ROTATION_SPEED * Time.deltaTime);
                    transform.rotation = rotation;
                }

                if (rotation == targetRotation)
                {
                    isRotating = false;
                }

                //direction = GetDirectionFromRotation(transform.rotation.eulerAngles.y);

                CheckForNodeOverlap();
                ProcessDirectionQueue();

                if (!isRotating)
                {
                    moveSpeed = PLAYER_SPEED;
                    anim.SetTrigger("Chomp");
                }
                else
                {
                    moveSpeed = 0;
                    anim.SetTrigger("Stop");
                }

                if (rayDirection != Vector3.zero)
                {
                    // The farthest valid node we've hit
                    DirectionalNode lastValidNode = null;

                    if (CastRayDebug(GetDirectionFromRayDirection(rayDirection), out lastValidNode))
                    {
                        if (lastValidNode != null)
                        {
                            nodePosition = lastValidNode.transform.position;
                        }
                    }
                }
                if (position != nodePosition)
                {
                    position = Vector3.MoveTowards(position, nodePosition, moveSpeed * Time.deltaTime);
                }

                transform.position = position;

                Debug.Log("Pellets: " + gameControl.pelletsLeft);
            }
            else 
            {
                powerPellet.Stop();
            }
        }
    }
}
