﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// responsible for generating the visual representation of the data provided by
/// a TileMapData object
/// It generates a single mesh to represent the entire map
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TileMapMesh : MonoBehaviour {
    public float tileSize = 1f;      // vertex spacing per tile
    public float heightScale = 0.5f; // vertex height per unit of tile elevation
    public int tileResolution = 8;   // pixels per side of tile

    public void BuildMesh(TileMapData map) {
        var meshData = new MeshData();

        for (int row = 0; row < map.numRows; row++) {
            for (int col = 0; col < map.numCols; col++) {
                var tile = map.tileAt(row, col);
                // get world coordinates that correspond to tile
                float height = map.tileAt(row, col).elevation * heightScale;
                float bottom = row * tileSize;
                float top = row * tileSize + tileSize;
                float left = col * tileSize;
                float right = col * tileSize + tileSize;
                // assign these positions to the 4 vertices owned by this tile
                var vertices = new Vector3[] {
		    new Vector3(left, height, top),
		    new Vector3(right, height, top),
                    new Vector3(left, height, bottom),
                    new Vector3(right, height, bottom)
		};

                float uvX = (float)col / map.numCols;
                float uvY = (float)row / map.numCols;
                var normal = Vector3.up;
                // add top surface of tile to mesh
                AddSurface(meshData, uvX, uvY, vertices, normal);
		// add right and bottom surfaces of tile to mesh
                if (col < map.numCols - 1) { // create mesh to next tile in x direction
                    var tileToRight = map.tileAt(row, col + 1);
                    AddSideX(tile, tileToRight, meshData, uvX, uvY);
                }
                if (row < map.numRows - 1) { // create mesh to next tile in z direction
                    var tileAbove = map.tileAt(row + 1, col);
                    AddSideZ(tile, tileAbove, meshData, uvX, uvY);
                }
            }
        }

        // apply mesh to filter/renderer/collider
        ApplyMesh(meshData);
        BuildTexture();
    }

    void AddSideX(Tile tile, Tile next, MeshData meshData, float uvX, float uvY) {
        float height = tile.elevation * heightScale;
        float right = tile.col * tileSize + tileSize;
        float bottom = tile.row * tileSize;
        float top = tile.row * tileSize + tileSize;

        int diff = next.elevation - tile.elevation;
        if (diff != 0) {
            var norm = diff > 0 ? Vector3.left : Vector3.right;
            var nextHeight = next.elevation * heightScale;

            var vertices = new Vector3[] {
		new Vector3(right, height, top),
		new Vector3(right, nextHeight, top),
		new Vector3(right, height, bottom),
		new Vector3(right, nextHeight, bottom)
	    };

            AddSurface(meshData, uvX, uvY, vertices, norm);
        }
    }

    void AddSideZ(Tile tile, Tile next, MeshData meshData, float uvX, float uvY) {
        float height = tile.elevation * heightScale;
        float left = tile.col * tileSize;
        float right = tile.col * tileSize + tileSize;
        float top = tile.row * tileSize + tileSize;

        int diff = next.elevation - tile.elevation;
        if (diff != 0) {
            var norm = diff > 0 ? Vector3.back : Vector3.forward;
            var nextHeight = next.elevation * heightScale;

            var vertices = new Vector3[] {
		new Vector3(left, nextHeight, top),
            	new Vector3(right, nextHeight, top),
            	new Vector3(left, height, top),
            	new Vector3(right, height, top)
	    };

            AddSurface(meshData, uvX, uvY, vertices, norm);
        }
    }

    void AddSurface(MeshData meshData, float uvX, float uvY, Vector3[] vertices, Vector3 normal) {
        int v0 = meshData.vertices.Count;
        int v1 = v0 + 1;
        int v2 = v0 + 2;
        int v3 = v0 + 3;
        var uv = new Vector2(uvX, uvY); // TODO: get uv from terrain type

        meshData.vertices.AddRange(vertices);

        meshData.normals.AddRange(new Vector3[] { normal, normal, normal, normal });

        meshData.triangles.AddRange(new int[] { v0, v3, v2 }); // first tri
        meshData.triangles.AddRange(new int[] { v0, v1, v3 }); // second tri

        meshData.uv.AddRange(new Vector2[] { uv, uv, uv, uv });
    }

    void BuildTexture() {
        //Texture2D texture = new Texture2D(sizeX * tileResolution, sizeZ * tileResolution);
        int texWidth = 10;
        int texHeight = 10;
        Texture2D texture = new Texture2D(texWidth, texHeight);
        for (int z = 0; z < texHeight; z++) {
            for (int x = 0; x < texWidth; x++) {
                Color c = new Color((float)z / texHeight, 0, (float)x / texWidth);
                texture.SetPixel(x, z, c);
            }
        }

        texture.filterMode = FilterMode.Point; // Use Bilinear for blending, Point for no blending
        texture.Apply();
        var renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterials[0].mainTexture = texture;
    }

    void ApplyMesh(MeshData meshData) {
        // create and populate a new mesh
        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertices.ToArray();
        mesh.triangles = meshData.triangles.ToArray();
        mesh.normals = meshData.normals.ToArray();
        mesh.uv = meshData.uv.ToArray();
        mesh.Optimize(); // this could increase performance but also incurs higher generation time

        var filter = GetComponent<MeshFilter>();
        var collider = GetComponent<MeshCollider>();

        filter.mesh = mesh;
        collider.sharedMesh = mesh;
    }

    /// <summary>
    /// convenience container for storing related mesh data during generation
    /// </summary>
    private class MeshData {
        public MeshData() {
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            uv = new List<Vector2>();
            triangles = new List<int>();

        }
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<Vector2> uv;
        public List<int> triangles;
    }
}
