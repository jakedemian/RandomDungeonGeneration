using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GenerateDungeon : MonoBehaviour {
    public GameObject[] oneDoorPrefabs;
    public GameObject[] twoDoorPrefabs;
    public GameObject[] twoDoorAlternatePrefabs;
    public GameObject[] threeDoorPrefabs;
    public GameObject[] fourDoorPrefabs;
    public GameObject emptyRoomPrefab;

    public int roomWidth = 7;
    public int roomHeight = 7;

    [Range(2,20)]
    public int chanceOfGeneratingExtraRoom = 4;

    private int dungeonWidth = 4;
    private int dungeonHeight = 4;

    // note that grabbing a value requires layout[y,x]
    private string[,] layout;
    private List<Node> criticalPath;
    private List<RoomTypeData> roomTypeData;
    private List<GameObject> roomObjects;

    private void Init() {
        GameObject[] rooms = GameObject.FindGameObjectsWithTag("Room");
        if(rooms.Length > 0) {
            for (int i = 0; i < rooms.Length; i++) {
                GameObject room = rooms[i];
                Destroy(room);
            }
        }

        layout = new string[dungeonHeight, dungeonWidth];
        criticalPath = new List<Node>();
        roomTypeData = new List<RoomTypeData>();
    }

    private void PrintLayout() {
        string printStr = "";
        for (int y = 0; y < layout.GetLength(1); y++) {
            for (int x = 0; x < layout.GetLength(0); x++) {
                if (layout[y, x] == null || layout[y, x].Equals("")) {
                    printStr += "X";
                } else {
                    printStr += layout[y, x];
                }
            }
            printStr += "\n";
        }

        Debug.Log(printStr);
    }

    private bool IsRoomValidAndEmpty(int x, int y) {
        if (x < 0 || x >= layout.GetLength(1)) {
            return false;
        }

        if (y < 0 || y >= layout.GetLength(0)) {
            return false;
        }

        if (layout[y, x] != null && !layout[y, x].Equals("")) {
            return false;
        }

        return true;
    }

    private bool IsRoomValidAndPopulated(int x, int y) {
        if (x < 0 || x >= layout.GetLength(1)) {
            return false;
        }

        if (y < 0 || y >= layout.GetLength(0)) {
            return false;
        }

        if (layout[y, x] == null || layout[y, x].Equals("") || layout[y, x].Equals("X")) {
            return false;
        }

        return true;
    }

    private List<Vector2Int> GetValidNeighboringRooms(int x, int y) {
        List<Vector2Int> validNeighboringRooms = new List<Vector2Int>();
        if (IsRoomValidAndPopulated(x + 1, y)) {
            validNeighboringRooms.Add(new Vector2Int(x + 1, y));
        }
        if (IsRoomValidAndPopulated(x - 1, y)) {
            validNeighboringRooms.Add(new Vector2Int(x - 1, y));
        }
        if (IsRoomValidAndPopulated(x, y + 1)) {
            validNeighboringRooms.Add(new Vector2Int(x, y + 1));
        }
        if (IsRoomValidAndPopulated(x, y - 1)) {
            validNeighboringRooms.Add(new Vector2Int(x, y - 1));
        }

        return validNeighboringRooms;
    }

    private bool IsCorner(int x, int y) {
        // if we are at the starting node, return false
        if (x == criticalPath[0].x && y == criticalPath[0].y) {
            return false;
        }

        bool xIsAtEdge = x == 0 || x == layout.GetLength(1);
        bool yIsAtEdge = y == 0 || y == layout.GetLength(0);
        return xIsAtEdge && yIsAtEdge;
    }


    /// <summary>
    /// Generates a dungeon floor map
    /// </summary>
    /// <returns></returns>
    public void GenerateNewDungeon() {
        Init();

        bool criticalPathComplete = false;
        while (!criticalPathComplete) {
            criticalPathComplete = GenerateCriticalPath();
        }

        PopulateExtraRooms();
        //PrintLayout();

        for (int y = 0; y < layout.GetLength(1); y++) {
            for (int x = 0; x < layout.GetLength(0); x++) {
                if (layout[y, x] == null || layout[y, x].Equals("") || layout[y,x].Equals("X")) {
                    roomTypeData.Add(new RoomTypeData(x, y, RoomType.EMPTY));
                } else {
                    List<Vector2Int> validNeighboringRooms = GetValidNeighboringRooms(x, y);
                    switch (validNeighboringRooms.Count) {
                        case 0:
                            Debug.LogWarning("Somehow the above if statement didn't catch this??");
                            roomTypeData.Add(new RoomTypeData(x, y, RoomType.EMPTY));
                            break;
                        case 1:
                            roomTypeData.Add(new RoomTypeData(x, y, RoomType.ONEDOOR));
                            break;
                        case 2:
                            roomTypeData.Add(new RoomTypeData(x, y, RoomType.TWODOOR));
                            break;
                        case 3:
                            roomTypeData.Add(new RoomTypeData(x, y, RoomType.THREEDOOR));
                            break;
                        case 4:
                            roomTypeData.Add(new RoomTypeData(x, y, RoomType.FOURDOOR));
                            break;
                        default:
                            Debug.LogError("Something went horribly wrong in populating RoomTypeData");
                            break;
                    }
                }
            }
        }

        // now we need to create a map using preconstructed room prefabs
        for(int i = 0; i < roomTypeData.Count; i++) {
            RoomTypeData thisRoomData = roomTypeData[i];
            GameObject roomPrefab = GetRoomPrefab(thisRoomData.roomType);
            Vector3Int roomGridPosition = new Vector3Int(roomWidth * thisRoomData.x, roomHeight * thisRoomData.y, 0);
            Vector3 roomWorldPosition = GlobalGrid.GetGrid().CellToWorld(roomGridPosition);

            GameObject newRoom = Instantiate(roomPrefab, roomWorldPosition, Quaternion.identity);

            if(thisRoomData.roomType == RoomType.FOURDOOR) {
                // we don't have to align anything, since any orientation of a 4 door room will work.
                // give it a random rotation then move on
                newRoom.GetComponent<RoomDirection>().RotateRandom();
                continue;
            } else if (thisRoomData.roomType == RoomType.EMPTY) {
                // we don't care about the orientation of empty rooms
                continue;
            }

            Vector2[] adjacentRoomDirections = GetAdjacentRoomDirections(thisRoomData.x, thisRoomData.y);
            Vector2[] roomDoorDirections = newRoom.GetComponent<RoomDirection>().GetDoorDirections();

            if(!Vector2ArraysAreEqual(adjacentRoomDirections, roomDoorDirections)){
                // we need to rotate the room up to 3 times
                bool rotationMatchFound = false;
                for(int j = 0; j < 3; j++) {
                    newRoom.GetComponent<RoomDirection>().RotateRoom90Degrees();
                    roomDoorDirections = newRoom.GetComponent<RoomDirection>().GetDoorDirections();
                    if(Vector2ArraysAreEqual(adjacentRoomDirections, roomDoorDirections)) {
                        rotationMatchFound = true;
                        break;
                    }
                }

                if(!rotationMatchFound && thisRoomData.roomType == RoomType.TWODOOR) {
                    Debug.Log("Resorting to alternate two door!");
                    // we need to try the alternate 2 door room
                    Destroy(newRoom);
                    roomPrefab = twoDoorAlternatePrefabs[Random.Range(0, twoDoorAlternatePrefabs.Length)];
                    newRoom = Instantiate(roomPrefab, roomWorldPosition, Quaternion.identity);
                    roomDoorDirections = newRoom.GetComponent<RoomDirection>().GetDoorDirections();

                    if (!Vector2ArraysAreEqual(adjacentRoomDirections, roomDoorDirections)) {
                        // we need to rotate the room up to 3 times
                        rotationMatchFound = false;
                        for (int k = 0; k < 3; k++) {
                            newRoom.GetComponent<RoomDirection>().RotateRoom90Degrees();
                            roomDoorDirections = newRoom.GetComponent<RoomDirection>().GetDoorDirections();
                            if (Vector2ArraysAreEqual(adjacentRoomDirections, roomDoorDirections)) {
                                rotationMatchFound = true;
                                break;
                            }
                        }

                        if (!rotationMatchFound) {
                            Debug.LogError("Could not figure out the rotation of this two door room after trying both types.");
                        }
                    }
                } else if(!rotationMatchFound){
                    Debug.LogError("Serious problem here, we couldn't find a rotation that worked.");
                }
            }
        }
    }

    private bool Vector2ArraysAreEqual(Vector2[] a, Vector2[] b) {
        for(int i = 0; i < a.Length; i++) {
            bool matchFound = false;
            for(int j = 0; j < b.Length; j++) {
                if(Vector2.Equals(a[i],b[j])) {
                    matchFound = true;
                    break;
                }
            }

            if (!matchFound) {
                return false;
            }
        }
        return true;
    }

    private Vector2[] GetAdjacentRoomDirections(int x, int y) {
        List<Vector2> adjacentRoomDirectionsList = new List<Vector2>();

        // check up
        if(IsRoomValidAndPopulated(x, y + 1)) {
            adjacentRoomDirectionsList.Add(Vector2.up);
        }

        // check down
        if (IsRoomValidAndPopulated(x, y - 1)) {
            adjacentRoomDirectionsList.Add(Vector2.down);
        }

        // check right
        if(IsRoomValidAndPopulated(x + 1, y)) {
            adjacentRoomDirectionsList.Add(Vector2.right);
        }

        // check left
        if (IsRoomValidAndPopulated(x - 1, y)) {
            adjacentRoomDirectionsList.Add(Vector2.left);
        }

        Vector2[] adjacentRoomDirections = adjacentRoomDirectionsList.ToArray();
        return adjacentRoomDirections;
    }

    public GameObject GetRoomPrefab(RoomType type) {
        if(type == RoomType.EMPTY) {
            return emptyRoomPrefab;
        } else if (type == RoomType.ONEDOOR) {
            return oneDoorPrefabs[Random.Range(0, oneDoorPrefabs.Length)];
        } else if (type == RoomType.TWODOOR) {
            return twoDoorPrefabs[Random.Range(0, twoDoorPrefabs.Length)];
        } else if (type == RoomType.THREEDOOR) {
            return threeDoorPrefabs[Random.Range(0, threeDoorPrefabs.Length)];
        } else if (type == RoomType.FOURDOOR) {
            return fourDoorPrefabs[Random.Range(0, fourDoorPrefabs.Length)];
        }

        Debug.LogWarning("Something went wrong in GetRoomPrefab, defaulting to giving empty room.");
        return emptyRoomPrefab;
    }

    private bool GenerateCriticalPath() {
        Init();

        // choose a random corner to start at, place an E there
        int[] cornersDimensions = new int[] {
            0,
            dungeonHeight - 1,
            dungeonWidth - 1,
        };

        int startingRoomX = cornersDimensions[Random.Range(0, 3)];
        int startingRoomY = cornersDimensions[Random.Range(0, 3)];
        layout[startingRoomY, startingRoomX] = "E";

        criticalPath.Add(new Node(startingRoomX, startingRoomY));

        // snake your way around, without backtracking, storing every move you make in a list and marking each cell with an C, until you reach another corner, marked L (ladder)
        bool criticalPathComplete = false;
        do {
            int currentX = criticalPath[criticalPath.Count - 1].x;
            int currentY = criticalPath[criticalPath.Count - 1].y;

            // check if we're done
            if (IsCorner(currentX, currentY)) {
                layout[currentY, currentX] = "L";
                criticalPathComplete = true;
                break;
            }

            // populate a list of valid positions to move to
            List<Vector2Int> possibleNextNodes = new List<Vector2Int>();

            if (IsRoomValidAndEmpty(currentX + 1, currentY)) {
                possibleNextNodes.Add(new Vector2Int(currentX + 1, currentY));
            }
            if (IsRoomValidAndEmpty(currentX - 1, currentY)) {
                possibleNextNodes.Add(new Vector2Int(currentX - 1, currentY));
            }
            if (IsRoomValidAndEmpty(currentX, currentY + 1)) {
                possibleNextNodes.Add(new Vector2Int(currentX, currentY + 1));
            }
            if (IsRoomValidAndEmpty(currentX, currentY - 1)) {
                possibleNextNodes.Add(new Vector2Int(currentX, currentY - 1));
            }

            if (possibleNextNodes.Count == 0) {
                Debug.LogWarning("We got stuck!  We can't find another valid node to move to!");

                break;
            }

            // choose one of these positions
            Vector2Int nextPosition = possibleNextNodes[Random.Range(0, possibleNextNodes.Count)];

            // add a node to the criticalpath list
            layout[nextPosition.y, nextPosition.x] = "C";
            criticalPath.Add(new Node(nextPosition.x, nextPosition.y));


        } while (!criticalPathComplete);

        return criticalPathComplete;
    }

    private void PopulateExtraRooms() {
        for (int y = 0; y < layout.GetLength(1); y++) {
            for (int x = 0; x < layout.GetLength(0); x++) {
                if (layout[y, x] == null || layout[y, x].Equals("")) {
                    TryPopulateExtraRoom(x, y);
                }
            }
        }
    }

    private void TryPopulateExtraRoom(int x, int y) {
        List<Vector2Int> neighboringCells = GetValidNeighboringRooms(x, y);

        if (neighboringCells.Count > 0) {
            int extraRoomDiceRoll = Random.Range(0, chanceOfGeneratingExtraRoom);
            if (extraRoomDiceRoll == 0) {
                layout[y, x] = "R";
            }
        }
    }
}

public enum RoomType {
    EMPTY,
    ONEDOOR,
    TWODOOR,
    THREEDOOR,
    FOURDOOR
}

public struct RoomTypeData {
    public int x;
    public int y;
    public RoomType roomType;

    public RoomTypeData(int x, int y, RoomType roomType) {
        this.x = x;
        this.y = y;
        this.roomType = roomType;
    }
}

public struct Node {
    public int x;
    public int y;

    public Node(int x, int y) {
        this.x = x;
        this.y = y;
    }
}
