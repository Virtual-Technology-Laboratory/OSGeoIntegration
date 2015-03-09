/*
 * Copyright (c) 2014, Roger Lew (rogerlew.gmail.com)
 * Date: 2/5/2015
 * License: BSD (3-clause license)
 * 
 * The project described was supported by NSF award number IIA-1301792
 * from the NSF Idaho EPSCoR Program and by the National Science Foundation.
 * 
 */
 
using UnityEngine;
using System.Collections;

using OSGeo.GDAL;

using System.IO;
using System;

public class ExportToGtiff : MonoBehaviour {

    public string ref_path;

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
        float[,] htmap = terrain.terrainData.GetHeights(0, 0, nx, ny);
        float scale = terrain.terrainData.heightmapScale.y;
        float offset = scale * 0.2f;

        var buffer = new float[htmap.GetLength(0) * htmap.GetLength(1)];

        int k = 0;
        for (int i=nx-1; i>0; i--)
        {
            for (int j=0; j<ny; j++)
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
