using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour
{
    //Initialise the grid, anf the mesh filter
    public SquareGrid squareGrid;   
    public MeshFilter walls;
    public MeshFilter cave;
    
    //Initialise vertices and triangle lists
    List<Vector3> vertices;
    List<int> triangles;

    //Given a vertex we need to be able to tell which triangle that vertex belongs to
    //So we create a dictionary
    //to make it work: we pass an integer for our key which will be thge vertex index from Triaangle Struct
    //and the value that we will get out will be a list of all the triangles that the vertex is part of
    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();

    //Variable that holds the outlines
    List<List<int>> outlines = new List<List<int>>();

    //Hash set that is used as an optimaisation variable in the generation of the mesh
    //so we dont have to check the vertices that we have checked before
    HashSet<int> checkedVertices = new HashSet<int>();

    //Method that clear the lists every time we generate a new mesh
    void ClearLists()
    {
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();
    }

    //Method that called to Generate the mesh of the cave every time
    //we 
    public void GenerateMesh(int[,] map, float squareSize)
    {
        //Clear lists every time we create a new mesh
        ClearLists();

        //Iniitalise the square grid every time we generate a new mesh
        squareGrid = new SquareGrid(map, squareSize);

        //initialise new lists for the triangles and vertices
        vertices = new List<Vector3>();
        triangles = new List<int>();

        //Loop through the square grid and "initalise" (thiangulate the squares) of the mesh 
        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        //Initialise the mesh and apply the textures to the cave:
        InitialiseMeshAndSetTexture(map, squareSize); 

        //Create the mesh for the Walls:
        CreateWallMesh();

    }

    //Methdod that initilaise the mesh of the cave and Assings texture to it
    void InitialiseMeshAndSetTexture(int[,] map, float squareSize)
    {
        //Create the mesh:
        Mesh mesh = new Mesh(); //Initialise the mesh
        cave.mesh = mesh; //Reference the filterer

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        //SetTextures to the cave:
        int tileSize = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize,
                                                map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileSize;
            float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize,
                                                map.GetLength(0) / 2 * squareSize, vertices[i].z) * tileSize;
            uvs[i] = new Vector2(percentX, percentY);
        }
        mesh.uv = uvs;
    }

    //Method that is used to create the mesh of the walls
    void CreateWallMesh()
    {
        //At first we need to calculate the outline of the mesh
        CalculateMeshOutlines();

        //Initialise the mesh and the lists that are holding the vertices
        //and the triangles of each mesh as well as the height of the walls 
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5;

        //Loop through every outline in the outline list
        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;    //initialise the starting index

                //4 vertices:
                wallVertices.Add(vertices[outline[i]]); // left
                wallVertices.Add(vertices[outline[i + 1]]); // right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                //1st triangle:
                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                //2nd triangle:
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }

        //Set the mesh vertices and tirangles to array:
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();

        //Create a collider for our mesh:
        SetWallsAndAddMeshColliderToWalls(wallMesh);
    }

    //Method that sets the wall mesh and Creates a collider for the 
    //walls so the walls that are crearted are fully collidable with 
    //an object (it can be our player or enemys)
    void SetWallsAndAddMeshColliderToWalls(Mesh wallMesh)
    {
        //Assign to mesh filter:
        walls.mesh = wallMesh;

        //Add mesh collides to wall object
        MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;
    }

    //Method that based on the points that we are selecting in order to 
    //create a square it creates triangles for the mesh of the walls.
    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            // all cases from 1 point selected:
            case 1:
                CreateMeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                break;
            case 2:
                CreateMeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                break;
            case 4:
                CreateMeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                break;
            case 8:
                CreateMeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                break;

            // all cases from 2 points selected:
            case 3:
                CreateMeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 6:
                CreateMeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                break;
            case 9:
                CreateMeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                break;
            case 12:
                CreateMeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                break;
            case 5:
                CreateMeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                break;
            case 10:
                CreateMeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // all cases from 3 points selected:
            case 7:
                CreateMeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 11:
                CreateMeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                CreateMeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                break;
            case 14:
                CreateMeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // all cases from 4 points selected:
            case 15:
                CreateMeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    //Method that Crewate the mesh from the points that we are giving from triangles 
    //params keyword specifies an unknown number of Nodes
    void CreateMeshFromPoints(params Node[] points)
    {
        //Make sure that points are assign to the list of vertices
        AssignVertices(points);

        //Create the triangles:
        CreateTrianglesBasedOnPoints(points);
    }

    //Method that creates a trinagle based o  the points 
    void CreateTrianglesBasedOnPoints(params Node[] points)
    {
        //Depending on the points that we are giving we generate the 
        //appropriate mesh.. We start from 3 since the shape with the 
        //less points is a triangle with 3 points.
        if (points.Length >= 3)
            GenerateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            GenerateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            GenerateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            GenerateTriangle(points[0], points[4], points[5]);
    }

    //Method that ensures that our points have value
    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)    //-1 by default (empty)
            {
                points[i].vertexIndex = vertices.Count; //initialise size
                vertices.Add(points[i].position);   //add vertex to vertices list
            }
        }
    }

    //Method that creates the triangles of the mesh
    void GenerateTriangle(Node a, Node b, Node c)
    {
        //add the 3 nodes in the triangle
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        //Create a new triangle
        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    //Mehtod that adds the new triangle to the Dictionary
    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        //Check if the dictionary contains the vertex index inside (key)
        //and if it does then we just add that triangle to the List of triangles
        //otherwise we create a new list of triangles and add it to the dictionary
        //(each list contains the indexes of the triangle)	
        if (triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    //Method that go through every vertex in map and check if is an outline vertex
    //and if it is it follows that outline all the way around until it mets itself again 
    //and then add to the outlines list
    void CalculateMeshOutlines()
    {

        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);

                //if exist:
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    //Method that is used by the CalculateMeshOutline method 
    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    //Method that givenr an outline vertex ccan find 
    //a connected vertex which forms the outline edge
    int GetConnectedOutlineVertex(int vertexIndex)
    {
        //List of all triangless containing all the vertex Index
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

        //Loop through that list of triangles 
        for (int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            //Initialise the triangle we are working on
            Triangle triangle = trianglesContainingVertex[i];

            //Loop through each of the vertices in the triangle and pass that vertrex allong with
            //the vertex index we pass in the begining of the method in to the outline edge to check if those 2
            //vertices formed an outline edge and if they do we return that index
            for (int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];

                //At some point the vertex index B will be the same as vertex index 
                //we dont want to check that since its pointless so we add the following if as a limitation
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    //Boolean method that tell us whether by giving two vertices if the edge that is 
    //formed by these 2 vertices is an outline or not.
    //How it works:
    //We are getting the list of all the triangles to which vertex a belongs and 
    //we check how many of this triangles contains vertex b and if vertex a  and vertex b share
    //only one common triangle (means is an outline edge)
    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        //Initialise a list of triangles that are containing the vertex a inside them
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        //Loop through the list
        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

    /***************************************************/
    //Struct that holds the data and methods of all the
    //triangles of the mesh
    /***************************************************/
    struct Triangle
    {
        //Initialise the 3 vertexIndex of a triangle and an integer array that holds the verticees 
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        int[] vertices;

        //Constructor:
        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        //Indexer:
        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        //Method that provide us a way to determine if a triangle contains a certain vertexIndex
        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    /***************************************************/
    //Class that represents the square grid in our mesh
    //which holds a 2d array of squares	
    /***************************************************/
    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    //Calculate position of control node:
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2,
                                              0,
                                              -mapHeight / 2 + y * squareSize + squareSize / 2);
                    //wall if its == 1 (active)
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }

        }
    }

    /***********************************************/
    //Class that represents every Square in our mesh
    /***********************************************/
    public class Square
    {
        //Reference the control nodes (edges of square)
        //and node (mid points of the squares)	
        //as well as the confiqurations of each square.
        //in one square there are 16 possible configurations fro control nodes
        //on/off -> representing a binary num eg 0000
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centreTop, centreRight, centreBottom, centreLeft;
        public int configuration;

        //Constructor:
        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft)
        {
            //Initianlise the control Nodes:
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            //Set values to nodes:
            centreTop = topLeft.right;
            centreRight = bottomRight.above;
            centreBottom = bottomLeft.right;
            centreLeft = bottomLeft.above;

            if (topLeft.active)
                configuration += 8; //1000
            if (topRight.active)
                configuration += 4; //0100
            if (bottomRight.active)
                configuration += 2; //0010
            if (bottomLeft.active)
                configuration += 1; //0001
        }
    }

    /***********************************************/
    //Class that represents every node in our mesh	
    /***********************************************/
    public class Node
    {
        public Vector3 position;    //Keep track of its position in the world
        public int vertexIndex = -1;    //Keep track of the index

        //Constructor:
        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }

    /***********************************************/
    //  Class that represents every 
    //  switch (control node) in our mesh
    /***********************************************/
    public class ControlNode : Node
    {
        public bool active; //active = wall // not active = emplty tile
        public Node above, right;

        //Constructor:
        //position is set by base constructor
        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
        {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }

    }
}