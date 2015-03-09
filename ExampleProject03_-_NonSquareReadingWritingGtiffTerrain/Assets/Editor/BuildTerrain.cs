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

    int NextHighestPowerOfTwo(int x, int maxPow = 13)
    {
        int val = 2;
        for (int i = 1; i <= maxPow+1; i++)
        {
            int newval = val * 2;
            if (newval >= x)
                return newval;

            val = newval;
        }
        return -1;
    }

    int max(int x, int y)
    {
        if (x >= y)
            return x;
        else
            return y;
    }

    // When the user clicks the Create this gets run
    void OnCreate()
    {
        // First thing we need to do is make sure to register the Gdal drivers!
        Gdal.AllRegister();

        // Read the data into a 2d float array
        // find the width and length of the tile from the dataset's geotransform
        float[,] htmap;
        float xres, yres;
        Read32bitGtiff(path, out htmap, out xres, out yres);

        int nx = htmap.GetLength(0);
        int ny = htmap.GetLength(1);
        int n = max(NextHighestPowerOfTwo(nx - 1),
                    NextHighestPowerOfTwo(ny - 1)) + 1;

        rlib.Log("nx = {0}", nx);
        rlib.Log("ny = {0}", ny);
        rlib.Log("n = {0}", n);

        // Find the minimum and maximum elevations
        float ymin, ymax;
        YminYmax2dArray(htmap, out ymin, out ymax);

        // Normalize heights between 0.2f and 0.9f
        // Unity needs them normalized between 0 and 1
        // The extra range on the top and bottom allows us
        // to sculpt the terrain
        Normalize2dArray(ref htmap, ymin, ymax);

        Pad2dArray(ref htmap, n);
        width = Mathf.Abs(xres * n);
        length = Mathf.Abs(yres * n);

        rlib.Log("width = {0}", width);
        rlib.Log("length = {0}", length);

        // Build a TerrainData object
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = n;
        terrainData.alphamapResolution = m_controlMapRes;
        terrainData.SetDetailResolution(m_detailMapRes, m_detailResPerPatch);
        terrainData.SetHeights(0, 0, htmap);
        terrainData.size = new Vector3(width, height, length);

        // Use the terrainData object to build a new Terrain
        GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);
        terrain.name = "Terrain";
        terrain.transform.parent = terrain.transform;
        terrain.transform.localPosition = new Vector3(0, ymax - 0.9f * height, yres * (n - ny));

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

    void Pad2dArray(ref float[,] htmap, int n)
    {
 //       int yoffset = n - htmap.GetLength(1);
        float[,] tmp = new float[n, n];

        for (int i = 0; i < htmap.GetLength(0); i++)
        {
            for (int j = 0; j < htmap.GetLength(1); j++)
            {
                tmp[i, j] = htmap[i, j];
            }
        }

        htmap = tmp;
    }

    // Reads the elevation data from a 32-bit DEM (digital elevation map) using GDAL
    // calculates the width and length from the dataset's geotransform
    void Read32bitGtiff(string path, out float[,] htmap, out float xres, out float yres)
    {
        Dataset ds = Gdal.Open(path, Access.GA_ReadOnly);

        int xSize = ds.RasterXSize;
        int ySize = ds.RasterYSize;

        htmap = new float[ySize, xSize];

        Band band = ds.GetRasterBand(1);

        float[] r = new float[xSize * ySize];

        band.ReadRaster(0, 0, xSize, ySize, r, xSize, ySize, 0, 0);

        int k = 0;
        for (int i = 0; i < ySize; i++)
        {
            for (int j = 0; j < xSize; j++)
            {
                htmap[ySize - i - 1, j] = r[k];
                k++;
            }
        }

        double[] gt = new double[6];
        ds.GetGeoTransform(gt);

        xres = (float)gt[1];
        yres = (float)gt[5];
//        width = (float)(Math.Abs(gt[1]) * (xSize - 1));
//        length = (float)(Math.Abs(gt[5]) * (ySize - 1));

        ds = null;
    }

}
