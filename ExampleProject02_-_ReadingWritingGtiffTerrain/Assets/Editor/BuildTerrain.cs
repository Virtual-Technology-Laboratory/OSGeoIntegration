/*
 * Adapted from:
 * http://scrawkblog.com/2013/03/21/creating-seamless-terrian-tiles-in-unity/
 * 
 * Copyright (c) 2014, Roger Lew (rogerlew.gmail.com)
 * Date: 2/5/2015
 * License: BSD (3-clause license)
 * 
 * The project described was supported by NSF award number IIA-1301792
 * from the NSF Idaho EPSCoR Program and by the National Science Foundation.
 * 
 */

#define DEBUG

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using OSGeo.GDAL;
using System;

public class BuildTerrain : EditorWindow
{
    public Material bumpedSpecMaterialRef = (Material)Resources.Load("Materials\nature_bumped_specular.mat", typeof(Material));

    private float width;  // width in meters (x-axis)
    private float height; // height in meters (y-axis)
    private float length; // length in meters (z-axis)

    public int m_detailMapRes = 12;         // Not exactly sure what these should be
    public int m_detailResPerPatch = 2049;  // Not even Melo really knows
    public int m_controlMapRes = 2049;

    string path = @""; // path to the dem gtiff

    // Specify were the editor extension should be located
    [MenuItem("OSGeo.Gdal/Build Terrain")]
    public static void CreateBuildTerrain()
    {
        EditorWindow.GetWindowWithRect(typeof(BuildTerrain), new Rect(0, 0, 700, 200));
    }

    // When the user clicks "Build Terrain" this runs
    void OnGUI()
    {

        GUILayout.Label("Terrain Settings", EditorStyles.boldLabel);

        m_detailMapRes = Mathf.ClosestPowerOfTwo(EditorGUILayout.IntField("DetailMap Res", m_detailMapRes));
        m_detailResPerPatch = Mathf.ClosestPowerOfTwo(EditorGUILayout.IntField("Detail Res Per Patch", m_detailResPerPatch));
        m_controlMapRes = Mathf.ClosestPowerOfTwo(EditorGUILayout.IntField("Control Map Res", m_controlMapRes));

        if(GUILayout.Button("Specify Geotiff"))
            path = EditorUtility.OpenFilePanel("Specify Geotiff", "", "tif");

        EditorGUILayout.TextField("Path", path);

        if (GUILayout.Button("Create"))
        {
            OnCreate();
            this.Close();
        }
    }

    // When the user clicks the Create this gets run
    void OnCreate()
    {
        // First thing we need to do is make sure to register the Gdal drivers!
        Gdal.AllRegister();

        // Read the data into a 2d float array
        // find the width and length of the tile from the dataset's geotransform
        float[,] htmap;
        Read32bitGtiff(path, out htmap, out width, out length);

        // Find the minimum and maximum elevations
        float ymin, ymax;
        YminYmax2dArray(htmap, out ymin, out ymax);

        // Normalize heights between 0.2f and 0.9f
        // Unity needs them normalized between 0 and 1
        // The extra range on the top and bottom allows us
        // to sculpt the terrain
        Normalize2dArray(ref htmap, ymin, ymax);

        // Build a TerrainData object
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = htmap.GetLength(1);
        terrainData.alphamapResolution = m_controlMapRes;
        terrainData.SetDetailResolution(m_detailMapRes, m_detailResPerPatch);
        terrainData.SetHeights(0, 0, htmap);
        terrainData.size = new Vector3(width, height, length);

        // Use the terrainData object to build a new Terrain
        GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);
        terrain.name = "Terrain";
        terrain.transform.parent = terrain.transform;
        terrain.transform.localPosition = new Vector3(0, ymax - 0.9f * height, 0);

        // Now we will add the export to Gtiff component
        // If all you are doing is building a terrain you could stop here
        ExportToGtiff exporter = terrain.AddComponent<ExportToGtiff>();
        exporter.ref_path = path;
    }
    
    // Finds the minimum and maximum elevations of htmap
    void YminYmax2dArray(float[,] htmap, out float ymin, out float ymax)
    {
        // find elevation min, max, and range
        ymin = 1e38f;
        ymax = -1e38f;

        for (int i = 0; i < htmap.GetLength(0); i++)
        {
            for (int j = 0; j < htmap.GetLength(1); j++)
            {
                if (htmap[i, j] < ymin)
                    ymin = htmap[i, j];

                if (htmap[i, j] > ymax)
                    ymax = htmap[i, j];
            }
        }
    }


    // Normalizes htmap
    void Normalize2dArray(ref float[,] htmap, 
                   float oldmin, float oldmax, float newmin=0.2f, float newmax=0.9f)
    {
        if (newmax < newmin)
            Debug.Log("newmax must be greater than newmin");

        float newrng = newmax - newmin;
        float oldrng = oldmax - oldmin;
        height = oldrng / newrng;

        for (int i = 0; i < htmap.GetLength(0); i++)
        {
            for (int j = 0; j < htmap.GetLength(1); j++)
            {
                float x = htmap[i, j];
                float normval = (x - oldmin) / oldrng;
                normval *= newrng;
                normval += newmin;

                htmap[i, j] = normval;
            }
        }
    }

    // Reads the elevation data from a 32-bit DEM (digital elevation map) using GDAL
    // calculates the width and length from the dataset's geotransform
    void Read32bitGtiff(string path, out float[,] htmap, out float width, out float length)
    {
        Dataset ds = Gdal.Open(path, Access.GA_ReadOnly);

        int xSize = ds.RasterXSize;
        int ySize = ds.RasterYSize;

        if (xSize != Mathf.ClosestPowerOfTwo(xSize) + 1)
        {
            Debug.Log("Gtiff width must be 2^x + 1");
        }

        if (ySize != Mathf.ClosestPowerOfTwo(ySize) + 1)
        {
            Debug.Log("Gtiff width must be 2^x + 1");
        }

        if (ySize != xSize)
        {
            Debug.Log("Gtiff must be square");
        }

        htmap = new float[xSize, xSize];

        Band band = ds.GetRasterBand(1);

        float[] r = new float[xSize * ySize];

        band.ReadRaster(0, 0, xSize, ySize, r, xSize, ySize, 0, 0);

        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                htmap[xSize - j - 1, i] = r[i + j * xSize];
            }
        }

        double[] gt = new double[6];
        ds.GetGeoTransform(gt);

        width = (float)(Math.Abs(gt[1]) * (xSize - 1));
        length = (float)(Math.Abs(gt[5]) * (ySize - 1));

        ds = null;
    }

}
