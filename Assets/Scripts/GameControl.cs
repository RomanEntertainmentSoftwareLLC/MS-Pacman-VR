using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.ContentSizeFitter;

public class GameControl : MonoBehaviour
{
    public enum directionEnum { None, North, South, East, West };
    public int level;
    public int totalPellets;
    public int pelletsLeft;
    public bool lockControls;
    private GameObject pellet;
    private GameObject powerpellet;
    private GameObject nodepellet;
    private GameObject[,] directionNode;
    private GameObject[,] pellets;
    [SerializeField] private AudioSource level1IntroSong;
    private float rayDistance = 50.0f;
    public bool levelCleared;
    private MsPacman msPacman;
    private GameObject levelType1;
    private GameObject levelType1Blink;
    private int blinkCount = 0;
    private bool isBlinking = false;
    private bool isResetting = false;
    private float timer = 0f;

    private int[][][] maps = new int[][][]
    {
        // Map 1 (pink maze)
        new int[][]
        {
            new int[] {1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1},
            new int[] {2, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 2},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            new int[] {0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {2, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 2},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1}
        },
        // Map 2 (light blue maze)
        new int[][]
        {
            // The second map configuration goes here.
        },
        // Map 3 (brown maze)
        new int[][]
        {
            // The third map configuration goes here.
        },
        // Map 4 (dark blue maze)
        new int[][]
        {
            // The fourth map configuration goes here.
        }
    };

    public int[][][] nodePlacementMaps = new int[][][]
    {
        // Map 1 (pink maze)
        new int[][]
        {
            new int[] {1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            new int[] {0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0},
            new int[] {2, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 2},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0},
            new int[] {2, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 2},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1},
            new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1}
        },
        // Map 2 (light blue maze)
        new int[][]
        {
            // The second map configuration goes here.
        },
        // Map 3 (brown maze)
        new int[][]
        {
            // The third map configuration goes here.
        },
        // Map 4 (dark blue maze)
        new int[][]
        {
            // The fourth map configuration goes here.
        }
    };

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

    public bool CastRay(Vector3 position, directionEnum direction)
    {
        Vector3 rayDirection = GetDirectionVector(direction);

        RaycastHit hit;

        //Debug.DrawRay(position, GetDirectionVector(direction) * rayDistance, Color.red, 5f);

        if (Physics.Raycast(position, rayDirection, out hit, rayDistance))
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

    private int[][] GetCurrentMap()
    {
        // Use modulo to get the current map based on the level.
        int mapIndex = (level - 1) % 14;

        if (mapIndex < 2) // Levels 1-2
            return maps[0];
        else if (mapIndex < 5) // Levels 3-5
            return maps[1];
        else if (mapIndex < 9) // Levels 6-9
            return maps[2];
        else // Levels 10-14
            return maps[3];
    }

    private int[][] GetCurrentNodeMap()
    {
        // Use modulo to get the current map based on the level.
        int mapIndex = (level - 1) % 14;

        if (mapIndex < 2) // Levels 1-2
            return nodePlacementMaps[0];
        else if (mapIndex < 5) // Levels 3-5
            return nodePlacementMaps[1];
        else if (mapIndex < 9) // Levels 6-9
            return nodePlacementMaps[2];
        else // Levels 10-14
            return nodePlacementMaps[3];
    }

    void PlacePellets(float x_pos, float z_pos, float x_offset, float z_offset)
    {
        totalPellets = 0;
        int[][] currentMap = GetCurrentMap();

        pellets = new GameObject[currentMap[0].Length, currentMap.Length];

        for (int z = 0; z < currentMap.Length; z++)
        {
            for (int x = 0; x < currentMap[z].Length; x++)
            {
                GameObject prefabtoplace = null;

                switch (currentMap[z][x])
                {
                    case 1:
                        prefabtoplace = pellet;
                        break;
                    case 2:
                        prefabtoplace = powerpellet;
                        break;
                    default:
                        continue; // Skip if it's 0 or any other number
                }
                Vector3 startingposition = new Vector3(x_pos, 0.0f, z_pos);
                Vector3 position = startingposition + new Vector3(x * x_offset, 25.0f, -z * z_offset);
                pellets[x, z] = Instantiate(prefabtoplace, position, Quaternion.identity, transform);
                totalPellets += 1;
            }
        }
        pelletsLeft = totalPellets;
    }

    void PlaceNodePellets(float x_pos, float z_pos, float x_offset, float z_offset)
    {
        int[][] currentMap = GetCurrentNodeMap();
        directionNode = new GameObject[currentMap[0].Length, currentMap.Length];

        for (int z = 0; z < currentMap.Length; z++)
        {
            for (int x = 0; x < currentMap[z].Length; x++)
            {
                GameObject prefabtoplace = null;

                switch (currentMap[z][x])
                {
                    case 1:
                        prefabtoplace = nodepellet;
                        break;
                    case 2:
                        prefabtoplace = nodepellet;
                        break;
                    default:
                        continue; // Skip if it's 0 or any other number
                }

                Vector3 startingposition = new Vector3(x_pos, 0.0f, z_pos);
                Vector3 position = startingposition + new Vector3(x * x_offset, 25.0f, -z * z_offset);
                directionNode[x, z] = Instantiate(prefabtoplace, position, Quaternion.identity, transform);
            }
        }

        for (int z = 0; z < currentMap.Length; z++)
        {
            for (int x = 0; x < currentMap[z].Length; x++)
            {
                if (directionNode[x, z] != null)
                {
                    if (directionNode[x, z].transform.position != null)
                    {
                        if (CastRay(directionNode[x, z].transform.position, directionEnum.North)) 
                            directionNode[x, z].GetComponent<DirectionalNode>().canMoveNorth = true;
                        if (CastRay(directionNode[x, z].transform.position, directionEnum.South)) 
                            directionNode[x, z].GetComponent<DirectionalNode>().canMoveSouth = true;
                        if (CastRay(directionNode[x, z].transform.position, directionEnum.East))
                            directionNode[x, z].GetComponent<DirectionalNode>().canMoveEast = true;
                        if (CastRay(directionNode[x, z].transform.position, directionEnum.West))
                            directionNode[x, z].GetComponent<DirectionalNode>().canMoveWest = true;
                    }
                }
            }
        }
        directionNode[12, 22].GetComponent<DirectionalNode>().canMoveEast = true;
        directionNode[13, 22].GetComponent<DirectionalNode>().canMoveWest = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject msPacmanObject = GameObject.Find("Ms Pacman");
        if (msPacmanObject != null)
        {
            msPacman = msPacmanObject.GetComponent<MsPacman>();
        }

        levelType1 = GameObject.Find("Level Type 1");
        levelType1Blink = GameObject.Find("Level Type 1 Blink");
        levelType1Blink.SetActive(false);
        level = 0;
        lockControls = true;
        pellet = Resources.Load("Prefabs/Sprites/White Pellet") as GameObject;
        powerpellet = Resources.Load("Prefabs/Sprites/White Power Pellet") as GameObject;
        nodepellet = Resources.Load("Prefabs/Sprites/Invisible Pellet") as GameObject;
        PlaceNodePellets(-362.0f, 421.0f, 29.0f, 30.0f);
        PlacePellets(-362.0f, 421.0f, 29.0f, 30.0f);

        if (level1IntroSong != null)
            level1IntroSong.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (!level1IntroSong.isPlaying && level == 0)
            lockControls = false;

        if (levelCleared)
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                return;
            }

            // Start the sequence
            if (!isBlinking)
            {
                blinkCount = 0;
                isBlinking = true;
                timer = 2f; // Wait for 2 seconds initially
            }

            if (isBlinking && blinkCount < 6 && timer <= 0) // 6 times: On-Off-On-Off-On-Off
            {
                if (blinkCount % 2 == 0) // Even: On
                {
                    levelType1.SetActive(false);
                    levelType1Blink.SetActive(true);
                }
                else // Odd: Off
                {
                    levelType1.SetActive(true);
                    levelType1Blink.SetActive(false);
                }

                blinkCount++;
                timer = 0.25f; // Wait a quarter second between blinks
            }
            else if (isBlinking && blinkCount >= 6 && timer <= 0) // After blinking is done
            {
                isBlinking = false;
                isResetting = true;
                // Reset MsPacman and place pellets
                msPacman.position = msPacman.initialPosition;
                msPacman.rotation = msPacman.initialRotation;
                msPacman.direction = msPacman.initialDirection;
                msPacman.nodePosition = msPacman.initialPosition;
                msPacman.rayDirection = Vector3.zero;
                msPacman.directionQueue.Clear();
                msPacman.rayDirectionQueue.Clear();
                PlaceNodePellets(-362.0f, 421.0f, 29.0f, 30.0f);
                PlacePellets(-362.0f, 421.0f, 29.0f, 30.0f);
            }
        }

        if (isResetting)
        {
            levelCleared = false;
            isResetting = false;
        }
    }
}
