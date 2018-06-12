using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateDungeon : MonoBehaviour {
    public GameObject[] oneDoorPrefabs;
    public GameObject[] twoDoorPrefabs;
    public GameObject[] threeDoorPrefabs;
    public GameObject[] fourDoorPrefabs;
    public GameObject[] emptyRoomPrefab;

    private int dungeonWidth = 4;
    private int dungeonHeight = 4;

    // note that grabbing a value requires layout[y,x]
    private string[,] layout;
    private List<Node> criticalPath;
    private List<RoomTypeData> roomTypeData;
    private List<GameObject> roomObjects;

    private void Init() {
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

    public void GenerateNewDungeon() {
        Init();

        bool criticalPathComplete = false;
        while (!criticalPathComplete) {
            criticalPathComplete = GenerateCriticalPath();
        }

        PopulateExtraRooms();
        PrintLayout();

        // now we have the general room layout
        // now we need to generate some data as to what type of room each room should be
        // types are EMPTY, ONE, TWO, THREE, FOUR.
        // these values coorespond to the number of doors this room type should have.
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

        // once the prefabs are placed on the grid, we need to iterate through the critical path and rotate the placed rooms so that the doors line up correctly

        // now that the critical path is lined up.. hmm as i type this i'm thinking i might have to do all of them at once
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
            int extraRoomDiceRoll = Random.Range(0, 5);
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
