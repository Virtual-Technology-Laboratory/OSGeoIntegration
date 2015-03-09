using UnityEngine;
using System.Collections;

using OSGeo.GDAL;

using System.IO;
using System;

public class ExportToGtiff : MonoBehaviour {

    public string ref_path;
    public int yoffset = 0;
    public bool overwrite_ref;
    
    public string dst_path = @""; // path to the dem gtiff
    
	// Use this for initialization
    public void Export() 
    {
        if (!ref_path.Equals(dst_path))
        {
            if (File.Exists(dst_path))
                File.Delete(dst_path);

            File.Copy(ref_path, dst_path);
        }

        Dataset ds = Gdal.Open(dst_path, Access.GA_Update);
        int nx = ds.RasterXSize;
        int ny = ds.RasterYSize;

        Terrain terrain = GetComponent<Terrain>();
        float[,] htmap = terrain.terrainData.GetHeights(0, yoffset, nx, ny);

        rlib.Log("Terrain x = {0}", terrain.terrainData.heightmapWidth);
        rlib.Log("Terrain y = {0}", terrain.terrainData.heightmapHeight);
        rlib.Log("htmap  x = {0}", htmap.GetLength(0));
        rlib.Log("htmap y = {0}", htmap.GetLength(1));

        float scale = terrain.terrainData.heightmapScale.y;
        float offset = scale * 0.2f;

        var buffer = new float[htmap.GetLength(0) * htmap.GetLength(1)];

        int k = 0;
        for (int i=ny-1; i>0; i--)
        {
            for (int j=0; j<nx; j++)
            {
                buffer[k] = htmap[i, j] * scale - offset;
                k++;
            }
        }

        Band band = ds.GetRasterBand(1);
        band.WriteRaster(0, 0, nx, ny, buffer, nx, ny, 0, 0);

        ds = null;
        

    }
}
